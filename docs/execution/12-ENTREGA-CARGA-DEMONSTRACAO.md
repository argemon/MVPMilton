# Entrega09 — Carga de demonstração acadêmica

## Concluído — CI integral aprovado

Somente09 implementado. ADR-37 e documentação oficial remota ada96fd conciliadas;10 é homologação futura e não foi iniciado. API, sessões Web e carga usam o único banco gestao_comercial_dev no Atlas existente. .env preservado; sem banco/ambiente Demo, outra URI, instância ou configuração obrigatória adicional. Nenhum deploy/merge de PR executado.

## Comportamento

Administração > Dados de Demonstração (`/administracao/dados-demonstracao`) mostra resumo, banco lógico e última carga concluída. GET `/api/v1/administracao/dados-demonstracao` retorna banco/instante sem URI/segredo e no-store. POST `/gerar` cria/retoma; POST `/restaurar` exige confirmar=true também na API. Todos usam policy administrativa e revalidação da sessão nos serviços.

O gerador concreto reutiliza cadastros, Identity, estoque e vendas. Seed902026 e data-base07/11/2026; estoque inicial01/08/2026. UUIDs/senhas aleatórios, e-mails reservados example.invalid. Senhas não são retornadas: redefinição administrativa usa o fluxo existente. Filtre01/08/2026–08/11/2026 para todo o cenário.

Proveniência vem da auditoria transacional existente (`tcc-demo-v1/geracao/etapa`), sem coleção/indexação adicional de controle nem inferir propriedade por nomes. Repetição recupera etapas concluídas. Uma réplica conforme ADR-13: cargas sequenciais nesse host. Não existe coordenação distribuída; múltiplas réplicas exigem revisão antes de habilitar o gerador.

Restauração verifica referências de vendas, clientes, produtos, categorias, marcas, funcionários/contas, movimentos, sessões e alterações manuais auditadas. Conflito409 recusa o conjunto inteiro. Exclusões somente dos UUIDs comprovados, na mesma transação snapshot/majority/primary com guarda administrativa e conflito contra escritas concorrentes nas referências. Auditoria permanece. Sem DropDatabase/limpeza de coleção no gerador, sem apagar dependência manual para liberar restauração.

## Cenário final conferido no banco existente

| Item | Observado |
| --- | --- |
| Funcionários/contas da carga |12, três perfis,9 ativos/3 inativos |
| Usuários totais |13:12 demonstrativos + administrador bootstrap fora da carga |
| Clientes |40;35 com vendas/5 sem compras |
| Produtos |100;95 ativos/5 inativos;10 saldos zero |
| Categorias/marcas |5 de cada |
| Vendas |250 com dois itens;225 confirmadas/25 canceladas |
| Movimentos |850:300 manuais,500 baixas,50 estornos |
| Conciliação produto × soma dos deltas |0 divergências |
| Receita líquida do período completo |18401569 centavos (R$184.015,69) |

Consulta anterior à primeira carga confirmou users/customers/products/sales zerados. Inicializadores explícitos00–06 e tickets8 foram aplicados ao banco persistente por autorização atual, resolvendo a pendência histórica. Primeiro administrador sintético provisionado pelo bootstrap normal e senha trocada pelo formulário HTTP/antiforgery. Acesso somente em .local/demo-access.json ignorado; não em Git/log/HTML. Banco Web antigo não foi apagado. Hosts de validação são temporários locais, sem publicação.

## Evidências e testes executados

| Verificação | Resultado real |
| --- | --- |
| dotnet restore / restore --locked-mode |8 projetos aprovados |
| Build Release e builds atuais API/Web/tests |0 avisos/erros |
| Regra/unidade atual (unit-final) |126 aprovados,0 falhas/skips |
| Regressão Atlas antes de complementar a massa (final) |126 unitários+113 integrações aprovados,0 falhas/skips; filtro apenas ContainerSecurity/ContainerScale |
| Contratos atuais após complemento (current-contracts) |4 integrações aprovadas: GET/negações, página/BFF, catálogo compartilhado/upgrade |
| Carga inicial (focused) |3 integrações aprovadas; geração/repetição/restauração, saldo/relatórios e conflito manual; cenário preliminar de um item |
| Jornada atual no banco persistente |Geração, restauração confirmada e repetição por clique; resumo conferido idêntico após repetir;250 vendas/850 movimentos, metadados da última conclusão, páginas360/1280, Dashboard e cinco relatórios,0 erros JavaScript |
| Auditoria de dependências |0 frameworks vulneráveis |
| Scanner e busca por segredos conhecidos |239 arquivos,0 detecções e0 arquivos contendo credenciais reais |
| Arquitetura / documentação / diff |Referências válidas,44 Markdown/137 links locais sem quebra; diff --check limpo |
| .env |Hash SHA256 inalterado, conteúdo nunca exibido |

O teste de regressão antes do complemento também executou interrupção antes de ProdutoCriado, retomada sem duplicação, duas cargas concorrentes, restauração sem confirmação400, falha de auditoria na restauração com rollback integral, cliente manual preservado e venda manual referenciando produto demonstrativo impedindo exclusão. O teste atual amplia as mesmas verificações para dois itens/inativos/última conclusão e foi executado/aprovado no CI integral c622648, com116 integrações. Não transferir a evidência da massa antiga para declarar esse teste atual executado localmente.

Evidências locais ignoradas pelo Git: artifacts/block09/{unit,unit-final,focused,final,current-contracts}; ui para geração inicial, ui-final para restauração atual, ui-repeat para repetição; persistent-check.json para a conciliação. Testes técnicos continuam usando ADR-28, bancos descartáveis de fixture no mesmo Atlas; isso não cria um ambiente Demo da aplicação. CI existente usa MongoDB8.0.17/Testcontainers autenticado/replica set, sem Atlas, sem filtros ou testes ignorados.

## Comandos reais

- `dotnet restore`; `dotnet restore --locked-mode`; `dotnet build --no-restore -c Release -warnaserror`.
- `dotnet test -c Release --filter FullyQualifiedName~DemonstrationTests --logger trx --results-directory artifacts/block09/focused`.
- `dotnet test -c Release --no-build --filter 'Category!=ContainerSecurity&Category!=ContainerScale' --logger trx --results-directory artifacts/block09/final`.
- Builds com `-o .local/demo-*`; `dotnet test .local/demo-unit-final/GestaoComercial.UnitTests.dll --logger trx --results-directory artifacts/block09/unit-final`.
- `dotnet test .local/demo-tests-final/GestaoComercial.IntegrationTests.dll --filter 'FullyQualifiedName~DemonstrationBffTests|FullyQualifiedName~DemonstrationWebTests|FullyQualifiedName~DenyNonAdministratorsAndAnonymous' --logger trx --results-directory artifacts/block09/current-contracts`.
- `dotnet ...Api.dll --initialize-persistence --Mongo:Database gestao_comercial_dev` em Development; inicialização Web/bootstrapping normais na jornada.
- `node .local/demo-e2e.cjs`; `node .local/demo-restore-e2e.cjs`; `node .local/demo-repeat-e2e.cjs`; `dotnet run --project .local/demo-probe/Probe.csproj -c Release -p:BuildProjectReferences=false`.
- `dotnet package list --vulnerable --include-transitive --format json`; `node .local/scan-candidate.cjs artifacts/block09/gitleaks-final.json`; `node .local/check-demo-secrets.cjs`; `./scripts/verify-architecture.ps1`; `node .local/check-demo-docs.cjs`; `git diff --check`.

## Diagnósticos preservados

4 erros iniciais de overload DeleteMany corrigidos antes do primeiro build aprovado. Diferenças de EOF normalizadas. Script histórico validate-docs falhou por comparar uma baseline PostgreSQL/não-Markdown antiga; validação atual inventaria os arquivos reais e aprovou os links, sem alterar testes para esconder falha. Consulta opcional ListDatabases foi negada pela permissão Atlas, sem efeito: conferência usa somente o banco nomeado. Carga preliminar de um item (575 movimentos/12449573 centavos líquidos) foi restaurada explicitamente; seus valores não representam o cenário final.

## Arquivos, rastreio e DoD

Application: IDadosDemonstracao/resultado/estado sem driver. Infrastructure: gerador concreto e relógio de negócio escopado; autenticação usa relógio real. Api: EndpointsDemonstracao/composição/renovação normal de uso da sessão. Contracts: requests/responses HTTP. Web: página/menu/cliente, mensagem de sucesso, prazo15min somente para carga (demais chamadas10s), banco compartilhado e upgrade explícito do catálogo tickets2→8. Sem pacotes novos.

Testes: DemonstrationTests, DemonstrationBffTests, DemonstrationWebTests, ConfigurationTests e OpenAPI em HostTests. Documentação: AGENTS explícito do usuário, README/índice, módulo09, arquitetura01/02/03/05/06/07/10, execução00/01/02/03/04/07/12 e Compose com banco único. Documentação remota de requisitos/plano/10 foi preservada; não reintroduzir módulos removidos.

Rastreio: RF-03–05/07–19 e respectivos RN/CA são reutilizados, sem nova regra comercial. Aceites09: geração, quantidades, referências, repetição, restauração, conciliação, Dashboard/Relatórios e autorização demonstrados pelos testes/jornadas acima; CI atual fechou a regressão integral. Não alegar homologação10/backup/publicação. Próximo bloco10 exige nova solicitação, sem início nesta entrega.

## GitHub e gate final

Commit funcional c622648, [PR4](https://github.com/argemon/MVPMilton/pull/4) aberto. [CI PR42424883343](https://github.com/argemon/MVPMilton/actions/runs/42424883343) e [push42424854966](https://github.com/argemon/MVPMilton/actions/runs/42424854966) concluídos success em todas as etapas. Logs:126 unitários+116 integrações =242 aprovados,0 falhas/skips, sem filtros. Build0 avisos/erros, auditoria/scanner aprovados, artefato13595640050 confirmado. Isso inclui o teste atual de250 vendas com dois itens/inativos, reexecução/restauração, falha/rollback, concorrência e dependência manual. Sem pendência funcional09, deploy/merge ou início10.

### Inventário exato (37 arquivos)

- `AGENTS.md`
- `README.md`
- `backend/GestaoComercial.Api/Acesso/EndpointsDemonstracao.cs`
- `backend/GestaoComercial.Api/Program.cs`
- `backend/GestaoComercial.Application/DadosDemonstracao.cs`
- `backend/GestaoComercial.Infrastructure/DadosDemonstracao.cs`
- `backend/GestaoComercial.Infrastructure/Persistence/OperacoesCadastro.cs`
- `deploy/compose.yaml`
- `docs/README.md`
- `docs/architecture/01-VISAO-GERAL.md`
- `docs/architecture/02-ARQUITETURA.md`
- `docs/architecture/03-ESTRUTURA-MONOREPO.md`
- `docs/architecture/05-BANCO-DE-DADOS.md`
- `docs/architecture/06-CONTRATOS-API.md`
- `docs/architecture/07-AUTENTICACAO-SEGURANCA.md`
- `docs/architecture/10-DEPLOY-AMBIENTES.md`
- `docs/execution/00-PLANO-EXECUCAO.md`
- `docs/execution/01-DEPENDENCIAS.md`
- `docs/execution/02-DEFINITION-OF-DONE.md`
- `docs/execution/03-ESTADO-PROJETO.md`
- `docs/execution/04-DECISOES-TECNICAS.md`
- `docs/execution/07-EXECUCAO-LOCAL-FUNDACAO.md`
- `docs/execution/12-ENTREGA-CARGA-DEMONSTRACAO.md`
- `docs/modules/09-CARGA-DEMONSTRACAO.md`
- `frontend/GestaoComercial.Web/ApiClients/AcessoApiClient.cs`
- `frontend/GestaoComercial.Web/ApiClients/DemonstracaoApiClient.cs`
- `frontend/GestaoComercial.Web/Components/LayoutPrincipal.razor`
- `frontend/GestaoComercial.Web/Components/Pages/DadosDemonstracao.razor`
- `frontend/GestaoComercial.Web/Configuracao.cs`
- `frontend/GestaoComercial.Web/Program.cs`
- `frontend/GestaoComercial.Web/Security/MongoTickets.cs`
- `shared/GestaoComercial.Contracts/DadosDemonstracao.cs`
- `tests/GestaoComercial.IntegrationTests/DemonstrationBffTests.cs`
- `tests/GestaoComercial.IntegrationTests/DemonstrationTests.cs`
- `tests/GestaoComercial.IntegrationTests/DemonstrationWebTests.cs`
- `tests/GestaoComercial.UnitTests/ConfigurationTests.cs`
- `tests/GestaoComercial.UnitTests/HostTests.cs`
