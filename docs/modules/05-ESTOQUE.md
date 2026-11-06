# M05 — Estoque

## 1. Identificação, objetivo e valor

**Bloco 05, P0 para integridade e P1 para consulta.** Controlar saldo por movimentos rastreáveis, protegendo contra negativo, duplicidade e disputa, sem depender de Venda.

## 2. Pré-requisitos e dependências concluídas

00–04 aprovados, especialmente produto existente com saldo zero, policies e auditoria. Não executar este bloco antes de Produtos. Ler protocolos de banco/API e ADR-03/04/07/10.

## 3. Requisitos e prioridades

RF-09/10/11 P0; RF-12 P1. RNF-01/02/07/08/11/12; RN-02/04/05/07/08/09/14/15/20. CA-06/07 e limites de CA-15. RF-14/16 não pertencem a este bloco.

## 4. Escopo e exclusões

Dentro: entrada, saída manual, ajuste por delta, saldos, histórico, idempotência e auditoria. Fora: venda/estorno comercial, reserva, saldo absoluto, múltiplos depósitos, editar/excluir movimento e integração de compra/fiscal.

## 5. Atores e permissões

Estoque.Ler: todos. Historico/Movimentar: Admin/Gerente/Estoquista. Ajustar: Admin/Gerente. Vendedor não lê histórico nem movimenta; Estoquista não ajusta. Nunca aceitar ator enviado pelo cliente.

## 6. Entidades e migrations

Reusar saldos_estoque do 04. Criar movimentos_estoque com produto, delta, saldoAntes/Depois, tipo, motivo, ator, instante e operação manual; criar operacoes_idempotentes e índices únicos. CHECK saldo>=0, delta não zero, saldoDepois=saldoAntes+delta e sinal coerente. Produto/ator/operacao com FKs RESTRICT. Tipos comerciais podem estar reservados como nomenclatura, mas não são aceitos no POST nem inseridos; colunas/FKs de Venda/Item só são acrescentadas no 06. Não criar FK apontando para tabela futura.

Movimentos/auditoria não concedem UPDATE/DELETE ao runtime. Quantidade final deve caber em int (0–2.147.483.647); limite por operação é 1.000.000. Saldo ausente é inconsistência, não zero: abortar e exigir investigação sem criar saldo automaticamente.

## 7. Fluxos principais e alternativos

1. Consultar produto/saldo → escolher entrada/saída/ajuste → informar quantidade/delta e motivo → BFF cria chave UUID da intenção.
2. API valida policy/formato → transação registra chave → bloqueia produto e saldo → revalida ativo/saldo → calcula saldo novo → grava movimento, saldo, auditoria e resposta juntos.
3. Entrada soma; saída subtrai; ajuste usa delta assinado. Saldo negativo/overflow não aplica nada.
4. Retry/time-out preserva mesma chave/corpo; resultado concluído retorna recibo; mesma chave diferente corpo dá 409.
5. Produto inativo → 409; saldo inexistente → 409 SALDO_INEXISTENTE, log de integridade; sem mutação.
6. Sucesso atualiza saldo pelo GET atual e histórico; não tratar recibo antigo como saldo corrente.

## 8. Validações e regras

Quantidade inteira 1–1.000.000; ajuste não zero de -1.000.000 a 1.000.000; motivo 10–500 após trim. RN-02/04/05/08/09/14. Chave idempotente UUID obrigatória, mesma chave por intenção; API valida, UI apenas orienta. Não receber saldo final, tipo livre, ator ou data como autoridade. Data de consulta segue RN-20; se período informado, exigir início/fim juntos e <=366 dias.

## 9. Contratos REST

Base `/api/v1`. `Movimento={id,produtoId,tipo,delta,saldoAntes,saldoDepois,motivo,ator:{id,nome},ocorridoEm}`. Após 06 acrescenta vendaId nullable; antes pode retornar null sem consultar Venda. Saldo DTO inclui produtoId/sku/nome/saldo/minimo/ativo.

| Método / rota | Request | Response | Erros |
| --- | --- | --- | --- |
| GET `/estoque/saldos` | `search,ativo,abaixoMinimo,page,pageSize` | 200 página saldos | 400; 401/403 |
| GET `/estoque/movimentos` | `produtoId?,tipo?,inicio?,fim?,page,pageSize` | 200 página Movimento | 400; 401/403 |
| GET `/estoque/movimentos/{id}` | UUID | 200 Movimento | 401/403/404 |
| POST `/estoque/entradas` | Header Idempotency-Key; `{produtoId,quantidade,motivo}` | 201 Movimento + Location | 400; 401/403/404; 409 conflito |
| POST `/estoque/saidas` | Mesmo formato | 201 Movimento + Location | 400; 401/403/404; 409 ESTOQUE_INSUFICIENTE |
| POST `/estoque/ajustes` | Header Idempotency-Key; `{produtoId,delta,motivo}` | 201 Movimento + Location | 400; 401/403/404; 409 insuficiente/overflow |

Conflitos comuns: RECURSO_INATIVO, SALDO_INEXISTENTE, SALDO_EXCEDE_LIMITE, CHAVE_REUTILIZADA e OPERACAO_EM_ANDAMENTO (espera >5 s). Erros usam ProblemDetails e não expõem SQL. Deadlock/serialização: rollback e até duas novas tentativas completas; esgotado 503/Retry-After. Nenhum DELETE/PUT de movimento.

## 10. Páginas, componentes e estados

| Rota | Componentes propostos | Vazio / carregando / sucesso / erro |
| --- | --- | --- | --- |
| `/estoque` | ListaSaldos, FiltrosEstoque, Paginacao | Nenhum produto; carregando; saldos; Repetir |
| `/estoque/movimentos` | HistoricoMovimentos, FiltroPeriodoTipo | Nenhum movimento; carregando; histórico; erro com filtros |
| `/estoque/entrada`, `/estoque/saida` | FormularioMovimento, SeletorProduto, ResumoSaldo | Selecionar produto; enviando; recibo + saldo atual; insuficiência/conflito |
| `/estoque/ajuste` | FormularioAjuste, ConfirmacaoJustificativa | Selecionar produto; enviando; ajuste registrado; erro sem perder motivo |

Botão desabilitado ao enviar não substitui idempotência. Timeout mostra “Resultado ainda não confirmado” e Reconsultar/Repetir com mesma chave; não criar nova intenção automaticamente. Formulário de ajuste deixa sinal explícito.

## 11. Erros e concorrência

Ordem: chave → produto → saldo; múltiplos produtos sempre por UUID. PostgreSQL real, uma conexão/transação por comando. Duas saídas com saldo 1: uma vence, outra 409; nenhum negativo. UNIQUE na operação manual e na chave evita duplicidade. Falha ao inserir auditoria/movimento reverte saldo. Nunca editar movimento para corrigir erro: gerar ajuste autorizado justificado.

## 12. Testes unitários e integração

Unidade: sinais, motivo, limite, negativo e overflow calculados antes da atribuição; soma de deltas. Integração xUnit/PostgreSQL: CA-06/07, duas conexões com barreira, idempotência paralela/corpo diferente, falha antes do commit, resposta perdida após commit, saldo ausente, produto inativo, permissões, constraint negativa e imutabilidade com usuário runtime. Testar que mesma chave de outro ator não recupera o recibo original.

## 13. Given/When/Then

| ID / rastreio | Given | When | Then |
| --- | --- | --- | --- |
| M05-CA01 / CA-06 | Produto ativo saldo 0 | Entrada 10, saída 2, ajuste -1 | Saldo 7, três movimentos e auditorias, soma 7 |
| M05-CA02 / CA-07 | Saldo 1 | Duas saídas de 1 concorrentes | Uma 201, outra 409, saldo 0, uma baixa manual |
| M05-CA03 / RN-05 | Entrada válida e chave única | Repetir em paralelo após perda de resposta | Mesmo recibo, uma entrada |
| M05-CA04 / RN-02 | Produto sem linha de saldo em fixture de corrupção | Tentar saída | 409 SALDO_INEXISTENTE, nenhum movimento ou saldo criado |
| M05-CA05 / RN-15 | Estoquista autenticado | POST ajuste direto | 403 e saldo preservado |

## 14. Checklist de execução

- [ ] Confirmar gate Produtos e ausência de estoque manipulado por cadastro.
- [ ] Criar modelo/portas/transação/idempotência sem tabelas de Venda.
- [ ] Gerar/revisar migrations e privilégios; testar do zero e upgrade 04→05; aplicar explicitamente.
- [ ] Implementar API/UI/HTTP/OpenAPI, timeout e repetição segura.
- [ ] Executar `dotnet restore`, `dotnet build --no-restore -c Release`, `dotnet test -c Release --no-build` com PostgreSQL isolado.
- [ ] Registrar concorrência, rollback, reconciliação e M05-CA01–05 no estado.

## 15. DoD e desbloqueio

DoD global, RF-09–12 e CA-06/07 demonstrados; saldo/razão conciliam e zero falha de integridade. Desbloquear **06**. Nenhum teste depende de venda, cancelamento ou relatório futuro.
