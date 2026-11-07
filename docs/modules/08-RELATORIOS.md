# M08 — Relatórios

## 1. Identificação, objetivo e valor

**Bloco 08, P1.** Entregar consultas comerciais e de estoque com filtros, totais verificáveis e exportação CSV segura, usando as mesmas definições do dashboard.

## 2. Pré-requisitos e dependências concluídas

Normas transversais: arquitetura05/09 e ADR-20–29, MongoDB.Driver oficial restrito à infraestrutura, transações sequenciais em topologia compatível e testes MongoDB reais isolados (Atlas ou perfil Testcontainers explícito) conforme ADR-28. Classes/serviços de módulo futuro não são pré-requisitos ocultos.

00–07 aprovados, especialmente fórmulas de venda, fuso, saldo e histórico. Ler arquitetura 06/07/09 e ADR-11/12. Não depende de publicação, serviço de email, gerador PDF ou integração externa.

## 3. Requisitos e prioridades

RF-19/20 P1; RNF-01/03/04/05/07/12; RN-01/07/09/13/15/19/20. CA-14/15 e parte CSV do CA-17.

## 4. Dentro e fora do escopo

Dentro: vendas por período de confirmação, estoque atual e movimentos por período; tela paginada, totais do filtro completo e CSV UTF-8. Fora: saldo retroativo, fechamento contábil, PDF, impressão fiscal, pagamento/caixa, agendamento/envio de relatórios e BI externo.

## 5. Atores e permissões

Relatorios.Vendas: Administrador. Relatorios.Estoque/Movimentos: Administrador/Estoquista. Vendedor negado em todos. Exportação exige a mesma policy da consulta; BFF não substitui autorização API. Não retornar documento completo do cliente por conveniência se não consta no resumo necessário.

## 6. Entidades e banco

Sem coleção duplicada de relatórios. Projetar sales com itens incorporados, products com saldo e stock_movements; não incorporar histórico ilimitado em cadastros. Uma resposta tem snapshot consistente em transação curta somente leitura ou agregação com garantia equivalente. Índices adicionais somente com plano explain e evolução explícita testada. Sem saldo histórico fictício.

## 7. Fluxos principais e alternativos

1. Abrir página → informar período obrigatório para vendas/movimentos (UI pode preencher mês corrente) → aplicar filtros → paginar e mostrar totais gerais.
2. Estoque mostra posição atual com geradoEm; não possui campo de data histórica.
3. Exportar → BFF chama endpoint correspondente com filtros → API valida policy e limite → gera CSV de todo filtro, sem page/pageSize → download autenticado.
4. Sem resultados → página/CSV com cabeçalhos e totais zero; não 404.
5. >10 mil linhas → 400, orientar reduzir filtro; não truncar nem iniciar download de arquivo incompleto.
6. Falha API/DB → JSON ProblemDetails; BFF mostra erro, não salva esse JSON com extensão CSV.

## 8. Regras, formulários e validações backend

RN-13/20 e RF-19: vendas confirmadoEm no período, status atual Confirmada/Cancelada, contribuição zero para Cancelada, total original preservado; total geral independente da página. RN-09: movimento preserva delta/saldos/ator/motivo. Estoque inclui ativos e inativos conforme filtro; padrão ativo, `ativo=todos` explícito.

Início/fim obrigatórios em vendas/movimentos, início < fim, fim exclusivo, máximo 366 dias; página >=1, pageSize 1–100 e filtros UUID/tipo válidos. Exportação usa até 10.000 linhas; request com paginação em exportação é rejeitado para não prometer somente a página. Decimal com duas casas na saída; ticket, se incluído no resumo de vendas, aplica AwayFromZero do RF-17. Nenhum float/double.

CSV delimitador `;`, aspas escapadas, quebras tratadas, datas ISO e decimal com ponto. Textos iniciados por fórmula (=,+,-,@,tab,CR) são neutralizados conforme segurança; quantidade/dinheiro vêm de tipos numéricos. Não abrir caminho de arquivo indicado pelo usuário.

## 9. Endpoints REST

Base `/api/v1/relatorios`. Respostas JSON incluem filtros aplicados, fuso, geradoEm, paginação e totais; campos abaixo são o mínimo.

| Método / rota | Request | Response | Policy |
| --- | --- | --- | --- |
| GET `/vendas` | `inicio,fim,page,pageSize` | 200 `{items:[{vendaId,confirmadoEm,clienteNome,status,totalOriginal,contribuicaoLiquida}],page,pageSize,totalItems,totais:{quantidadeConfirmada,quantidadeCancelada,receitaLiquida},inicio,fim,fuso,geradoEm}` | Relatorios.Vendas |
| GET `/estoque` | `search,ativo,page,pageSize` | 200 `{items:[{produtoId,sku,nome,saldo,minimo,ativo}],page,pageSize,totalItems,geradoEm}` | Relatorios.Estoque |
| GET `/movimentos` | `inicio,fim,produtoId?,tipo?,page,pageSize` | 200 `{items:[Movimento],page,pageSize,totalItems,totais:{entradas,saidas,delta},inicio,fim,fuso,geradoEm}` | Relatorios.Movimentos |
| GET `/vendas/exportacao` | Mesmos filtros sem paginação | 200 text/csv + Content-Disposition attachment | Relatorios.Vendas |
| GET `/estoque/exportacao` | Mesmos filtros sem paginação | 200 text/csv + Content-Disposition attachment | Relatorios.Estoque |
| GET `/movimentos/exportacao` | Mesmos filtros sem paginação | 200 text/csv + Content-Disposition attachment | Relatorios.Movimentos |

Todos: 400 VALIDACAO/EXPORTACAO_EXCEDE_LIMITE, 401, 403 SEM_PERMISSAO, 503 indisponível e 500 genérico. ID filtro sem registros gera 200 vazio, sem consultar detalhe alheio. Movimento reutiliza DTO do 05/06. Totais entradas = soma dos deltas positivos; saídas = valor absoluto da soma dos negativos; delta = entradas-saidas, incluindo ajustes/vendas/estornos conforme filtro de tipo.

## 10. Páginas, componentes e estados

| Rota | Componentes propostos | Vazio / carregando / sucesso / erro |
| --- | --- | --- | --- |
| `/relatorios/vendas` | FiltroPeriodo, TabelaVendasRelatorio, TotaisVendas, BotaoCsv | Sem vendas; carregando; linhas/totais/período; falha/Repetir |
| `/relatorios/estoque` | FiltroEstoque, TabelaEstoque, BotaoCsv | Sem produtos; carregando; posição atual/geradoEm; falha |
| `/relatorios/movimentos` | FiltroPeriodoProdutoTipo, TabelaMovimentos, TotaisMovimentos, BotaoCsv | Sem movimentos; carregando; deltas/totais; falha |

Durante exportação mostrar progresso indeterminado e impedir cliques duplicados sem travar navegação. Downloads passam por handler Web autenticado `/downloads/relatorios/{tipo}` (GET com tipos permitidos e mesmos filtros), sem token em URL e sem gerar rota pública da API. Resposta CSV só depois de validar limite; erro permanece legível. UI avisa que tela/exportação em instantes diferentes podem refletir mudanças.

## 11. Erros e concorrência

Linhas/totais de uma resposta têm o mesmo snapshot. Escrita concorrente não mistura saldo/razão de instantes diferentes dentro da consulta. Exportação separada tem novo geradoEm e não promete congelamento do painel. Falha/cancelamento durante leitura não grava dados comerciais. Exportações limitadas não devem manter transação enquanto aguarda ação do usuário.

## 12. Testes unitários e integração

Unidade: datas/fuso, escaping/neutralização CSV, centavos Int64 e conversão decimal de transporte, filtros e limite. Integração MongoDB: totais independentes da página; 21 linhas/página 20/export21; bordas locais; cancelamento posterior; produto inativo e movimento original; 10.000 versus 10.001 linhas; snapshot concorrente; policies iguais nos seis endpoints; handler BFF não transforma erro em arquivo CSV. Conferir Content-Type/Disposition e ausência de token em URL.

## 13. Given/When/Then

| ID / rastreio | Given | When | Then |
| --- | --- | --- | --- |
| M08-CA01 / CA-15 | 21 vendas no filtro | Página20 e exportação | Total inclui 21; CSV 21 linhas mais cabeçalho |
| M08-CA02 / CA-14 | Vendas25/15 e segunda cancelada | Relatório do período original | Duas linhas, contribuição25/0 e receita 25 |
| M08-CA03 / CA-17 | Nome textual começa `=1+1` | Exportar | Campo neutralizado, não fórmula |
| M08-CA04 / RNF-12 | Filtro com 10.001 linhas | Exportar | 400 EXPORTACAO_EXCEDE_LIMITE, sem truncar |
| M08-CA05 / RN-15 | Vendedor autenticado | Consultar/exportar direto | 403 em ambos |

## 14. Checklist de execução

- [ ] Implementar consultas/totais/snapshot e políticas, sem schema comercial duplicado.
- [ ] Registrar ausência de evolução de persistência ou revisar/testar índice novo do zero/upgrade 07→08.
- [ ] Implementar CSV/handler BFF/UI/contratos e OpenAPI.
- [ ] Executar `dotnet restore`, `dotnet build --no-restore -c Release`, `dotnet test -c Release --no-build` em MongoDB isolado.
- [ ] Comparar dashboard/relatório sobre fixture estável; registrar M08-CA01–05 e resultados no estado.

## 15. DoD e desbloqueio

DoD global, RF-19/20, filtros/segurança CSV e limites comprovados. Desbloquear **09**. Nenhuma dependência de publicação, PDF ou serviço financeiro futuro.
