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
