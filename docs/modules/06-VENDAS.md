# M06 — Vendas

## 1. Identificação, objetivo e valor

**Bloco 06, P0 para operação e P1 para consulta.** Entregar ciclo comercial íntegro: rascunho, confirmação com baixa atômica, consulta e cancelamento com estorno rastreável. Concluir histórico real do cliente.

## 2. Pré-requisitos e dependências concluídas

Normas transversais: arquitetura05/09 e ADR-20–29, MongoDB.Driver oficial restrito à infraestrutura, transações sequenciais em topologia compatível e testes MongoDB reais isolados (Atlas ou perfil Testcontainers explícito) conforme ADR-28. Classes/serviços de módulo futuro não são pré-requisitos ocultos.

00–05 aprovados: cliente, produto ativo, saldo, movimentos, idempotência, transação e autorização demonstrados. Não iniciar antes de Estoque. Ler integralmente arquitetura 04–07 e protocolo de escritas de guarda. Dashboard/relatórios não são necessários para validar este bloco.

## 3. Requisitos e prioridades

RF-13/14/16 P0, RF-15 P1, conclusão RF-06 P1. RNF-01/02/07/08/11/12; RN-01–11/15/17/20. CA-04 preenchido e CA-08–13; preservação de histórico do CA-14 é testável sem dashboard.

## 4. Dentro e fora do escopo

Dentro: cliente obrigatório, itens, preço servidor, totalização, edição de rascunho, confirmação, descarte, listagem/detalhe e cancelamento integral. Fora: pagamento, caixa, contas, emissão fiscal, desconto/frete/tributo, reserva, edição confirmada, devolução parcial e pedido online.

## 5. Atores e permissões

Administrador: todas as vendas e cancelamento. Vendedor: Criar e Ler/Editar/Confirmar apenas próprias; descarte usa Editar. Estoquista: nenhuma operação de Venda. Cliente histórico também filtra por criador para vendedor. Policy de rota mais propriedade no caso de uso; listagem/contagem filtradas antes de paginar. Venda alheia para vendedor → 404; policy ausente → 403.

## 6. Entidades e banco

Criar `sales` com array incorporado de até 100 itens, cada item com id UUID estável, produtoId, quantidade int, precoUnitarioCentavos e totalCentavos Int64, SKU/nome snapshot. Raiz guarda clienteId, criadorId, status, totalCentavos, versao, criadoEm/confirmadoEm/canceladoEm/descartadoEm na raiz conforme arquitetura05 e snapshots de cliente/operadores; confirmação/cancelamento/descarte têm metadados compatíveis com estado. Produtos repetidos são rejeitados no backend/validador de array, não por índice multikey unique (que não garante unicidade dentro do mesmo documento). Evoluir stock_movements com vendaId/itemId e índice unique parcial (vendaId,itemId,tipo) para baixa/estorno, mantendo manuais antigos válidos. Referências entre item/venda/produto são verificadas na mesma transação; sem coleção de itens nem chave estrangeira composta.

## 7. Fluxos principais

1. Selecionar cliente ativo e produtos ativos → POST rascunho com chave → API copia preços/nome/SKU e totaliza; saldo não muda nem reserva.
2. PUT substitui cliente/lista inteira, exige versão e recalcula snapshots/preços atuais explicitamente. Rejeitar produto repetido e qualquer produto inativo, inclusive mantido na lista após inativação; usuário pode removê-lo do rascunho.
3. Confirmar: chave → escrita condicional na venda → guarda cliente → produtos em UUID crescente com baixa condicional do saldo incorporado. Revalidar versão/estado, todos ativos, preços iguais e saldo de todos os itens. Gravar estado/snapshots finais/baixas/saldos/auditoria/recibo em commit único.
4. Descartar Rascunho com motivo → preservar registro e itens, sem movimentos.
5. Cancelar Confirmada com permissão/motivo → escritas de guarda ordenadas → validar capacidade de estorno → somar quantidades originais → registrar um EstornoVenda por item e metadados → commit. Produto/cliente inativo não impede estorno.
6. GET detalhe/histórico mostra snapshots comerciais e cadastros atuais separados. Substituir adaptador vazio do 03 e registrar conclusão da entrega incremental no estado.

## 8. Fluxos alternativos, validações e regras

RN-01: preço recebido do cadastro em centavos Int64, multiplicação e soma checked, sem float/double; HTTP permanece em reais com duas casas. Quantidade inteira 1–1.000.000, até 100 produtos distintos. Total por item e soma devem ficar <=999.999.999.999,99; cálculo validado antes de gravar. Multiplicação inteira por preço em centavos é exata: não arredondar por item nem alterar terceira casa de preço; preço inválido é 400. Regra de arredondamento de média só se aplica no dashboard.

RN-02/03/04/05: rascunho não exige estoque disponível, mas confirmação exige saldo incorporado existente e suficiente para cada produto. Saldo ausente não é recriado; venda permanece Rascunho. RN-08/11: produto inativo não entra por POST/PUT e inativação posterior impede confirmar. Preço alterado → 409 PRECO_ALTERADO até PUT explícito; nunca reajustar e confirmar silenciosamente.

RN-06/07/10: cancelamento integral exige Confirmada, motivo 10–500 e mantém baixa original. Descarte exige Rascunho e motivo 10–500. Repetição com mesma chave devolve recibo original; nova chave em estado terminal → 409. Cliente/itens confirmados não são fisicamente removidos.

## 9. Endpoints REST

Base `/api/v1`. `VendaDetalhe={id,clienteId,clienteSnapshot?,criador,status,itens:[{id,produtoId,skuSnapshot,nomeSnapshot,quantidade,precoUnitario,total}],total,versao,criadoEm,confirmacao?,cancelamento?,descarte?}`. Metadados de estado contêm ator/data/motivo quando aplicável. Snapshot do cliente é congelado ao confirmar. Erros ProblemDetails com code/traceId.

| Método / rota | Request | Response | Erros específicos |
| --- | --- | --- | --- |
| GET `/vendas` | `clienteId?,status?,inicio?,fim?,page,pageSize` | 200 página resumo, criação no período | 400 filtro; 401/403 |
| GET `/vendas/{id}` | UUID | 200 VendaDetalhe | 401/403/404 inclusive propriedade |
| POST `/vendas` | Idempotency-Key; `{clienteId,itens:[{produtoId,quantidade}]}` | 201 VendaDetalhe Rascunho + Location | 400 itens/limites; 404 referência; 409 inativo/chave |
| PUT `/vendas/{id}` | `{clienteId,itens:[...],versao}` | 200 VendaDetalhe atualizado | 400; 404; 409 versão/estado/inativo |
| POST `/vendas/{id}/confirmacao` | Idempotency-Key; `{versaoEsperada}` | 200 VendaDetalhe Confirmada | 400 versão ausente; 404; 409 estado/versão/preço/saldo/inativo |
| POST `/vendas/{id}/cancelamento` | Idempotency-Key; `{versaoEsperada,motivo}` | 200 VendaDetalhe Cancelada | 400 motivo; 403 policy; 404; 409 estado/versão/limite estorno |
| POST `/vendas/{id}/descarte` | Idempotency-Key; `{versaoEsperada,motivo}` | 200 VendaDetalhe Descartada | 400; 404; 409 estado/versão |
| GET `/clientes/{id}/vendas` | `inicio?,fim?,page,pageSize` | 200 página de resumos reais, filtrados pelo ator | 400; 401/403/404 |

Todas as escritas também admitem 401/403. Códigos: ESTOQUE_INSUFICIENTE, SALDO_INEXISTENTE, SALDO_EXCEDE_LIMITE, PRECO_ALTERADO, RECURSO_INATIVO, ESTADO_INVALIDO, VERSAO_DESATUALIZADA, CHAVE_REUTILIZADA, OPERACAO_EM_ANDAMENTO. Tentativas transacionais esgotadas → 503/Retry-After; commit incerto →503 RESULTADO_DESCONHECIDO com mesma chave; falha inesperada →500 seguro. Nenhum request aceita preço/total/status/ator do navegador.

## 10. Páginas, componentes e estados

| Rota | Componentes propostos | Vazio / carregando / sucesso / erro |
| --- | --- | --- | --- |
| `/vendas` | ListaVendas, FiltroVenda, Paginacao | Sem vendas; carregando; resumo; Repetir |
| `/vendas/nova` | EditorVenda, SeletorCliente, SeletorProduto, ItensVenda, TotalVenda | Sem itens permite salvar rascunho; salvando; detalhe; erro por campo |
| `/vendas/{id}` | DetalheVenda, EditorRascunho, ConfirmacaoVenda, CancelamentoVenda, DescarteVenda | Não localizado; carregando; estado/metadados visíveis; conflito pede reler |
| `/clientes/{id}` aba Histórico | HistoricoCliente, ResumoVenda | Nenhuma venda no escopo; carregando; histórico real; erro recuperável |

Confirmada/Cancelada/Descartada são somente leitura de itens. Ações sensíveis pedem confirmação/motivo. Resultado desconhecido por timeout mantém chave e oferece resolver repetição. Após recibo recuperado, GET atual evita mostrar Confirmada se já Cancelada. Erros preservam formulário e não exibem sucesso antes de resposta confirmada.

## 11. Erros e concorrência

Protocolo completo na arquitetura 05: sessão curta, snapshot/majority/primary; chave → venda → cliente → produtos por UUID, operações sequenciais. POST/PUT de rascunho escrevem guardVersion de cliente/produtos para validar ativo/preço contra alterações concorrentes, sem consumir/reservar saldo. Confirmação modifica venda por estado+versao, guarda cliente e produtos e baixa condicionalmente cada saldo incorporado; estado, snapshots, saldos, movimentos, auditoria e recibo têm um commit. Cancelamento usa quantidades originais, mesmo inativos, com limite de overflow e um estorno por item. Disputa com preço/status exige escrita real no documento (incremento guardVersion ou alteração de saldo), nunca leitura isolada ou update no-op. Conflito reinicia snapshot; commit incerto é resolvido com mesma chave, sem repetir efeitos cegamente. Nenhum HTTP/arquivo dentro da transação.

## 12. Testes unitários e integração

Unidade: total em centavos Int64, limites/duplicidade, transições, produto inativo no POST/PUT, preço alterado, autorização por criador, motivo e snapshots. Integração xUnit/MongoDB: CA-08–13, duas sessões/barreira, multi-item insuficiente, saldos incorporados ausentes, corrida com preço/status, idempotência paralela/corpo diferente/nova chave, resposta perdida pós-commit, falha de auditoria, estorno inativo/overflow, referências/índices, histórico próprio e snapshots após renomear cadastros. Testar recibo antigo após cancelamento seguido de GET atual.

## 13. Given/When/Then

| ID / rastreio | Given | When | Then |
| --- | --- | --- | --- |
| M06-CA01 / CA-08 | Produto 12.50, saldo 10 e cliente ativo | Rascunho de 2 e confirmação | Total 25.00, saldo 8, uma baixa, estado Confirmada |
| M06-CA02 / CA-09 | Dois itens, um sem saldo | Confirmar | 409, todos os saldos intactos, nenhuma baixa |
| M06-CA03 / CA-10 | Saldo 1, duas vendas distintas | Confirmar simultaneamente | Uma 200, outra 409, saldo 0 e uma baixa |
| M06-CA04 / CA-11 | Confirmação commitada, resposta perdida | Repetir mesma chave e depois nova chave | Mesmo recibo, sem baixa extra; nova chave 409 |
| M06-CA05 / CA-12 | Venda de 2 confirmada e produto agora inativo | Administrador cancelar e repetir | Um estorno +2, baixa original intacta, estado Cancelada |
| M06-CA06 / RN-08 | Produto inativo | POST/PUT adicionando produto | 409 RECURSO_INATIVO, nenhum item/saldo alterado |
| M06-CA07 / CA-04/13 | Vendas de A/B para mesmo cliente | A consulta histórico e tenta abrir venda de B | Só próprias; venda alheia 404 |

## 14. Checklist de execução

- [x] Confirmar gate 05; implementar entidade/casos/transação sem fiscal/financeiro.
- [x] Gerar/revisar evolução de persistência de sales com itens incorporados e referências/índices; testar base limpa e upgrade 05→06, preservando movimentos manuais.
- [x] Aplicar evolução de persistência explicitamente; implementar DTOs/API/policies/UI/HTTP/OpenAPI.
- [x] Remover adaptador vazio do 03, atualizar seu registro de entrega parcial e demonstrar preenchimento real.
- [x] Executar `dotnet restore`, `dotnet build --no-restore -c Release`, `dotnet test -c Release --no-build` com MongoDB isolado.
- [x] Reconciliar razão/saldos/baixas/estornos; registrar M06-CA01–07 e todos os CA críticos no estado.

## 15. DoD e desbloqueio

RF-06/13–16 completos, DoD global e CA-08–13 aprovados, zero falha de integridade/segurança. Desbloquear **07**. Toda comprovação usa endpoints existentes até 06, sem depender de dashboard/relatório.

## Implementação06 —07/11/2026, CONCLUÍDO e aprovado

Implementados rascunho, edição versionada, confirmação, cancelamento, descarte, resumos paginados e histórico real do cliente. `VendaResumo={id,clienteId,clienteNome?,criador,status,total,versao,criadoEm}` é projeção sem itens/documento pessoal; detalhe preserva o contrato acima. Movimentos retornam vendaId/itemId nullable, somente preenchidos em Venda/EstornoVenda. Chave/recibo/transação são coordenados pela mesma implementação comprovada05, e saldos usam a mesma atualização condicional.

Evolução06-sales/version7 explícita e repetível: sales, validação de estados/itens/somas, índices de consulta, expansão compatível de movimentos/recibos e índice comercial unique.05 reconhece definições exatas antigas/expandidas para retomar upgrade parcial. Escritas sem catálogo explícito retornam503, sem criar coleções implicitamente. Nenhuma DDL no startup.

Interface inclui busca de cliente/produto por nome/documento/SKU, quantidade/remoção, preços/subtotais/total, estados e motivo, filtros com seleção legível, paginação, histórico real e links de cadastros atuais separados de snapshots. Confirmação exige rascunho salvo; intenções pendentes mantêm chave/corpo e oferecem repetir. Após recibo recuperado, GET mostra estado atual. Itens confirmados/cancelados são somente leitura.

Primeira validação:17 integrações de Vendas/upgrade/BFF aprovadas,0 falhas/0 ignorados. Jornada de navegador passou com saldo10→8→10 e histórico cancelado. Comandos, inventário, falhas corrigidas e gates estão consolidados no [relatório](../execution/10-ENTREGA-ESTOQUE-VENDAS.md) e no estado03.07 não autorizado.

Resultado final: regressão Atlas95 integrações+102 unitários =197 aprovados; jornada completa com teclado na tabela e edição pendente validada. Commit812126c/CI42379411217:199 aprovados (102+97),0 falhas/skips, build/auditoria/scanner/artefatos aprovados; ambos perfis especiais executados. Todos os itens da seção14 demonstrados por execução explícita em bancos isolados. Aplicação das evoluções em gestao_comercial_dev aguarda autorização explícita após bloqueio da revisão automática; .env e cadastros preservados. Próximo07 depende de nova solicitação, sem deploy/merge automático.
