# M05 — Estoque

## 1. Identificação, objetivo e valor

**Bloco 05, P0 para integridade e P1 para consulta.** Controlar saldo por movimentos rastreáveis, protegendo contra negativo, duplicidade e disputa, sem depender de Venda.

## 2. Pré-requisitos e dependências concluídas

Normas transversais: arquitetura05/09 e ADR-20–29, MongoDB.Driver oficial restrito à infraestrutura, transações sequenciais em topologia compatível e testes MongoDB reais isolados (Atlas ou perfil Testcontainers explícito) conforme ADR-28. Classes/serviços de módulo futuro não são pré-requisitos ocultos.

00–04 aprovados, especialmente produto existente com saldo zero, policies e auditoria. Não executar este bloco antes de Produtos. Ler protocolos de banco/API e ADR-03/04/07/10.

## 3. Requisitos e prioridades

RF-09/10/11 P0; RF-12 P1. RNF-01/02/07/08/11/12; RN-02/04/05/07/08/09/14/15/20. CA-06/07 e limites de CA-15. RF-14/16 não pertencem a este bloco.

## 4. Escopo e exclusões

Dentro: entrada, saída manual, ajuste por delta, saldos, histórico, idempotência e auditoria. Fora: venda/estorno comercial, reserva, saldo absoluto, múltiplos depósitos, editar/excluir movimento e integração de compra/fiscal.

## 5. Atores e permissões

Estoque.Ler: todos. Historico/Movimentar: Administrador/Estoquista. Ajustar: Administrador. Vendedor não lê histórico nem movimenta; Estoquista não ajusta. Nunca aceitar ator enviado pelo cliente.

## 6. Entidades e evoluções de persistência

Reusar products.saldo incorporado desde 04. Criar `stock_movements` e `idempotency_receipts`. Movimento imutável contém _id, produtoId, delta int não zero, saldoAntes/Depois int >=0, tipo, motivo 10–500, atorId e nome snapshot, ocorridoEm e operacaoId UUID. Validadores verificam tipos, sinal e expressão saldoDepois=saldoAntes+delta; referências e auditoria são verificadas na transação. Índice unique parcial em operacaoId UUID para manuais. Sem sales antecipada; campos opcionais vendaId/itemId e índices de baixa/estorno só chegam no 06. Saldo final limitado a 2.147.483.647, limite por operação 1.000.000. Runtime sem update/remove em movimentos/auditoria. Saldo ausente/corrompido nunca é autocriado.

## 7. Fluxos principais e alternativos

1. Consultar produto/saldo → escolher entrada/saída/ajuste → informar quantidade/delta e motivo → BFF cria chave UUID da intenção.
2. API valida policy/formato → transação registra chave → atualiza product/saldo com filtro condicional → revalida ativo/saldo → calcula saldo novo → grava movimento, saldo, auditoria e resposta juntos.
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

Conflitos comuns: RECURSO_INATIVO, SALDO_INEXISTENTE, SALDO_EXCEDE_LIMITE, CHAVE_REUTILIZADA e OPERACAO_EM_ANDAMENTO (espera >5 s). Erros usam ProblemDetails e não expõem detalhes do armazenamento. Conflito transitório: abortar/repetir a transação inteira com mesma chave e limite da arquitetura05; commit incerto repete só commit e pode retornar503 RESULTADO_DESCONHECIDO. Nenhum DELETE/PUT de movimento.

## 10. Páginas, componentes e estados

| Rota | Componentes propostos | Vazio / carregando / sucesso / erro |
| --- | --- | --- | --- |
| `/estoque` | ListaSaldos, FiltrosEstoque, Paginacao | Nenhum produto; carregando; saldos; Repetir |
| `/estoque/movimentos` | HistoricoMovimentos, FiltroPeriodoTipo | Nenhum movimento; carregando; histórico; erro com filtros |
| `/estoque/entrada`, `/estoque/saida` | FormularioMovimento, SeletorProduto, ResumoSaldo | Selecionar produto; enviando; recibo + saldo atual; insuficiência/conflito |
| `/estoque/ajuste` | FormularioAjuste, ConfirmacaoJustificativa | Selecionar produto; enviando; ajuste registrado; erro sem perder motivo |

Botão desabilitado ao enviar não substitui idempotência. Timeout mostra “Resultado ainda não confirmado” e Reconsultar/Repetir com mesma chave; não criar nova intenção automaticamente. Formulário de ajuste deixa sinal explícito.

## 11. Erros e concorrência

Uma sessão por comando, transação snapshot/majority no primary. Registrar chave, obter produto ativo, aplicar update condicional ao mesmo documento products: saída exige saldo.quantidade >= quantidade; entrada/estorno exige saldo <= int.MaxValue-delta; incremento/decremento e versão de saldo são atômicos. Conferir MatchedCount/ModifiedCount; zero correspondência exige distinguir ausente, inativo, saldo ausente/insuficiente/overflow. Gravar movimento, audit_events e recibo na mesma transação. Chamadas de driver são sequenciais, nunca Task.WhenAll. Duas sessões disputando saldo 1 produzem um sucesso e outro 409 após retry/revalidação. Política de conflito/commit incerto/idempotência na arquitetura 05.

## 12. Testes unitários e integração

Unidade: sinais, motivo, limite, negativo e overflow calculados antes da atribuição; soma de deltas. Integração xUnit/MongoDB: CA-06/07, duas sessões com barreira, idempotência paralela/corpo diferente, falha antes do commit, resposta perdida após commit, saldo ausente, produto inativo, permissões, validação documental negativa e imutabilidade com usuário runtime. Testar que mesma chave de outro ator não recupera o recibo original.

## 13. Given/When/Then

| ID / rastreio | Given | When | Then |
| --- | --- | --- | --- |
| M05-CA01 / CA-06 | Produto ativo saldo 0 | Entrada 10, saída 2, ajuste -1 | Saldo 7, três movimentos e auditorias, soma 7 |
| M05-CA02 / CA-07 | Saldo 1 | Duas saídas de 1 concorrentes | Uma 201, outra 409, saldo 0, uma baixa manual |
| M05-CA03 / RN-05 | Entrada válida e chave única | Repetir em paralelo após perda de resposta | Mesmo recibo, uma entrada |
| M05-CA04 / RN-02 | Produto sem saldo incorporado em fixture de corrupção com operador de teste privilegiado | Tentar saída | 409 SALDO_INEXISTENTE, nenhum movimento ou saldo criado |
| M05-CA05 / RN-15 | Estoquista autenticado | POST ajuste direto | 403 e saldo preservado |

## 14. Checklist de execução

- [ ] Confirmar gate Produtos e ausência de estoque manipulado por cadastro.
- [ ] Criar modelo/portas/transação/idempotência sem coleções de Venda.
- [ ] Gerar/revisar evoluções de persistência e privilégios; testar do zero e upgrade 04→05; aplicar explicitamente.
- [ ] Implementar API/UI/HTTP/OpenAPI, timeout e repetição segura.
- [ ] Executar `dotnet restore`, `dotnet build --no-restore -c Release`, `dotnet test -c Release --no-build` com MongoDB isolado.
- [ ] Registrar concorrência, rollback, reconciliação e M05-CA01–05 no estado.

## 15. DoD e desbloqueio

DoD global, RF-09–12 e CA-06/07 demonstrados; saldo/razão conciliam e zero falha de integridade. Desbloquear **06**. Nenhum teste depende de venda, cancelamento ou relatório futuro.
