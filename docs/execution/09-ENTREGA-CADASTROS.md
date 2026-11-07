# Entrega dos cadastros02–04 — 07/11/2026

## Escopo e sequência

Base Git05ee7c5, main, checkout inicialmente limpo. A primeira solicitação autorizou02/03; a complementar autorizou04 e publicação Git/PR/CI, sem deploy/merge/05. Gates foram sequenciais:02 com88 testes e E2E;03 com107 casos validados e E2E;04 aprovado com139 testes e E2E conforme estado03. .env existente preservado; dados de teste são sintéticos e bancos Atlas exclusivos são removidos pela fixture/roteiro.

## Funcionalidades e contratos

| Bloco | Entrega | API sob /api/v1 | UI |
| --- | --- | --- | --- |
|02|Funcionários, contato, versão, status, conta opcional, vínculo único, troca obrigatória, revogação atômica e último Admin protegido|GET/POST funcionarios; GET/PUT funcionarios/{id}; POST usuarios|/funcionarios, /funcionarios/novo, /funcionarios/{id}, /usuarios/novo e vínculo no usuário|
|03|Clientes PF/PJ, documento opcional com dígitos verificadores, busca/paginação/status, auditoria e histórico vazio tipado|GET/POST clientes; GET/PUT clientes/{id}; GET clientes/{id}/vendas|/clientes, /clientes/novo, /clientes/{id}|
|04|Produtos, categorias/marcas, SKU, preço exato, descrição, mínimo, saldo inicial zero, status e imagem privada|GET/POST categorias e marcas; GET/PUT categorias/{id} e marcas/{id}; GET/POST produtos; GET/PUT produtos/{id}; GET/POST/DELETE produtos/{id}/imagem|/produtos, /produtos/novo, /produtos/{id}, /categorias, /marcas; proxy /imagens/produtos/{id}|

Criações retornam201+Location; edições versionadas retornam200. Inexistente404; duplicidade/versão/referência em uso/inativa409; validação400; anônimo401; policy negada403. Upload rejeita formato415/tamanho413. Erros usam ProblemDetails seguro. DTOs HTTP não contêm documentos BSON ou credenciais de persistência. Criação de conta usa transporte privado dos hosts, sem senha em Shared Contracts ou resposta.

Somente Admin mantém funcionários/contas; Admin/Vendedor mantém clientes; três perfis consultam produtos/referências e Admin/Estoquista mantêm. Sem exclusão física comercial, conta de cliente, movimentação, venda, catálogo público ou relatório. RF-06 continua parcial planejado no03 e será preenchido somente no06.

## Persistência e segurança

02-employees/version3: employees com nome/id e users.employeeId unique parcial binData.03-customers/version4: customers com documento unique parcial string e nome/id.04-products/version5: categories/brands com nome normalizado único e nome/id; products com SKU único, nome/id e categoria/ativo/id, marca/ativo/id. Todos strict/error, UUID Standard, instantes UTC e versões Int64. Evoluções explícitas verificam definições/checksum, preservam dados e não rodam no startup. Aplicadas em Development pelo binário API --initialize-persistence; sem alteração do .env.

Conta humana/funcionário compartilham administração→usuário→funcionário→sessões; desativação revoga tudo no mesmo commit. Cliente/produto/referência e auditoria compartilham transação snapshot/majority/primary. Guardas efetivas nas referências impedem write skew; versão de cadastro é separada da guarda/saldo. Ator deriva da sessão, não do corpo. Preço decimal de borda vira centavos long checked; saldo criado zero e jamais editável pelo cadastro.

Imagem é validada por assinatura+SKCodec, dimensões antes da alocação, decode integral e encode de pixels em PNG sem metadados. Limites2MiB e4096×4096. SkiaSharp3.119.4 e assets Linux fixados e auditados. Diretório privado, chave UUID.png, sem nome/caminho de cliente, sem base64. Novo arquivo antes da transação, substituído removido somente após commit conhecido. Falhas mantêm arquivos; CLI --cleanup-orphan-images --maintenance exige esquema04 e escritas interrompidas, remove só chaves não referenciadas. Proxy Web valida cookie/policy e chama API por bearer de servidor.

## Verificações executadas

| Etapa | Comando/roteiro real | Resultado |
| --- | --- | --- |
|Dependências|dotnet restore GestaoComercial.slnx --locked-mode --disable-parallel -m:1 /nodeReuse:false|8 projetos; sem atualização indevida de versões. Restore sem locked foi executado ao introduzir SkiaSharp para gerar lockfiles, depois locked revalidado|
|Compilação|dotnet build GestaoComercial.slnx -c Release --no-restore -m:1 /nodeReuse:false -p:UseSharedCompilation=false|0 avisos/0 erros|
|Gate02|dotnet test GestaoComercial.slnx -c Release --no-build -m:1 --logger trx --results-directory artifacts/block02/final|47 unidade +41 integração,0 falhas/0 skips|
|Gate03|Suíte completa em artifacts/block03/tests; depois filtro CustomerBffTests em artifacts/block03/bff|58 unidade +48 integração na suíte, mais1 BFF,0 falhas/0 skips|
|04 regressão|Suíte completa em artifacts/block04/final|77 unidade +62 integração,139 aprovados/0 falhas/0 skips na repetição final artifacts/block04/verified, após o teste adicional de imagem|
|Arquitetura|scripts/verify-architecture.ps1|Referências válidas; frontend só Contracts; Driver/BSON fora de Domain/Application/Contracts|
|Dependências vulneráveis|dotnet package list --vulnerable --include-transitive --format json|0 pacotes vulneráveis reportados, artifacts/block04/vulnerabilities.json|
|Evoluções|dotnet backend/GestaoComercial.Api/bin/Release/net10.0/GestaoComercial.Api.dll --initialize-persistence com ambiente Development|02,03,04 aplicados explicitamente; exit0 e Atlas transacional confirmado|
|Interface02|.local/employees-e2e.cjs, bancos exclusivos, HTTPS confiado, Edge headless|Criar funcionário/conta SSR, desativar com confirmação, ativo/todos,360/1280 e teclado;0 JS/5xx|
|Interface03|.local/clients-e2e.cjs no mesmo perfil isolado|Cadastro/edição/desativação, CPF/tipo inválido e foco, histórico vazio, pesquisa por documento,360/1280;0 JS/5xx|
|Interface04|.local/products-e2e.cjs no mesmo perfil isolado|Categoria/marca/produto, preço pt-BR, validação e foco, saldo0, upload/proxy, referência em uso, busca SKU,360/1280;0 JS/5xx|

Capturas/relatórios locais em artifacts/block02/ui, block03/ui e block04/ui. Todos os bancos E2E removidos. Roteiros locais não são código de produção. Primeiro roteiro04 falhou por agir antes do rerender da categoria; espera corrigida e jornada inteira repetida/aprovada. Primeiro teste de contrato02 esperava POST de conta ausente: expectativa atualizada para o incremento; suite repetida. Tentativas de execução restritas falharam por DNS/comunicação local; comandos autorizados com acesso de rede e compilação sem servidor concluíram. Uma regressão posterior recebeu503 na disputa conta/desativação; o teste agora relê estado e repete PUT versionado no máximo2 vezes, exigindo desativação efetiva, sem considerar503 sucesso. Caso afetado e suíte completa repetidos/aprovados. O orçamento da aplicação não foi alterado. Nenhum teste foi desabilitado.

## Rastreabilidade e DoD

RF-03/04 e M02-CA01–05: EmployeeTests/EmployeeBffTests/ContactTests. RF-05/RF-06 parcial e M03-CA01–05: CustomerTests/CustomerBffTests/CustomerDocumentTests. RF-07/08 e M04-CA01–05: ProductTests/ProductBffTests/ProductsUpgradeTests/ProductRulesTests. Authentication/Administration/AuthAtomicity/Bff/Foundation/Host/Configuration continuam na suíte para regressões00/01.

Disputas usam requests, scopes e sessões independentes, com barreira no início; operações de uma sessão são sequenciais. Cobertos: conta/vínculo únicos, criação versus inativação, último Admin, documento/SKU/nome únicos, edição obsoleta, categoria versus criação, imagem versus imagem e falha deliberada de auditoria com rollback. BSON inválido/centavos double/saldo negativo são rejeitados diretamente no banco isolado. Auditoria e documentos anteriores preservados no upgrade. Nenhum teste usa substituto transacional em memória.

Q-01/02/03 continuam para homologação/dados reais/publicação operacional. Docker não foi usado localmente; Atlas isolado é o caminho autorizado. CI usa Testcontainers replica set sem .env/Atlas e deve ser consultada após o push. Backup/restore produtivo, carga09 e deploy10 não são requisitos executáveis deste incremento. Não alegar prontidão produtiva. Próximo bloco05 depende de nova autorização e gate04.

## Inventário e publicação

O inventário dos arquivos novos/alterados e os resultados finais de Git/CI estão no estado03. A publicação preserva o workflow funcional existente (.github/workflows/fundacao.yml): push/pull_request em Ubuntu, SDK fixo, restore locked, build Release, suíte real Testcontainers, auditoria, Gitleaks do snapshot e artefatos. Não há deploy ou merge automático. GitHub e resultado funcional são reportados separadamente.

## Arquivos criados/alterados nesta entrega

- `.env.example`
- `AGENTS.md`
- `Directory.Packages.props`
- `README.md`
- `backend/GestaoComercial.Api/Acesso/AutenticacaoHttp.cs`
- `backend/GestaoComercial.Api/Acesso/EndpointsAcesso.cs`
- `backend/GestaoComercial.Api/Acesso/EndpointsClientes.cs`
- `backend/GestaoComercial.Api/Acesso/EndpointsFuncionarios.cs`
- `backend/GestaoComercial.Api/Acesso/EndpointsProdutos.cs`
- `backend/GestaoComercial.Api/Program.cs`
- `backend/GestaoComercial.Api/packages.lock.json`
- `backend/GestaoComercial.Application/Acesso/IAutenticacao.cs`
- `backend/GestaoComercial.Application/Clientes/CadastroException.cs`
- `backend/GestaoComercial.Application/Clientes/IClientes.cs`
- `backend/GestaoComercial.Application/Funcionarios/IFuncionarios.cs`
- `backend/GestaoComercial.Application/Produtos/IProdutos.cs`
- `backend/GestaoComercial.Domain/Cadastros/Contato.cs`
- `backend/GestaoComercial.Domain/Cadastros/DadosProduto.cs`
- `backend/GestaoComercial.Domain/Cadastros/DocumentoCliente.cs`
- `backend/GestaoComercial.Infrastructure/Clientes/ClienteDocumento.cs`
- `backend/GestaoComercial.Infrastructure/Clientes/ClientesSchema.cs`
- `backend/GestaoComercial.Infrastructure/Clientes/MongoClientes.cs`
- `backend/GestaoComercial.Infrastructure/Funcionarios/FuncionarioDocumento.cs`
- `backend/GestaoComercial.Infrastructure/Funcionarios/FuncionariosSchema.cs`
- `backend/GestaoComercial.Infrastructure/GestaoComercial.Infrastructure.csproj`
- `backend/GestaoComercial.Infrastructure/Identity/MongoAutenticacao.cs`
- `backend/GestaoComercial.Infrastructure/Identity/MongoFuncionarios.cs`
- `backend/GestaoComercial.Infrastructure/Identity/RegistroAcesso.cs`
- `backend/GestaoComercial.Infrastructure/Persistence/OperacoesCadastro.cs`
- `backend/GestaoComercial.Infrastructure/Produtos/DocumentosProduto.cs`
- `backend/GestaoComercial.Infrastructure/Produtos/ImagensPrivadas.cs`
- `backend/GestaoComercial.Infrastructure/Produtos/MongoProdutos.cs`
- `backend/GestaoComercial.Infrastructure/Produtos/ProdutosSchema.cs`
- `backend/GestaoComercial.Infrastructure/packages.lock.json`
- `deploy/Api.Dockerfile`
- `deploy/compose.yaml`
- `docs/README.md`
- `docs/architecture/01-VISAO-GERAL.md`
- `docs/architecture/03-ESTRUTURA-MONOREPO.md`
- `docs/architecture/05-BANCO-DE-DADOS.md`
- `docs/architecture/07-AUTENTICACAO-SEGURANCA.md`
- `docs/execution/00-PLANO-EXECUCAO.md`
- `docs/execution/03-ESTADO-PROJETO.md`
- `docs/execution/04-DECISOES-TECNICAS.md`
- `docs/execution/09-ENTREGA-CADASTROS.md`
- `docs/modules/02-FUNCIONARIOS.md`
- `docs/modules/03-CLIENTES.md`
- `docs/modules/04-PRODUTOS.md`
- `docs/requirements/01-ESCOPO.md`
- `docs/requirements/06-CRITERIOS-ACEITE.md`
- `frontend/GestaoComercial.Web/ApiClients/AcessoApiClient.cs`
- `frontend/GestaoComercial.Web/ApiClients/ClientesApiClient.cs`
- `frontend/GestaoComercial.Web/ApiClients/FuncionariosApiClient.cs`
- `frontend/GestaoComercial.Web/ApiClients/ProdutosApiClient.cs`
- `frontend/GestaoComercial.Web/Components/LayoutPrincipal.razor`
- `frontend/GestaoComercial.Web/Components/Pages/ClienteDetalhe.razor`
- `frontend/GestaoComercial.Web/Components/Pages/Clientes.razor`
- `frontend/GestaoComercial.Web/Components/Pages/FuncionarioDetalhe.razor`
- `frontend/GestaoComercial.Web/Components/Pages/Funcionarios.razor`
- `frontend/GestaoComercial.Web/Components/Pages/ProdutoDetalhe.razor`
- `frontend/GestaoComercial.Web/Components/Pages/Produtos.razor`
- `frontend/GestaoComercial.Web/Components/Pages/Referencias.razor`
- `frontend/GestaoComercial.Web/Components/Pages/UsuarioDetalhe.razor`
- `frontend/GestaoComercial.Web/Components/Pages/UsuarioNovo.razor`
- `frontend/GestaoComercial.Web/Security/ContasHttp.cs`
- `frontend/GestaoComercial.Web/wwwroot/app.css`
- `shared/GestaoComercial.Contracts/Clientes.cs`
- `shared/GestaoComercial.Contracts/Funcionarios.cs`
- `shared/GestaoComercial.Contracts/Produtos.cs`
- `tests/GestaoComercial.IntegrationTests/AuthenticationTests.cs`
- `tests/GestaoComercial.IntegrationTests/CustomerBffTests.cs`
- `tests/GestaoComercial.IntegrationTests/CustomerTests.cs`
- `tests/GestaoComercial.IntegrationTests/EmployeeBffTests.cs`
- `tests/GestaoComercial.IntegrationTests/EmployeeTests.cs`
- `tests/GestaoComercial.IntegrationTests/FoundationTests.cs`
- `tests/GestaoComercial.IntegrationTests/ProductBffTests.cs`
- `tests/GestaoComercial.IntegrationTests/ProductTests.cs`
- `tests/GestaoComercial.IntegrationTests/ProductsUpgradeTests.cs`
- `tests/GestaoComercial.IntegrationTests/packages.lock.json`
- `tests/GestaoComercial.UnitTests/ContactTests.cs`
- `tests/GestaoComercial.UnitTests/CustomerDocumentTests.cs`
- `tests/GestaoComercial.UnitTests/HostTests.cs`
- `tests/GestaoComercial.UnitTests/ProductRulesTests.cs`
- `tests/GestaoComercial.UnitTests/packages.lock.json`

## GitHub e CI desta entrega

Commit de implementação:6d21cb3935ab39c19e49104d29175c63383bc48a, publicado em feature/block-04-products no repositório argemon/MVPMilton. [PR1](https://github.com/argemon/MVPMilton/pull/1) aberto para main, sem merge/deploy. CI do [push](https://github.com/argemon/MVPMilton/actions/runs/42346433471) e [PR](https://github.com/argemon/MVPMilton/actions/runs/42346477960) concluídas success em todas as etapas: build0 avisos/erros,77 unitários+62 integrações =139 aprovados/0 falhas/0 skips, dependências sem vulnerabilidades reportadas e scanner sem segredos. MongoDB8.0.17 replica set via Testcontainers e processamento de imagens Linux comprovados, sem Atlas/.env. Artefato evidencias-fundacao publicado. Nenhuma correção de CI necessária. Workflow existente preservado; nenhuma validação desabilitada. Snapshot exato do commit via git archive:170 arquivos/~861KB, Gitleaks8.24.2 com0 ocorrências. Um scan vazio causado por falha de cópia do índice foi descartado e refeito com contagem de arquivos e commit exato; não representa evidência de segurança.
