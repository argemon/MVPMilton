# GestaoComercial

Sistema web de gestão comercial desenvolvido como projeto de TCC de Engenharia de Software. A proposta é centralizar cadastros, estoque e vendas de uma empresa, com controle de acesso, histórico auditável, indicadores e relatórios.

> **Estágio atual:** blocos00–08 aprovados localmente; CI integral em acompanhamento. Dashboard com evolução/ranking/recentes e cinco relatórios paginados com exportação CSV segura. Estoque e vendas transacionais preservados. Evidências e limites no [estado](docs/execution/03-ESTADO-PROJETO.md).

## Objetivo

Entregar um MVP administrativo que permita cadastrar clientes, funcionários e produtos, controlar movimentações de estoque e registrar vendas com segurança e integridade dos dados. A confirmação de uma venda deve baixar o estoque de forma atômica; o cancelamento deve preservar a operação original e gerar um estorno rastreável.

O projeto prioriza uma solução adequada ao TCC, com separação de responsabilidades, testes das regras críticas e infraestrutura simples de operar.

## Escopo do MVP

| Área | Funcionalidades previstas |
| --- | --- |
| Autenticação e autorização | Login, logout, troca e redefinição de senha, usuários, perfis e revogação de sessões |
| Funcionários | Cadastro, edição, consulta, desativação e vínculo opcional com conta de acesso |
| Clientes | Cadastro PF/PJ, edição, pesquisa, desativação e histórico de vendas conforme permissão |
| Produtos | Cadastro, edição, categorias, marcas, preço, imagem, estoque mínimo e status |
| Estoque | Entradas, saídas manuais, ajustes justificados, consulta de saldo e histórico auditável |
| Vendas | Rascunho com cliente e itens, totalização, confirmação, baixa de estoque, consulta, descarte e cancelamento com estorno integral |
| Dashboard | Indicadores por período, ticket médio, canceladas, produtos cadastrados/ativos/estoque baixo, evolução, ranking e vendas recentes |
| Relatórios | Vendas por período e funcionário/operador, produtos mais vendidos, posição atual do estoque e movimentações; filtros, ordenação, paginação e CSV até 10.000 linhas |
| Qualidade e entrega | Testes, homologação, publicação, backup e restauração demonstrada |

Os requisitos estão identificados como **RF-01 a RF-20**. As prioridades P0 e P1 fazem parte da entrega completa do MVP.

## Usuários e permissões

| Perfil | Responsabilidade principal |
| --- | --- |
| Administrador | Gerenciar usuários, perfis e todas as operações disponíveis |
| Vendedor | Manter clientes, consultar produtos e saldos e operar as próprias vendas |
| Estoquista | Manter produtos, registrar entradas e saídas manuais e consultar histórico e relatórios de estoque |

As permissões são verificadas no backend. O acesso completo do administrador não permite ignorar regras de saldo, estados da venda ou preservação de histórico. Consulte a [matriz de permissões](docs/requirements/05-MATRIZ-PERMISSOES.md) para os limites de cada ação.

## Premissas e regras críticas

- Uma empresa por instalação e um saldo de estoque por produto.
- Estoque em unidades inteiras (`UN`), moeda BRL e interface administrativa responsiva em português do Brasil.
- Cliente identificado e ativo para a venda; produto inativo não pode ser adicionado a um novo rascunho nem confirmado.
- Valores monetários internos em centavos `long`/BSON Int64, moeda BRL; HTTP mantém decimais em reais com conversão exata. Preços com precisão excedente são rejeitados; médias usam arredondamento final `MidpointRounding.AwayFromZero`.
- Rascunhos não reservam estoque. Confirmar exige saldo existente e suficiente para todos os itens.
- Confirmação e baixa de estoque acontecem na mesma transação, protegidas contra concorrência e reprocessamento.
- Cancelamento exige autorização e motivo, mantém os registros originais e estorna cada item uma única vez.
- Cadastros com histórico são desativados, sem exclusão física de dados comerciais históricos.
- Tokens de acesso à API ficam nos servidores; segredos não são versionados.
- Confirmar uma venda não representa comprovação de pagamento ou emissão fiscal.

As premissas comerciais ainda sujeitas à validação estão identificadas na [definição de escopo](docs/requirements/01-ESCOPO.md) e no [registro de decisões e questões abertas](docs/execution/04-DECISOES-TECNICAS.md).

## Fora do escopo do MVP

- Emissão de documentos fiscais e integrações fiscais.
- Pagamentos online, caixa financeiro, contas a pagar/receber e conciliação financeira.
- Compras, fornecedores, comissões e cálculo de lucro.
- Múltiplas empresas, múltiplos depósitos e estoque fracionado.
- Reserva de estoque, descontos, frete, devoluções parciais e sincronização offline.
- Catálogo público e pedidos online, que pertencem a uma fase posterior.

## Fase posterior: catálogo e pedidos online

O [módulo 11](docs/modules/11-CATALOGO-PEDIDOS.md) contém uma **proposta P2**, ainda condicionada à definição e aceitação do escopo: publicar produtos selecionados e receber solicitações anônimas de pedido para atendimento interno.

Nesse recorte proposto, a solicitação não reserva nem consome estoque, não confirma venda e não processa pagamento ou entrega. Os requisitos RF-21 a RF-24 são propostas dessa fase e não ampliam o MVP interno.

## Tecnologias e arquitetura previstas

| Componente | Escolha |
| --- | --- |
| Linguagem e plataforma | C# e .NET 10 LTS |
| Backend | ASP.NET Core Web API |
| Frontend | Blazor Web App, inicialmente Interactive Server |
| Persistência | MongoDB e MongoDB.Driver oficial; documentos, índices/validadores e transações em replica set |
| Testes | xUnit e Testcontainers com MongoDB real em replica set para integração, integridade e concorrência |
| Execução e publicação | Docker Compose |
| Organização | Monorepo com `frontend/` e `backend/` fisicamente separados |

A API segue Clean Architecture com **Domain, Application, Infrastructure e Api**. O frontend consome contratos HTTP e referencia somente contratos de transporte compartilhados, sem acessar o banco comercial. A autenticação usa o servidor Blazor como intermediário (BFF), com cookie de referência no navegador e credenciais da API protegidas no servidor.

A implementação seguirá SOLID, KISS, DRY e injeção de dependências. Microsserviços, CQRS/MediatR, mensageria, Event Sourcing e Generic Repository não fazem parte da arquitetura definida.

## Ordem de implementação

Cada bloco deve cumprir seus testes, critérios de aceite e Definition of Done antes de liberar o seguinte.

| Bloco | Entrega |
| --- | --- |
| [00](docs/modules/00-FUNDACAO.md) | Fundação da solução, configuração, banco e execução local |
| [01](docs/modules/01-AUTENTICACAO.md) | Autenticação e autorização |
| [02](docs/modules/02-FUNCIONARIOS.md) | Funcionários e criação de contas vinculadas |
| [03](docs/modules/03-CLIENTES.md) | Clientes |
| [04](docs/modules/04-PRODUTOS.md) | Produtos, categorias, marcas e imagens |
| [05](docs/modules/05-ESTOQUE.md) | Estoque e movimentações |
| [06](docs/modules/06-VENDAS.md) | Vendas e cancelamentos |
| [07](docs/modules/07-DASHBOARD.md) | Dashboard |
| [08](docs/modules/08-RELATORIOS.md) | Relatórios |
| [09](docs/modules/09-HOMOLOGACAO.md) | Homologação e qualidade |
| [10](docs/modules/10-PUBLICACAO.md) | Publicação e entrega do MVP |
| [11](docs/modules/11-CATALOGO-PEDIDOS.md) | Catálogo e pedidos online — fase posterior condicionada à liberação de escopo |

Não há operações de estoque antes de Produtos nem vendas antes de Estoque. Os incrementos planejados permitem validar cada bloco sem depender de software de um bloco futuro.

## Documentação

- [Índice completo](docs/README.md)
- [Visão geral](docs/architecture/01-VISAO-GERAL.md) e [arquitetura](docs/architecture/02-ARQUITETURA.md)
- [Escopo](docs/requirements/01-ESCOPO.md), [requisitos funcionais](docs/requirements/02-REQUISITOS-FUNCIONAIS.md) e [regras de negócio](docs/requirements/04-REGRAS-DE-NEGOCIO.md)
- [Plano de execução](docs/execution/00-PLANO-EXECUCAO.md) e [matriz consolidada dos módulos](docs/execution/06-MATRIZ-MODULOS.md)
- [Estado atual](docs/execution/03-ESTADO-PROJETO.md) e [decisões técnicas](docs/execution/04-DECISOES-TECNICAS.md)
- [Definition of Done](docs/execution/02-DEFINITION-OF-DONE.md) e [instruções para agentes](AGENTS.md)

## Execução local

A fundação atual usa a conexão Atlas já fornecida no .env local. Preserve esse arquivo, que está ignorado pelo Git; variáveis do processo prevalecem. Não precisa iniciar outro MongoDB. Veja as etapas abaixo e o [roteiro detalhado](docs/execution/07-EXECUCAO-LOCAL-FUNDACAO.md).

Antes de publicar com dados reais, será necessário definir o ambiente de hospedagem, os responsáveis pela operação e o tratamento e a retenção de dados pessoais, além de demonstrar backup e restauração.

## Revisão de persistência

A auditoria documental06 registrou um bloqueio histórico, superado pela conversão autorizada00. As evidências vigentes dos blocos00/01 constam no [estado](docs/execution/03-ESTADO-PROJETO.md).

Arquitetura atual: MongoDB.Driver oficial, IMongoClient reutilizado, produtos com saldo incorporado e vendas com itens incorporados; movimentos/auditoria separados. BFF/cookie/sessão opaca permanecem, com stores Identity implementados sobre o driver. Não há reserva, novo requisito comercial ou mudança de módulos. Decisões/incompatibilidades em [ADR-20–25](docs/execution/04-DECISOES-TECNICAS.md) e [auditoria documental](docs/execution/08-REVISAO-MONGODB.md). A revisão documental histórica não implementou blocos; a implementação atual cobre00 e01.

## Executar a fundação com o Atlas existente

Pré-requisitos: SDK10.0.401 (global.json), acesso de rede permitido no Atlas e certificado de desenvolvimento HTTPS válido/confiado. A URI deve suportar replica set/sessões/transações; o nome do replica set é descoberto, sem fixar rs0. .env permanece local e nunca é copiado para imagens. Não copie .env.example sobre um .env existente. Um checkout novo deve preencher sua própria configuração externamente; o exemplo é apenas ilustrativo.

Na raiz, preparar a solução e verificar a conexão sem imprimir URI:

```powershell
dotnet restore --locked-mode
dotnet build --no-restore -c Release
./scripts/verify-architecture.ps1
dotnet run --project backend/GestaoComercial.Api -c Release --no-build -- --probe-mongo
```

Executar uma vez (reexecução idempotente é permitida) antes dos hosts:

```powershell
dotnet run --project backend/GestaoComercial.Api -c Release --no-build -- --initialize-persistence
dotnet run --project frontend/GestaoComercial.Web -c Release --no-build -- --initialize-persistence
```

A evolução00 cria persistence_versions; a01 cria users, api_sessions, audit_events e administration_control na API, e tickets no Web. Não há funcionários, estoque ou vendas. Inicializadores explícitos conferem definições/checksum e falham sem sobrescrever incompatibilidades; startup normal não faz DDL. Em Production/Staging, fornecer Evolution__ConnectionString externo e credenciais runtime específicas, sem fallback do .env.

Em dois terminais:

```powershell
dotnet run --project backend/GestaoComercial.Api -c Release --no-build
```

```powershell
dotnet run --project frontend/GestaoComercial.Web -c Release --no-build
```

Abrir https://localhost:7243. API live: https://localhost:7143/health/live; readiness API: https://localhost:8081/health/ready; readiness Web: https://localhost:8082/health/ready. Listeners locais ficam em localhost; readiness na porta pública retorna404. Web chama API no servidor por HTTPS. Certificado inválido deve ser confiado/corrigido na instalação; não há bypass TLS. URL/tamanho de timeouts são configuração técnica, sem tokens/credenciais no navegador.

## Testes isolados

```powershell
dotnet test -c Release --no-build --logger trx --results-directory artifacts/tests
```

UnitTests inclui hosts/configuração/hash/expiração/retorno seguro/negação de acesso. IntegrationTests inclui autenticação real e BFF (CSRF, isolamento, revogação, lockout, concorrência e rollback), além da regressão da fundação. IntegrationTests usa TEST_MONGODB_URI quando fornecido; senão lê a URI existente do .env exclusivamente para a fixture, sem carregar .env em hosts Testing/Production. Reserva nome aleatório de36 caracteres gc_m00_ +24 hex +_test, verifica banco vazio e limpa somente esse banco ao terminar. Atlas Free limita nomes a38 bytes; configuração adota esse limite conservador. Nenhuma coleção de produção é usada. Testes reais comprovam init idempotente, índice/validador, checksum incompatível preservado, commit/abort em duas coleções e HTTP/cliente tipado com readiness real.

Perfil opcional de CI/local: TEST_MONGO_CONTAINER=true usa Testcontainers.MongoDb com mongo:8.0.17 em rs0, porta dinâmica e conexão direta somente da fixture. Requer Docker e não é ativado automaticamente quando Atlas está disponível. Sem conexão de teste válida, falhar explicitamente; nenhum skip silencioso. .env e resultados locais ficam ignorados pelo Git.

## Compose opcional com banco externo

deploy/compose.yaml cria somente API/Web, reutilizando a URI externa; não possui serviço de banco. Fornecer CERT_DIR e TLS_PASSWORD por ambiente, sem alterar credenciais .env. CERT_DIR contém local.pfx (SAN api/localhost) e ca.crt; Web confia nessa CA apenas no HttpClient interno, preservando trust público do Atlas. Chaves técnicas Web em volume privado com owner app. Exemplos de comandos, quando Docker estiver disponível:

```powershell
docker compose --env-file .env -f deploy/compose.yaml build
docker compose --env-file .env -f deploy/compose.yaml run --rm api --initialize-persistence
docker compose --env-file .env -f deploy/compose.yaml run --rm web --initialize-persistence
docker compose --env-file .env -f deploy/compose.yaml up -d
```

Compose não foi executado nesta máquina (Docker ausente); o caminho local validado é dotnet+Atlas existente. A [CI no GitHub passou](https://github.com/argemon/MVPMilton/actions/runs/42330792233), com build Release,72 testes incluindo MongoDB real em replica set via Testcontainers, auditoria de dependências e Gitleaks sobre os arquivos do commit. Nenhuma credencial Atlas é fornecida ao Actions. Produção permanece no bloco10 e exige usuários de banco separados, permissões mínimas, certificados/backup e Q-02/Q-03; a credencial única de desenvolvimento não comprova isolamento de privilégios de produção.

## Primeiro administrador e acesso

Execute a inicialização explícita de ambos os hosts antes do bootstrap. A senha de aplicação é independente da credencial MongoDB; nenhuma senha padrão foi criada. No PowerShell, receba a senha sem ecoar e passe-a somente por ambiente:

```powershell
$env:Bootstrap__Nome = Read-Host 'Nome do administrador'
$env:Bootstrap__Email = Read-Host 'Email de login'
$senhaAdmin = Read-Host 'Senha temporária (12–128 caracteres, maiúscula/minúscula/número/símbolo)' -AsSecureString
$env:Bootstrap__Senha = [System.Net.NetworkCredential]::new('', $senhaAdmin).Password
try {
    dotnet run --project backend/GestaoComercial.Api -c Release --no-build -- --bootstrap-admin
} finally {
    Remove-Item Env:Bootstrap__Senha, Env:Bootstrap__Email, Env:Bootstrap__Nome -ErrorAction SilentlyContinue
    $senhaAdmin.Dispose()
}
```

A tarefa só cria Admin se nenhum existir e nunca redefine uma conta existente. Ao abrir https://localhost:7243, entre e conclua a troca obrigatória; todas as sessões serão revogadas e será necessário entrar novamente. O Administrador pode consultar contas, alterar perfil/status e redefinir senha; criação de novas contas humanas será entregue no02. Vendedor/Estoquista não administram usuários.

Não altere o .env existente para testar: forneça configurações específicas no processo. Api__BaseUrl=https://localhost:7143/ e Api__HealthBaseUrl=https://localhost:8081/ já estão no perfil Web; Compose usa8443/8081 internos. Data Protection persiste em .local/keys no desenvolvimento e em volume privado no Compose. Perder as chaves invalida tickets. Cookies são Secure/HttpOnly/Lax; todos os POSTs de conta exigem antiforgery. Não há JWT nem tokens em storage do navegador.

Testes usam somente bancos próprios verificados vazios, com limpeza limitada ao nome reservado. Resultados detalhados: [Bloco01](docs/execution/03-ESTADO-PROJETO.md). Docker/Testcontainers/CI continuam perfis preparados, sem execução local nesta máquina. RBAC por credenciais de banco separadas, proteção operacional do key ring, publicação/backup e Q-02/Q-03 permanecem gates de produção.

## Cadastros e imagens (02–04)

Rotas: /funcionarios, /usuarios/novo, /clientes, /produtos, /categorias e /marcas. Cada cadastro possui consulta/edição e desativação lógica. Apenas Administrador mantém funcionários/contas; Administrador/Vendedor mantêm clientes; Administrador/Estoquista mantêm produtos e referências. Vendedor consulta produtos. Clientes têm histórico vazio tipado nesta etapa; vendas chegam somente no06.

A tarefa explícita --initialize-persistence aplica as evoluções00–06; nunca ocorre alteração de schema no startup. Development reutiliza o .env ignorado, sem sobrescrevê-lo. Testes usam bancos descartáveis exclusivos. Senhas de contas humanas não aparecem em Shared Contracts nem são devolvidas pela API.

Images__RootPath configura o diretório privado da API, obrigatório fora de Development/Testing. Development usa .local/images; Compose usa volume productimages em /app/images, com acesso do usuário app. Backup deve incluir esse volume. Nunca montar o diretório no Web/wwwroot. JPEG/PNG: até2MiB e4096×4096; conteúdo decodificado/reencodado em PNG sem metadados. Atualizar cadastro não altera saldo, cuja criação é zero.

Para remover órfãos, interrompa todas as escritas/API e faça backup de banco+imagens. No mesmo ambiente e diretório configurados, execute o binário API com --cleanup-orphan-images --maintenance. O comando remove apenas nomes UUID.png não referenciados pelo banco selecionado; não limpa no startup, não apaga diretórios e não altera metadados comerciais. Nunca executar apontando para banco diferente do volume. Falha de commit mantém arquivos para essa manutenção, preservando a imagem anterior.

Entrega de Estoque/Vendas: [evidências e pendências](docs/execution/10-ENTREGA-ESTOQUE-VENDAS.md). Para o perfil Atlas, execute testes com `--filter "Category!=ContainerSecurity&Category!=ContainerScale"`; o CI executa a suíte integral com MongoDB autenticado isolado, incluindo restrições de coleção. Não usar a conta Atlas de desenvolvimento no CI.

As evoluções05/06 foram validadas em bancos isolados; a aplicação em `gestao_comercial_dev` aguarda autorização explícita após bloqueio da revisão automática. O `.env` e o banco persistente permanecem preservados.

Dashboard e relatórios: [contratos, validação e limites](docs/execution/11-ENTREGA-DASHBOARD-RELATORIOS.md). PDF fora do escopo aprovado. Próximo bloco09-HOMOLOGACAO exige nova solicitação; sem deploy ou merge automático.
