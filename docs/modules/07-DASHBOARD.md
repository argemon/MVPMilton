# M07 — Dashboard

## 1. Identificação, objetivo e valor

**Bloco 07, P1.** Mostrar indicadores comerciais auditáveis por período e produtos com estoque baixo, sem confundir venda com recebimento financeiro.

## 2. Pré-requisitos e dependências concluídas

00–06 aprovados, vendas confirmadas/canceladas e movimentos íntegros. Ler RF-17/18, RN-13/20 e ADR-11. Relatórios do 08 não são necessários; lista completa de estoque já existe no 05.

## 3. Requisitos e prioridades

RF-17/18 P1; RNF-01/03/05/07/12; RN-01/02/13/15/20. CA-14 e período do CA-15. Medição final de carga é 09; consultas deste bloco já devem ser paginadas/eficientes.

## 4. Dentro e fora do escopo

Dentro: período/fuso, quantidades Confirmada/Cancelada, receita líquida, ticket médio, até 20 produtos de estoque baixo e link completo. Fora: lucro, margem, caixa, recebimentos, previsões, agregação persistida/cache e requisito de relatório futuro.

## 5. Atores e permissões

Dashboard.Ler: Admin/Gerente. Vendedor/Estoquista recebem 403 no GET e não veem menu; home continua com seus atalhos permitidos. API valida a policy mesmo por URL direta.

## 6. Entidades e banco

Sem entidade/tabela/migration obrigatória. Projeções em vendas/saldos/produtos existentes; revisar planos de consulta e índices existentes. Se novo índice for demonstravelmente necessário, criar migration própria revisada e testar upgrade; não adicionar tabela de estatística. Snapshot consistente por consulta única ou transação REPEATABLE READ curta somente leitura.

## 7. Fluxos principais e alternativos

1. Abrir painel → mês corrente no fuso da instalação → GET → cartões e estoque baixo.
2. Alterar início/fim → validar → recarregar painel preservando filtro.
3. Sem venda → indicadores zero; estoque baixo pode continuar preenchido.
4. Mais de 20 produtos baixos → mostrar primeiros 20 com totalItems e “Ver todos” em `/estoque?abaixoMinimo=true`.
5. API fora → mensagem/Repetir; não substituir indicadores conhecidos por zero como se fosse resultado.

## 8. Regras e validações

RN-13/20: `[inicio,fim)` civil convertido a UTC; início < fim, máximo 366 dias, informar ambos ou omitir ambos. Mês corrente: primeiro dia até primeiro dia do mês seguinte, fim exclusivo. Universo comercial: vendas confirmadoEm no intervalo, status atual Confirmada/Cancelada. Confirmadas = contagem Confirmada; canceladas = contagem Cancelada; receita = soma das Confirmadas; ticket = receita/confirmadas ou zero. Cancelamento posterior retira contribuição do período original. Rascunho/Descartada não entram.

RN-01: decimal em todo cálculo; ticket arredondado a duas casas por MidpointRounding.AwayFromZero somente na média final. Exemplo: média 1.005 →1.01; entradas com terceira casa de preço continuam rejeitadas. Estoque baixo = produto ativo com saldo <=mínimo, incluindo 0/0. Ordenação determinística por saldo crescente, nome e id (decisão proposta de apresentação).

## 9. Contrato REST

Base `/api/v1`.

| Método / rota | Request | Response | Erros |
| --- | --- | --- | --- |
| GET `/dashboard` | `inicio?,fim?` | 200 `{inicio,fim,fuso,geradoEm,quantidadeConfirmada,quantidadeCancelada,receitaLiquida,ticketMedio,estoqueBaixo:{items:[{produtoId,sku,nome,saldo,minimo}],page:1,pageSize:20,totalItems}}` | 400 VALIDACAO; 401; 403 SEM_PERMISSAO; 503 indisponível; 500 seguro |

Usar GET `/estoque/saldos?abaixoMinimo=true` existente para lista completa; nenhum endpoint de relatório novo. Request não recebe timezone arbitrário, total ou perfil.

## 10. Página, componentes e estados

Rota `/dashboard`: FiltroPeriodo, CartoesComerciais, ListaEstoqueBaixo, AvisoPosicaoAtual. Vazio: cartões zero e texto “Sem vendas confirmadas no período”; lista vazia “Nenhum produto com estoque baixo”. Carregando: skeleton/indicador sem comandos duplicados. Sucesso: números pt-BR/R$, período/fuso/geradoEm e aviso de posição atual. Erro: mensagem segura com Repetir; 401 navega login, 403 acesso negado. Nenhum dado sensível aparece para perfil não autorizado.

## 11. Erros e concorrência

Contagens/receita/linhas do mesmo response usam snapshot consistente; cancelamento concorrente não pode gerar ticket calculado com contagem de outro instante. Outro GET pode refletir novo estado e geradoEm diferente. Filtros inválidos não executam consulta ilimitada. Sem escrita ou lock comercial neste bloco.

## 12. Testes unitários e integração

Unidade: período/fuso, mês padrão, zero denominador e arredondamento AwayFromZero. Integração PostgreSQL: CA-14/15, rascunhos/descartadas excluídos, cancelamento posterior, bordas locais, produto ativo/inativo/0-0, limite de 20/totalItems e 401/403. Exercitar escrita concorrente versus snapshot e revisar ausência de N+1; não carregar todos os itens em memória.

## 13. Given/When/Then

| ID / rastreio | Given | When | Then |
| --- | --- | --- | --- |
| M07-CA01 / CA-14 | Vendas 25 e 15 no período, segunda cancelada | Consultar painel | Confirmadas 1, canceladas 1, receita/ticket 25 |
| M07-CA02 / RF-18 | Produto ativo saldo 0/mínimo 0 e outro inativo igual | Abrir baixo estoque | Só ativo aparece |
| M07-CA03 / RN-20 | Confirmações no início e no fim exato | GET período | Início incluído, fim excluído |
| M07-CA04 / RN-01 | Duas vendas 1.00 e 1.01 | Calcular ticket | 1.01 por AwayFromZero, sem double |
| M07-CA05 / RN-15 | Vendedor autenticado | GET direto | 403, sem métricas no corpo |

## 14. Checklist de execução

- [ ] Implementar projeção/snapshot/fórmulas e policy, sem duplicar cálculo no frontend.
- [ ] Verificar necessidade de migration: registrar “sem alteração de schema” ou testar novo índice do zero/upgrade 06→07.
- [ ] Implementar contrato/HTTP/painel e link de estoque existente; atualizar OpenAPI.
- [ ] Executar `dotnet restore`, `dotnet build --no-restore -c Release`, `dotnet test -c Release --no-build` em PostgreSQL isolado.
- [ ] Registrar M07-CA01–05, plano das consultas, estados visuais e comandos no estado.

## 15. DoD e desbloqueio

DoD global, RF-17/18 e fórmulas/segurança demonstradas; zeros distinguíveis de falha. Desbloquear **08**. Nenhum teste ou link depende da implementação futura de relatórios.
