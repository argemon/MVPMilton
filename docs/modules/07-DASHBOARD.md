# M07 — Dashboard

## 1. Identificação, objetivo e valor

**Bloco 07, P1.** Mostrar indicadores comerciais auditáveis por período e produtos com estoque baixo, sem confundir venda com recebimento financeiro.

## 2. Pré-requisitos e dependências concluídas

Normas transversais: arquitetura05/09 e ADR-20–29, MongoDB.Driver oficial restrito à infraestrutura, transações sequenciais em topologia compatível e testes MongoDB reais isolados (Atlas ou perfil Testcontainers explícito) conforme ADR-28. Classes/serviços de módulo futuro não são pré-requisitos ocultos.

00–06 aprovados, vendas confirmadas/canceladas e movimentos íntegros. Ler RF-17/18, RN-13/20 e ADR-11. Relatórios do 08 não são necessários; lista completa de estoque já existe no 05.

## 3. Requisitos e prioridades

RF-17/18 P1; RNF-01/03/05/07/12; RN-01/02/13/15/20. CA-14 e período do CA-15. Medição final de carga é 09; consultas deste bloco já devem ser paginadas/eficientes.

## 4. Dentro e fora do escopo

Dentro: período/fuso, quantidades Confirmada/Cancelada, receita líquida, ticket médio, até 20 produtos de estoque baixo e link completo. Fora: lucro, margem, caixa, recebimentos, previsões, agregação persistida/cache e requisito de relatório futuro.

## 5. Atores e permissões

Dashboard.Ler: Administrador. Vendedor/Estoquista recebem 403 no GET e não veem menu; home continua com seus atalhos permitidos. API valida a policy mesmo por URL direta.

## 6. Entidades e banco

Sem nova entidade/coleção obrigatória. Agregações em sales e products com projeção e índices; estoque baixo consulta products.saldo.quantidade. Totais/contagens/linhas do mesmo response usam snapshot consistente (uma operação de agregação com snapshot quando suportado, ou transação curta somente leitura com ReadConcern.Snapshot). Índice adicional exige evidência, evolução explícita e upgrade testado; sem coleção de estatísticas.

## 7. Fluxos principais e alternativos

1. Abrir painel → mês corrente no fuso da instalação → GET → cartões e estoque baixo.
2. Alterar início/fim → validar → recarregar painel preservando filtro.
3. Sem venda → indicadores zero; estoque baixo pode continuar preenchido.
4. Mais de 20 produtos baixos → mostrar primeiros 20 com totalItems e “Ver todos” em `/estoque?abaixoMinimo=true`.
5. API fora → mensagem/Repetir; não substituir indicadores conhecidos por zero como se fosse resultado.

## 8. Regras e validações

RN-13/20 preservadas: intervalo civil [inicio,fim), fuso configurado, início < fim e máximo 366 dias, ambos informados ou omitidos. Universo: confirmadoEm no período e estado atual Confirmada/Cancelada; rascunhos/descartadas excluídos. Receita soma totalCentavos apenas das Confirmadas; canceladas contam separadamente e contribuem zero. Ticket médio em centavos arredonda uma única vez por AwayFromZero, com divisão inteira e resto (101+100)/2 → 101 centavos; denominador zero retorna zero. Usar acumulação exata sem promoção BSON para double e limites da arquitetura 05. Converter para reais somente no DTO/CSV. Estoque baixo ativo com saldo <= mínimo, inclusive 0/0, ordenado saldo/nome/id.

## 9. Contrato REST

Incremento autorizado ADR-36: GET único mantém período/fuso/geradoEm e retorna `resumo:{quantidadeConfirmada,quantidadeCancelada,receitaLiquida,ticketMedio}`, `produtosCadastrados`, `produtosAtivos`, `estoqueBaixo` paginado20, `recentes` até10 (vendaId,confirmadoEm,clienteNome,operadorNome,status,totalOriginal,contribuicaoLiquida), `evolucao` diária preenchida com zeros (dia,quantidade,receitaLiquida) e `maisVendidos` até10 (produtoId,sku,nome,quantidade,receitaLiquida). Esta estrutura substitui os quatro totais soltos do contrato mínimo abaixo. Recentes inclui Confirmada/Cancelada no filtro; evolução/ranking somente Confirmada. Identificação do ranking pelo snapshot mais recente; ordenação quantidade/receita/id. Financeiro HTTP em reais. /dashboard entrega todas as seções do mesmo snapshot sem endpoints separados inconsistentes. Sem evolução de schema/índice neste incremento; medir plano do índice06 existente.

Pré-condição de execução: catálogo06 válido; ausência/divergência dá503 PERSISTENCIA_NAO_INICIALIZADA em vez de zeros que aparentem uma instalação válida. Nenhuma evolução é aplicada pela consulta; teste real corrompe/restaura somente o catálogo técnico do banco descartável.

Base `/api/v1`.

| Método / rota | Request | Response | Erros |
| --- | --- | --- | --- |
| GET `/dashboard` | `inicio?,fim?` | 200 `{inicio,fim,fuso,geradoEm,quantidadeConfirmada,quantidadeCancelada,receitaLiquida,ticketMedio,estoqueBaixo:{items:[{produtoId,sku,nome,saldo,minimo}],page:1,pageSize:20,totalItems}}` | 400 VALIDACAO; 401; 403 SEM_PERMISSAO; 503 indisponível; 500 seguro |

Usar GET `/estoque/saldos?abaixoMinimo=true` existente para lista completa; nenhum endpoint de relatório novo. Request não recebe timezone arbitrário, total ou perfil.

## 10. Página, componentes e estados

Rota `/dashboard`: FiltroPeriodo, CartoesComerciais, ListaEstoqueBaixo, AvisoPosicaoAtual. Vazio: cartões zero e texto “Sem vendas confirmadas no período”; lista vazia “Nenhum produto com estoque baixo”. Carregando: skeleton/indicador sem comandos duplicados. Sucesso: números pt-BR/R$, período/fuso/geradoEm e aviso de posição atual. Erro: mensagem segura com Repetir; 401 navega login, 403 acesso negado. Nenhum dado sensível aparece para perfil não autorizado.

## 11. Erros e concorrência

Contagens/receita/linhas do mesmo response usam snapshot consistente; cancelamento concorrente não pode gerar ticket calculado com contagem de outro instante. Outro GET pode refletir novo estado e geradoEm diferente. Filtros inválidos não executam consulta ilimitada. Sem escrita ou escrita comercial de guarda neste bloco.

## 12. Testes unitários e integração

Unidade: período/fuso, mês padrão, zero denominador e arredondamento AwayFromZero. Integração MongoDB: CA-14/15, rascunhos/descartadas excluídos, cancelamento posterior, bordas locais, produto ativo/inativo/0-0, limite de 20/totalItems e 401/403. Exercitar escrita concorrente versus snapshot e revisar ausência de N+1; não carregar todos os itens em memória.

## 13. Given/When/Then

| ID / rastreio | Given | When | Then |
| --- | --- | --- | --- |
| M07-CA01 / CA-14 | Vendas 25 e 15 no período, segunda cancelada | Consultar painel | Confirmadas 1, canceladas 1, receita/ticket 25 |
| M07-CA02 / RF-18 | Produto ativo saldo 0/mínimo 0 e outro inativo igual | Abrir baixo estoque | Só ativo aparece |
| M07-CA03 / RN-20 | Confirmações no início e no fim exato | GET período | Início incluído, fim excluído |
| M07-CA04 / RN-01 | Duas vendas 1.00 e 1.01 | Calcular ticket | 1.01 por AwayFromZero, sem double |
| M07-CA05 / RN-15 | Vendedor autenticado | GET direto | 403, sem métricas no corpo |

## 14. Checklist de execução

- [x] Implementar projeção/snapshot/fórmulas e policy, sem duplicar cálculo no frontend.
- [x] Verificar necessidade de evolução de persistência: registrar “sem alteração de schema” ou testar novo índice do zero/upgrade 06→07.
- [x] Implementar contrato/HTTP/painel e link de estoque existente; atualizar OpenAPI.
- [x] Executar `dotnet restore`, `dotnet build --no-restore -c Release`, `dotnet test -c Release --no-build` em MongoDB isolado.
- [x] Registrar M07-CA01–05, plano das consultas, estados visuais e comandos no estado.

## 15. DoD e desbloqueio

DoD global, RF-17/18 e fórmulas/segurança demonstradas; zeros distinguíveis de falha. Desbloquear **08**. Nenhum teste ou link depende da implementação futura de relatórios.

Gate07 aprovado localmente em07/11/2026:115 unitários+101 integrações Atlas,0 falhas/skips; perfis ContainerSecurity/ContainerScale serão executados integralmente no CI final. Duas jornadas HTTPS concluídas, incluindo revisão visual360/1280 e correção de legibilidade das tabelas. Sem alteração de schema; evidências, falhas corrigidas e inventário no relatório11/estado03.08 liberado pela solicitação atual, sem09.

Validação final do código07c8065: CI236/236,0 falhas/skips; pré-condição de persistência, snapshot e jornada HTTPS/teclado/downloads aprovados. Estado03/relatório11 contêm os links e evidências.09 aguarda nova solicitação.
