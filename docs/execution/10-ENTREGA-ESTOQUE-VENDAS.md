# Entrega05–06 — Estoque e Vendas

## Situação vigente em07/11/2026 (America/Cuiaba)

| Bloco | Estado | Evidência |
| --- | --- | --- |
|05 Estoque|Concluído e aprovado|Commits2016208/59acdbc; CI42374796524 success,173 testes sem falhas/skips; jornada real e proteções de banco comprovadas|
|06 Vendas|Implementação e validação local concluídas; CI final pendente|17 integrações específicas iniciais; regressão final95 integrações Atlas+102 unitários =197 aprovados,0 falhas/0 ignorados; navegador completo aprovado|

Branch `codex/stock-sales`, [PR2](https://github.com/argemon/MVPMilton/pull/2), base `feature/block-04-products`. PR1 e main preservados, sem merge/deploy.07-DASHBOARD é o próximo bloco e não foi iniciado nem autorizado.

## Funcionalidades e contratos entregues

- Estoque: GET `/api/v1/estoque/saldos`, `/movimentos`, `/movimentos/{id}`; POST `/entradas`, `/saidas`, `/ajustes`. Quantidade positiva/delta não zero, motivo obrigatório, saldo nunca negativo, histórico imutável, recibo por intenção e auditoria atômica. Web `/estoque`, `/estoque/movimentos`, `/estoque/entrada`, `/estoque/saida`, `/estoque/ajuste`.
- Vendas: GET/POST `/api/v1/vendas`, GET/PUT `/{id}`, POST `/{id}/confirmacao`, `/{id}/cancelamento`, `/{id}/descarte`; GET `/api/v1/clientes/{id}/vendas` real. Web `/vendas`, `/vendas/nova`, `/vendas/{id}` e histórico em `/clientes/{id}`. Listas projetam resumos, filtros/contagens são aplicados antes de paginar; Vendedor só recebe próprias vendas, Estoquista403, cancelamento somente Administrador.
- Valores calculados pelo servidor em centavos long/checked, HTTP em reais; snapshots congelados e cadastros atuais separados. Rascunho não reserva estoque; alterações não salvas bloqueiam confirmação na interface. Produto/cliente inativo impede rascunho/confirmação, mas permite estorno.
- Estado, saldo, movimentos, auditoria e recibo compartilham uma sessão snapshot/majority/primary, sem operações paralelas. Reuso efetivo da rotina de estoque e do controle de commit comprovado05. Mesma chave/corpo recupera resposta original; UI consulta estado atual em seguida. Confirmação/cancelamento repetidos não duplicam baixa/estorno.
- Evoluções explícitas05-stock/version6 e06-sales/version7, validators strict/error e índices idempotentes.06 preserva manuais/recibos05 e aceita reexecução de toda a cadeia; divergência de checksum/definição bloqueia. Comando sem catálogo503 não cria coleção implicitamente. Nada de DDL no startup, reserva, fiscal, pagamento,07 ou módulos posteriores.

## Validações finais realmente executadas

- `dotnet restore GestaoComercial.slnx --locked-mode --disable-parallel -m:1 /nodeReuse:false`: passou nos8 projetos; sem pacote novo.
- `dotnet build GestaoComercial.slnx -c Release --no-restore -m:1 /nodeReuse:false -p:UseSharedCompilation=false`: passou,0 avisos/0 erros.
- `dotnet test GestaoComercial.slnx -c Release --no-build -m:1 --filter 'Category!=ContainerSecurity&Category!=ContainerScale' --logger trx --results-directory artifacts/block06/final`:95 integrações reais Atlas+102 unitários =197 aprovados,0 falhas/0 ignorados. Inclui regressões00–05. Perfis ContainerSecurity e ContainerScale ficam explicitamente fora do perfil Atlas e serão executados no CI sem filtros; não contar como aprovados localmente nesta execução.
- `./scripts/verify-architecture.ps1` e `git diff --check`: passaram. Gitleaks8.24.2 no conjunto candidato203 arquivos/~1,04MB:0 segredos. Auditoria de pacotes fixados sem vulnerabilidades reportadas na execução05; CI repete a auditoria.
- `.local/sales-e2e.cjs`: HTTPS confiado, Edge headless, bancos exclusivos Atlas removidos ao final; entrada10 → rascunho2×12,50=25,00 → confirmação/saldo8 → cancelamento/saldo10 → histórico real do cliente.102/95 testes não incluem esse roteiro. Cookie/HTML/storage/logs sem credenciais,0 erros JS/5xx,360/1280 sem overflow, foco/teclado na tabela, edição pendente bloqueia confirmar. Capturas revisadas em `artifacts/block06/ui`.

## Concorrência, idempotência, rollback e rastreio

M05-CA01–05: razão concilia com saldo; duas saídas disputando última unidade têm uma201/outra409; chave paralela tem um efeito; corpo divergente/outro ator409; saldo ausente409 sem autocriar; Estoquista ajuste403. Falha de auditoria reverte saldo/movimento/recibo e mesma chave pode concluir depois.

M06-CA01–07:2500 centavos/25,00 HTTP com saldo10→8; múltiplos itens insuficientes e falha após baixas revertem todos os documentos; duas vendas disputando última unidade têm uma200/outra409; criação/confirmação idempotentes em paralelo; cancelamento uma vez com estorno, inclusive inativos; produto inativo rejeitado; histórico/contagem e detalhe alheio404 para Vendedor. Casos adicionais: alteração de preço exige PUT explícito, guardas reais contra preço/inativação de cliente/produto, saldo ausente, overflow/rollback do cancelamento, descarte sem movimento, instalação/upgrade/BSON inválido, fronteiras de paginação e validação de período.

StockCommitTests e ConfirmationControlFaults injetam falhas somente no controle da sessão, mantendo MongoDB real para todos os dados: resultado perdido após commit repete só commit, sem novo corpo/abort; transiente reinicia transação inteira; tentativas esgotadas abortam sem efeito; ambiguidade persistente503 é resolvida com a mesma chave. Filtros condicionais e resultado de update são conferidos. Baixa e estorno têm índice comercial único por venda/item/tipo.

## Correções e limites

Falhas históricas são preservadas abaixo e no estado03. Nesta implementação, corrigidos delimitadores JSON/referência `$$i` do validator, sintaxe de testes/Razor, largura dos campos de quantidade e legibilidade/teclado da tabela. Falha no seletor do roteiro de histórico foi corrigida sem mudar o produto. Todos os fluxos finais foram repetidos integralmente. Testes de corrupção restauram fixture para não depender da ordem de execução.

Atlas apresentou falhas transitórias em regressões anteriores, inclusive após o lote10mil; não foram contadas como sucesso. Perfil de volume passa a rodar no CI isolado junto ao de segurança, ambos sem filtro no workflow existente. Duas fixtures no máximo preservam disputas internas em sessões independentes. Nenhum orçamento/validador/assert foi ampliado/removido para obter CI verde. Banco descartável cuja limpeza falhou foi identificado por nome/horário exclusivo e removido com conferência exata; bancos de desenvolvimento preservados até esta validação. Docker não existe localmente; CI usa Testcontainers real autenticado. Não alegar carga20 usuários/p95 completo, homologação ou produção; Q-01/Q-02/Q-03 permanecem conforme os documentos.

## Inventário06

Novos: Domain/Vendas/RegrasVenda.cs; Application/Vendas/IVendas.cs; Infrastructure/Persistence/OperacoesComerciais.cs e Vendas/{VendasSchema,MongoVendas}.cs; Api/Acesso/EndpointsVendas.cs; Contracts/Vendas.cs; Web/ApiClients/VendasApiClient.cs e Pages/{Vendas,VendaDetalhe}.razor; testes SalesRulesTests, SalesTests, SalesUpgradeTests e SalesBffTests.

Alterados: composição/policies API e Web, leitura/atualização/contrato de estoque e evolução05 compatível, histórico MongoClientes, cliente HTTP/histórico do cliente, menu/CSS, readiness/OpenAPI/testes antigos, perfil de volume/RBAC e contratos/README/ADRs/estado/módulos/arquitetura afetados. Inventário completo do PR será listado na seção final. Nenhum segredo, bin/obj/log/artefato local será versionado.

## Histórico preservado das etapas anteriores

### Registro inicial05

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

Iniciado após gate05 no commit59acdbc: CI42374796524 aprovou93 unitários+80 integrações,0 falhas/skips, todas as etapas aprovadas. Banco de teste cuja limpeza falhou foi identificado por nome/horário exclusivo e removido com conferência exata; nenhuma pendência de limpeza dessa execução. Próximo após06 será07-DASHBOARD, sem autorização para implementação nesta solicitação.

## Arquivos do PR (05–06)

- [AGENTS.md](../../AGENTS.md)
- [README.md](../../README.md)
- [backend/GestaoComercial.Api/Acesso/EndpointsAcesso.cs](../../backend/GestaoComercial.Api/Acesso/EndpointsAcesso.cs)
- [backend/GestaoComercial.Api/Acesso/EndpointsEstoque.cs](../../backend/GestaoComercial.Api/Acesso/EndpointsEstoque.cs)
- [backend/GestaoComercial.Api/Acesso/EndpointsVendas.cs](../../backend/GestaoComercial.Api/Acesso/EndpointsVendas.cs)
- [backend/GestaoComercial.Api/Program.cs](../../backend/GestaoComercial.Api/Program.cs)
- [backend/GestaoComercial.Application/Estoque/IEstoque.cs](../../backend/GestaoComercial.Application/Estoque/IEstoque.cs)
- [backend/GestaoComercial.Application/Vendas/IVendas.cs](../../backend/GestaoComercial.Application/Vendas/IVendas.cs)
- [backend/GestaoComercial.Domain/Estoque/Movimentacao.cs](../../backend/GestaoComercial.Domain/Estoque/Movimentacao.cs)
- [backend/GestaoComercial.Domain/Vendas/RegrasVenda.cs](../../backend/GestaoComercial.Domain/Vendas/RegrasVenda.cs)
- [backend/GestaoComercial.Infrastructure/Clientes/MongoClientes.cs](../../backend/GestaoComercial.Infrastructure/Clientes/MongoClientes.cs)
- [backend/GestaoComercial.Infrastructure/Estoque/EstoqueSchema.cs](../../backend/GestaoComercial.Infrastructure/Estoque/EstoqueSchema.cs)
- [backend/GestaoComercial.Infrastructure/Estoque/MongoEstoque.cs](../../backend/GestaoComercial.Infrastructure/Estoque/MongoEstoque.cs)
- [backend/GestaoComercial.Infrastructure/Persistence/OperacoesComerciais.cs](../../backend/GestaoComercial.Infrastructure/Persistence/OperacoesComerciais.cs)
- [backend/GestaoComercial.Infrastructure/Vendas/MongoVendas.cs](../../backend/GestaoComercial.Infrastructure/Vendas/MongoVendas.cs)
- [backend/GestaoComercial.Infrastructure/Vendas/VendasSchema.cs](../../backend/GestaoComercial.Infrastructure/Vendas/VendasSchema.cs)
- [docs/README.md](../../docs/README.md)
- [docs/architecture/01-VISAO-GERAL.md](../../docs/architecture/01-VISAO-GERAL.md)
- [docs/architecture/05-BANCO-DE-DADOS.md](../../docs/architecture/05-BANCO-DE-DADOS.md)
- [docs/architecture/06-CONTRATOS-API.md](../../docs/architecture/06-CONTRATOS-API.md)
- [docs/execution/00-PLANO-EXECUCAO.md](../../docs/execution/00-PLANO-EXECUCAO.md)
- [docs/execution/03-ESTADO-PROJETO.md](../../docs/execution/03-ESTADO-PROJETO.md)
- [docs/execution/04-DECISOES-TECNICAS.md](../../docs/execution/04-DECISOES-TECNICAS.md)
- [docs/execution/10-ENTREGA-ESTOQUE-VENDAS.md](../../docs/execution/10-ENTREGA-ESTOQUE-VENDAS.md)
- [docs/modules/05-ESTOQUE.md](../../docs/modules/05-ESTOQUE.md)
- [docs/modules/06-VENDAS.md](../../docs/modules/06-VENDAS.md)
- [docs/requirements/01-ESCOPO.md](../../docs/requirements/01-ESCOPO.md)
- [frontend/GestaoComercial.Web/ApiClients/AcessoApiClient.cs](../../frontend/GestaoComercial.Web/ApiClients/AcessoApiClient.cs)
- [frontend/GestaoComercial.Web/ApiClients/ClientesApiClient.cs](../../frontend/GestaoComercial.Web/ApiClients/ClientesApiClient.cs)
- [frontend/GestaoComercial.Web/ApiClients/EstoqueApiClient.cs](../../frontend/GestaoComercial.Web/ApiClients/EstoqueApiClient.cs)
- [frontend/GestaoComercial.Web/ApiClients/VendasApiClient.cs](../../frontend/GestaoComercial.Web/ApiClients/VendasApiClient.cs)
- [frontend/GestaoComercial.Web/Components/LayoutPrincipal.razor](../../frontend/GestaoComercial.Web/Components/LayoutPrincipal.razor)
- [frontend/GestaoComercial.Web/Components/Pages/ClienteDetalhe.razor](../../frontend/GestaoComercial.Web/Components/Pages/ClienteDetalhe.razor)
- [frontend/GestaoComercial.Web/Components/Pages/Estoque.razor](../../frontend/GestaoComercial.Web/Components/Pages/Estoque.razor)
- [frontend/GestaoComercial.Web/Components/Pages/MovimentarEstoque.razor](../../frontend/GestaoComercial.Web/Components/Pages/MovimentarEstoque.razor)
- [frontend/GestaoComercial.Web/Components/Pages/Movimentos.razor](../../frontend/GestaoComercial.Web/Components/Pages/Movimentos.razor)
- [frontend/GestaoComercial.Web/Components/Pages/VendaDetalhe.razor](../../frontend/GestaoComercial.Web/Components/Pages/VendaDetalhe.razor)
- [frontend/GestaoComercial.Web/Components/Pages/Vendas.razor](../../frontend/GestaoComercial.Web/Components/Pages/Vendas.razor)
- [frontend/GestaoComercial.Web/Security/ContasHttp.cs](../../frontend/GestaoComercial.Web/Security/ContasHttp.cs)
- [frontend/GestaoComercial.Web/wwwroot/app.css](../../frontend/GestaoComercial.Web/wwwroot/app.css)
- [shared/GestaoComercial.Contracts/Estoque.cs](../../shared/GestaoComercial.Contracts/Estoque.cs)
- [shared/GestaoComercial.Contracts/Vendas.cs](../../shared/GestaoComercial.Contracts/Vendas.cs)
- [tests/GestaoComercial.IntegrationTests/AssemblySettings.cs](../../tests/GestaoComercial.IntegrationTests/AssemblySettings.cs)
- [tests/GestaoComercial.IntegrationTests/FoundationTests.cs](../../tests/GestaoComercial.IntegrationTests/FoundationTests.cs)
- [tests/GestaoComercial.IntegrationTests/SalesBffTests.cs](../../tests/GestaoComercial.IntegrationTests/SalesBffTests.cs)
- [tests/GestaoComercial.IntegrationTests/SalesTests.cs](../../tests/GestaoComercial.IntegrationTests/SalesTests.cs)
- [tests/GestaoComercial.IntegrationTests/SalesUpgradeTests.cs](../../tests/GestaoComercial.IntegrationTests/SalesUpgradeTests.cs)
- [tests/GestaoComercial.IntegrationTests/StockBffTests.cs](../../tests/GestaoComercial.IntegrationTests/StockBffTests.cs)
- [tests/GestaoComercial.IntegrationTests/StockCommitTests.cs](../../tests/GestaoComercial.IntegrationTests/StockCommitTests.cs)
- [tests/GestaoComercial.IntegrationTests/StockQueryTests.cs](../../tests/GestaoComercial.IntegrationTests/StockQueryTests.cs)
- [tests/GestaoComercial.IntegrationTests/StockRuntimeSecurityTests.cs](../../tests/GestaoComercial.IntegrationTests/StockRuntimeSecurityTests.cs)
- [tests/GestaoComercial.IntegrationTests/StockTests.cs](../../tests/GestaoComercial.IntegrationTests/StockTests.cs)
- [tests/GestaoComercial.IntegrationTests/StockUpgradeTests.cs](../../tests/GestaoComercial.IntegrationTests/StockUpgradeTests.cs)
- [tests/GestaoComercial.UnitTests/HostTests.cs](../../tests/GestaoComercial.UnitTests/HostTests.cs)
- [tests/GestaoComercial.UnitTests/SalesRulesTests.cs](../../tests/GestaoComercial.UnitTests/SalesRulesTests.cs)
- [tests/GestaoComercial.UnitTests/StockRulesTests.cs](../../tests/GestaoComercial.UnitTests/StockRulesTests.cs)
