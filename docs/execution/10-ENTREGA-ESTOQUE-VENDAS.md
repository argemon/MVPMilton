# Entrega05–06 — Estoque e Vendas

Solicitação de07/11/2026 autoriza05 e06 sequencialmente, commit/push/PR/CI; sem07/deploy/merge. Branch codex/stock-sales parte de f76697c, que contém os cadastros aprovados. PR anterior permanece aberto; a entrega pode ser revisada sobre feature/block-04-products, sem antecipar merge.

## Estoque05

Implementados RF-09–12: saldo incorporado em products como fonte única; entrada, saída e ajuste por delta; histórico imutável e paginado; autorização dos três perfis; chave UUID por intenção, recibo de resposta original por ator/escopo/hash e transação snapshot/majority/primary. Atualizações condicionais conferem saldo, versão e resultado. Falta de saldo não vira zero. Ajuste para zerar usa delta negativo explícito. Não há sales neste incremento.

API: GET /api/v1/estoque/saldos, /movimentos e /movimentos/{id}; POST /entradas, /saidas e /ajustes com Idempotency-Key obrigatório. API não aceita ator/data/saldo final. Web: /estoque, /estoque/movimentos, /estoque/entrada, /estoque/saida e /estoque/ajuste. Formulários preservam chave/corpo enquanto resultado está indefinido; repetição recupera recibo, depois GET consulta saldo atual.

Evolução05-stock/version6 explícita cria stock_movements e idempotency_receipts com validators strict/error, índices gerais/por produto/data/id, operação manual unique parcial, recibo ator/escopo/chave unique e data sem TTL. Não há DDL no startup. Inicialização e readiness passam a exigir05. Upgrade04→05 preserva produto e rejeita checksum divergente. Restrição de escrita em histórico deve ser provada no perfil ContainerSecurity do CI autenticado; usuários/roles Atlas não são alterados.

### Evidências locais já executadas

- Probe Atlas com acesso autorizado: conexão e topologia transacional disponíveis. Execução restrita não carregou conexão; nenhum segredo exibido ou .env alterado.
- Restore locked passou com acesso ao NuGet. Tentativa restrita falhou por DNS/NuGet e foi registrada, sem trocar versões.
- Build Release passou sem avisos/erros. Uma tentativa simultânea a teste em andamento falhou por DLL em uso; repetida após término, passou. Execuções seguintes devem ser sequenciais.
- StockTests:9 casos iniciais aprovados, incluindo duas saídas concorrentes da última unidade (uma201/outra409), mesma chave em paralelo com uma movimentação, corpo divergente/outro ator409, insuficiência/inativo/ausência/overflow, rollback por validator de auditoria e recuperação com mesma chave.
- Regressão intermediária:73 integrações +93 unitários aprovados,0 falhas/0 ignorados (artifacts/block05/verified). Teste ContainerSecurity explicitamente fora do perfil Atlas, aguardando CI; não contar como aprovado localmente.
- Uma regressão posterior concorrente ao navegador recebeu3 falhas de preparação/disputas antigas por503/timeout. Ajustado somente o máximo de fixtures simultâneas a2; concorrência interna dos cenários mantida e orçamento da aplicação inalterado. Nenhuma dessas falhas é contada como sucesso. Regresso final em execução.
- Explain com10.000 produtos:20 retornados,10.000 examinados,49ms no servidor/302ms ida e volta Atlas. Consulta por expressão exige varredura; nenhuma promessa de índice calculado/p95/carga20 usuários. Evidência em artifacts/block05/final.
- Jornada HTTPS/Atlas isolado/Edge: produto saldo0 → entrada10 → saída2 → ajuste-1 → saldo7, histórico com3 movimentos e pesquisa SKU. Cookie/HTML/storage/logs sem credenciais,0 JS/5xx, teclado. Roteiro inicial precisou esperar atualização do formulário; refeito completo. Revisão visual identificou fieldset largo em360; corrigido e fluxo repetido em viewport desktop exata360/1280, capturas revisadas em artifacts/block05/ui. Bancos exclusivos removidos.

### Inventário05

Novos: Domain/Estoque/Movimentacao.cs; Application/Estoque/IEstoque.cs; Infrastructure/Estoque/{EstoqueSchema,MongoEstoque}.cs; Api/Acesso/EndpointsEstoque.cs; Contracts/Estoque.cs; Web/ApiClients/EstoqueApiClient.cs; Web/Components/Pages/{Estoque,Movimentos,MovimentarEstoque}.razor; testes StockRulesTests, StockTests, StockBffTests, StockUpgradeTests, StockRuntimeSecurityTests e StockQueryTests; AssemblySettings.cs; este relatório.

Alterados: composição e policies Api/Web, cliente HTTP para chave opcional, menu/CSS, HostTests/OpenAPI e FoundationTests/readiness, AGENTS, README/índice, escopo/plano/estado/ADRs e documentos normativos de banco/API/módulo05. Nenhum pacote novo, arquivo de segredo ou dado real.

Regressão final: `dotnet test GestaoComercial.slnx -c Release --no-build -m:1 --filter 'Category!=ContainerSecurity' --logger trx --results-directory artifacts/block05/gate`:75 integrações+93 unitários =168 aprovados,0 falhas/0 ignorados. Build Release final sem avisos/erros. Gitleaks8.24.2:188 arquivos/~939KB,0 segredos;43 Markdown/117 links locais válidos; .env ignorado/hash preservado. Gate05 aguarda CI autenticado (inclui ContainerSecurity);06 ainda não iniciado. Não avançar enquanto houver item obrigatório pendente.

Complemento de integridade05: StockCommitTests executado contra Atlas real,3 casos aprovados/0 falhas/0 ignorados (artifacts/block05/commit-faults). Falha após commit real repete só commit (um corpo, dois commits, zero aborts); rótulo transitório antes de commit aborta/reinicia corpo (dois corpos, um abort); ambiguidade persistente retorna503 RESULTADO_DESCONHECIDO, sem repetir corpo/abortar, e mesma chave recupera recibo. Todos terminam com saldo3/um movimento. Correção: aceitar retry de TransientTransactionError também quando retornado pelo commit; desconhecido continua repetindo só commit. Regressão integral após essa correção em execução. Build inicial desse teste falhou por using ausente, corrigido; build seguinte0 avisos/erros.

## Vendas06

Validação adicional05: suíte com uma fixture Atlas por vez teve74 integrações aprovadas e5 falhas,0 ignoradas; timeouts no mesmo cliente da fixture StockTests afetaram criação de produto/usuário e limpeza do banco descartável. Os quatro StockCommitTests (incluindo tentativas esgotadas) passaram. Probe novo com cliente independente confirmou novamente conexão/topologia Atlas. Não contar essa suíte como aprovada; usar CI isolado para validar o commit completo e registrar banco de fixture cuja limpeza não concluiu. Nenhuma conexão/banco de desenvolvimento foi alterado.

Não iniciado. Pré-requisito é gate05 integralmente comprovado. Próximo após06 será07-DASHBOARD, sem autorização para implementação nesta solicitação.
