# Entrega07–08 — Dashboard e relatórios

Execução de07/11/2026, America/Cuiaba. Autorização anexada/ADR-36; somente07→08, Git/PR/CI, sem09/deploy/merge. Base812126c; árvore igual a origin/main5ebf72d após merge externo do PR2, nenhum merge executado pelo agente. Branch codex/dashboard-reports. Evidências locais ignoradas em artifacts/block07 e artifacts/block08. .env preservado (hash conferido contra registro anterior); nenhuma evolução aplicada ao banco persistente.

## Bloco07

RF-17/18, RN-01/02/13/15/20, CA-14/15 e M07-CA01–05. GET /api/v1/dashboard e página /dashboard exclusivos do Administrador; 401 anônimo,403 Vendedor/Estoquista. Um response contém período/fuso/geradoEm, resumo comercial, total de produtos cadastrados/ativos, vinte produtos baixos com totalItems e link completo, dez vendas recentes, evolução diária com zeros e ranking dez produtos.

Consultas MongoDB reais, projetadas/limitadas/agregadas no servidor em uma transação curta somente leitura snapshot/majority/primary. Reutiliza executor concreto e índice06 ix_sales_confirmado_status_id; explain com índice existente demonstrado na integração. Não foi criado schema/índice/coleção07. Produtos baixos usam ativo && saldo<=mínimo, inclusive0/0. Soma transitória Decimal128 exata convertida em centavos long com limite explícito; persistência monetária Int64 intacta. Nenhum double no cálculo. Ticket arredonda quociente/resto uma vez, AwayFromZero. Snapshot do produto na confirmação mais recente identifica o ranking; Cancelada aparece no histórico com contribuição zero.

### Comandos e evidências

- `dotnet restore --locked-mode`:8 projetos restaurados.
- `dotnet build --no-restore -c Release`:0 avisos/0 erros, incluindo compilação final das tabelas responsivas e testes de limite.
- `dotnet test -c Release --no-build --filter 'FullyQualifiedName~Dashboard' --logger trx --results-directory artifacts/block07/targeted`:8 unitários+4 integrações aprovados na primeira versão.
- Primeira regressão em artifacts/block07/regression:1 falha/109 passes unitários e2 falhas/99 passes de integração. Causas: teste OpenAPI ainda proibia07; contagem absoluta ignorava outra preparação da classe; indisponibilidade global invalidava cookie antes da consulta do painel. Corrigidos contrato esperado, baseline relativa e falha seletiva somente no transporte de teste, preservando negação quando validação de sessão não está disponível.
- `dotnet test -c Release --no-build --filter 'Category!=ContainerSecurity&Category!=ContainerScale' --logger trx --results-directory artifacts/block07/final`:110 unitários+101 integrações aprovados,0 falhas/0 ignorados. Os dois perfis específicos de container continuam obrigatórios no CI sem filtros (ADR-35), não são declarados executados no Atlas.
- `dotnet test tests/GestaoComercial.UnitTests -c Release --no-build --logger trx --results-directory artifacts/block07/money-final`:resultado final de limites registrado no estado03.
- `node .local/dashboard-e2e.cjs`:HTTPS confiado, Edge headless, API real, dois bancos Atlas exclusivos; login/troca, entrada10, confirmação2×12,50=25,00/saldo8, dashboard25, cancelamento/saldo10, dashboardzero/cancelada1, filtros vazio/inválido/recuperação,360/1280 e foco.0 erros JavaScript/5xx; cookie/HTML/storage/logs conferidos, bancos removidos. Capturas em artifacts/block07/ui. Segunda execução confere melhoria da leitura de tabelas por rolagem horizontal com foco.
- `./scripts/verify-architecture.ps1` e `git diff --check`:válidos. `dotnet package list --vulnerable --include-transitive --format json`:sem vulnerabilidades reportadas. `node .local/scan-candidate.cjs`:213 arquivos/~1,10MB,0 segredos na primeira varredura; repetir no candidato final.

Teste de snapshot força commit de cancelamento por cliente independente depois do resumo e antes das listas: o response permanece inteiramente Confirmada; próximo GET mostra Cancelada/receita0. Cem vendas sintéticas válidas demonstram soma maior que2^53 sem perder centavos. Unidade testa long.MaxValue, fração e excedente como400 LIMITE_MONETARIO_EXCEDIDO. Limite20/total22 e ativo/inativo, início incluído/fim excluído,1,00+1,01→ticket1,01, período vazio e policies estão automatizados. Sem N+1: quantidade de comandos independe de quantidade de linhas do filtro; somente projeções limitadas chegam à aplicação.

### Inventário07

- Application/Dashboard:IDashboard.cs, Indicadores.cs; Infrastructure/Dashboard:MongoDashboard.cs; Infrastructure/Properties/AssemblyInfo.cs (acesso interno somente ao assembly de testes unitários).
- Api/Acesso:EndpointsDashboard.cs, EndpointsAcesso.cs; Api/Program.cs.
- Contracts/Dashboard.cs; Web/ApiClients/DashboardApiClient.cs; Web/Components/Pages/Dashboard.razor e Estoque.razor (link de query abaixoMinimo); LayoutPrincipal.razor; Security/ContasHttp.cs; wwwroot/app.css.
- UnitTests:DashboardRulesTests.cs, AggregateMoneyTests.cs, HostTests.cs. IntegrationTests:DashboardTests.cs, DashboardBffTests.cs, BffTests.cs (falha seletiva de transporte exclusivamente de teste).
- AGENTS.md; docs/README.md; arquitetura06; execução00/03/04/11; requisitos01/02; módulo07. README raiz será atualizado na entrega final dos dois blocos.

Gate07 aprovado localmente:115 unitários+101 integrações reais,0 falhas/0 skips, duas jornadas HTTPS concluídas e capturas finais revistas. CI integral será acompanhado após publicação dos dois blocos.08 liberado.

## Bloco08

Iniciado somente após gate07. Especificação-base/regras/contratos/estado relidos; detalhes da entrega serão registrados após implementação/validação.

RF-19/20, RN-01/07/09/13/15/19/20; CA-14/15/17, M08-CA01–05. GET /api/v1/relatorios/{vendas|operadores|produtos|estoque|movimentos} e respectivos /exportacao. Vendas por confirmação/estado atual; visão por criador usuário (funcionário/operador histórico, inclusive bootstrap); ranking por produto com snapshots; saldo atual; razão com entradas/saídas/delta. Somente Administrador nos comerciais e Administrador/Estoquista nos de estoque. Todos têm paginação1–100, filtros e ordenação determinística, totais do filtro completo em snapshot. Estoque não aceita período histórico.

CSV UTF-8 BOM, delimitador ponto e vírgula, texto quoted/aspas escapadas/CR-LF tratados e prefixos de fórmula neutralizados (incluindo espaços e variantes Unicode); números invariantes em reais com duas casas. Limite10.000 linhas conferido no mesmo snapshot antes de ler/produzir o arquivo; request de exportação com paginação é rejeitado. Metadados de período/fuso/geradoEm em colunas; totais completos no JSON. Sem PDF, conforme escopo normativo. BFF /downloads/relatorios/{tipo} autentica cookie/policy, encaminha somente filtros permitidos e confere Content-Type; JSON de falha não recebe attachment. JS pequeno próprio (relatorios.js), sem biblioteca, faz download autenticado e mostra progresso no Blazor; token não sai do servidor.

Consultas reutilizam executor/projeções/totalização07, aggregate $group e projeções limitadas. Contagem e cursor separados dentro do snapshot evitam concentrar10 mil linhas em um documento facet16MiB. Sem N+1/coleção duplicada/evolução de schema. Índices existentes de confirmação, nome/SKU e movimentos permanecem; teste explain de estoque10.001 confirma consulta executável/página20, sem alegar p95/carga09.

Validações iniciais reais: restore locked8 projetos; build Release0 avisos/erros; testes direcionados11 unitários+6 integrações aprovados,0 falhas/skips. Consulta21 vendas/página20/export21, total500 e cancelada15/contribuição0, comparação exata com07, filtro por criador, ranking40 unidades, saldo8 inativo/histórico preservado, movimentos10/-2 e totais8, bordas/filtros/ordenação/erros/CSV e policies dos10 endpoints, BFF falhas400/503 sem attachment. Snapshot com movimento real confirmado entre leitura de totais e linhas preserva saldo0 no response, novoGETsaldo7. Perfil ReportsScale executado sozinho no Atlas:10.000 exportadas integralmente/10.001 rejeitadas400,1 aprovado0 skips. Navegador/regressão/CI ainda pendentes neste registro inicial.

Jornada HTTPS final08 aprovada: cinco relatórios360/1280, oito downloads CSV concluídos e conteúdo monetário/charset/metadata conferido, cancelamento refletido no período original, razão10 conciliada, filtro Venda com delta-2, vazio/erro/recuperação.0 JavaScript/5xx; cookie/HTML/storage/logs sem segredos; bancos descartáveis removidos. Capturas revistas em artifacts/block08/ui. A primeira automação disse download preparado mas não encontrou arquivo; a execução corrigida usou gesto de usuário e caminho absoluto Windows no controlador do navegador, com Browser.downloadProgress completed e arquivos reais. Nenhuma proteção da aplicação/TLS foi desabilitada; falha original não foi contada como aprovação. Sem regressão funcional observada até aqui.

Comandos finais executados: `dotnet restore --locked-mode` (atualizado); `dotnet build --configuration Release --no-restore` (0 avisos/erros); `dotnet test --configuration Release --no-build --filter 'Category!=ContainerSecurity&Category!=ContainerScale' --logger trx --results-directory artifacts/block08/final`:125 unitários+107 integrações aprovados,0 falhas/skips. Com ReportsScale isolado,233 casos distintos aprovados localmente. `node .local/scan-candidate.cjs artifacts/block08/gitleaks.json`:229 arquivos/~1,17MB,0 segredos; `dotnet package list --vulnerable --include-transitive --format json`:sem vulnerabilidades reportadas. `node .local/review-analytics.cjs`:44 Markdown/179 links locais,0 erros,0 credenciais reais, .env preservado. Arquitetura/diff válidos. Gate08 local aprovado; publicação/CI integral pendentes, sem09/deploy/merge. Resultado GitHub será registrado no estado03 após conclusão.

### Inventário08

- Application/Relatorios/IRelatorios.cs; Infrastructure/Relatorios/MongoRelatorios.cs.
- Api/Acesso/EndpointsRelatorios.cs,CsvSeguro.cs,EndpointsAcesso.cs,EndpointsEstoque.cs (mapper reaproveitado),Program.cs.
- Contracts/Relatorios.cs; Web/ApiClients/RelatoriosApiClient.cs; Security/DownloadsRelatorios.cs,ContasHttp.cs; Components/Pages/Relatorios.razor; App.razor/LayoutPrincipal.razor; Program.cs; wwwroot/relatorios.js.
- UnitTests/CsvTests.cs,HostTests.cs; IntegrationTests/ReportsTests.cs,ReportsScaleTests.cs,ReportsBffTests.cs.
- Normas:arquitetura06, módulo08, ADR-36; estado03/relatório11. Índices/validators/catálogos anteriores não alterados.
