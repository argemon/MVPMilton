# GestaoComercial

Sistema web de gestão comercial desenvolvido como Trabalho de Conclusão de Curso de Engenharia de Software. Reúne cadastros, estoque, vendas, indicadores e relatórios em uma aplicação administrativa em português do Brasil.

A aplicação é executada **localmente na máquina do apresentador**, com conexão ao **MongoDB Atlas existente**. A demonstração depende de internet; não existe hospedagem pública, catálogo público ou pedidos online.

> **Estado da entrega:** blocos 00 a 10 concluídos e homologados. O CI da entrega aprovou **242 testes: 126 unitários e 116 de integração**, sem falhas ou testes ignorados; build Release sem avisos ou erros. Esses números pertencem à homologação registrada, não a uma nova execução desta revisão documental. Consulte o [estado do projeto](docs/execution/03-ESTADO-PROJETO.md) e o [relatório de homologação](docs/execution/07-RELATORIO-HOMOLOGACAO.md).

## Conteúdo

- [Funcionalidades](#funcionalidades)
- [Perfis de acesso](#perfis-de-acesso)
- [Regras importantes](#regras-importantes)
- [Tecnologias e organização](#tecnologias-e-organização)
- [Como executar localmente](#como-executar-localmente)
- [Login e contas de acesso](#login-e-contas-de-acesso)
- [Dados de demonstração](#dados-de-demonstração)
- [Fluxo sugerido para usar o sistema](#fluxo-sugerido-para-usar-o-sistema)
- [Testes e validação](#testes-e-validação)
- [Solução de problemas](#solução-de-problemas)
- [Limites do projeto](#limites-do-projeto)
- [Documentação](#documentação)

## Funcionalidades

| Módulo | Recursos disponíveis |
| --- | --- |
| Autenticação | Login, logout, troca de senha, redefinição administrativa de senha, expiração e revogação de sessões |
| Usuários | Contas vinculadas a funcionários, atribuição de perfil, ativação/desativação e troca obrigatória de senha temporária |
| Funcionários | Cadastro, consulta, edição, pesquisa, paginação, desativação e reativação; vínculo opcional com conta de acesso |
| Clientes | Cadastro de pessoa física ou jurídica, CPF/CNPJ opcional com validação quando informado, controle de duplicidade, contatos, pesquisa, edição, ativação/desativação e histórico de vendas |
| Categorias e marcas | Cadastro, consulta, pesquisa, edição e ativação/desativação, com proteção das referências utilizadas por produtos ativos |
| Produtos | SKU único, nome, descrição, categoria, marca opcional, preço de venda, estoque mínimo, imagem privada, pesquisa, paginação e ativação/desativação |
| Estoque | Consulta de saldos, filtro de estoque baixo, entradas, saídas manuais, ajustes justificados e histórico de movimentações com responsável, data e saldos anterior/posterior |
| Vendas | Rascunho com cliente e múltiplos produtos, quantidades, preços calculados no servidor, edição, confirmação com baixa de estoque, consulta, descarte de rascunho e cancelamento com estorno |
| Dashboard | Faturamento líquido, vendas confirmadas e canceladas, ticket médio, produtos cadastrados/ativos, estoque baixo, evolução diária, produtos mais vendidos e vendas recentes |
| Relatórios | Vendas por período, vendas por funcionário/operador, produtos mais vendidos, posição atual do estoque e movimentações; filtros, ordenação, paginação, totalizadores e exportação CSV |
| Dados de demonstração | Geração, retomada, repetição sem duplicação e restauração confirmada da carga fictícia no banco existente |

A navegação é organizada em menu lateral no desktop e menu recolhível em telas menores. As telas principais foram revisadas em 1920×1080, 1366×768, 768px e 390px. Tabelas extensas possuem rolagem própria quando necessário.

### Imagens e exportações

- Imagens de produtos: JPEG ou PNG, até **2 MiB** e **4096×4096 pixels**, validadas e reencodadas; acesso exige autenticação.
- Relatórios: CSV UTF-8, separado por ponto e vírgula, com datas ISO e valores monetários em reais com ponto decimal no arquivo.
- A exportação inclui **todo o filtro aplicado**, até **10.000 linhas**, e não apenas a página exibida. Acima desse limite, refine os filtros.
- Relatórios de estoque mostram a posição atual, não o saldo de uma data passada.

## Perfis de acesso

Cada conta possui um dos três perfis fixos. As permissões são verificadas na API, inclusive em acessos diretos por URL.

| Operação | Administrador | Vendedor | Estoquista |
| --- | --- | --- | --- |
| Gerenciar funcionários e usuários | Sim | Não | Não |
| Manter clientes | Sim | Sim | Não |
| Consultar produtos, categorias, marcas e saldos | Sim | Sim | Sim |
| Manter produtos, categorias, marcas e imagens | Sim | Não | Sim |
| Registrar entradas e saídas manuais | Sim | Não | Sim |
| Consultar histórico de movimentações | Sim | Não | Sim |
| Ajustar estoque | Sim | Não | Não |
| Criar, editar, confirmar e consultar vendas | Todas | Somente as próprias | Não |
| Cancelar vendas | Sim | Não | Não |
| Consultar Dashboard e relatórios comerciais | Sim | Não | Não |
| Consultar/exportar relatórios de estoque e movimentos | Sim | Não | Sim |
| Gerar ou restaurar carga demonstrativa | Sim | Não | Não |

O histórico de vendas do cliente também respeita a propriedade das vendas para o Vendedor. Funcionário sem conta não tem acesso ao sistema. O último administrador ativo é protegido contra desativação ou rebaixamento. Detalhes na [matriz de permissões](docs/requirements/05-MATRIZ-PERMISSOES.md).

## Regras importantes

- Uma empresa, um estoque e quantidades em unidades inteiras (`UN`); moeda **BRL**.
- Produto novo começa com saldo zero. Use uma entrada para registrar mercadoria disponível.
- Rascunho de venda **não reserva estoque**. A confirmação exige cliente e produtos ativos, preços válidos e saldo suficiente para todos os itens.
- Confirmação e baixa ocorrem na mesma transação: uma falha não pode deixar uma venda parcialmente confirmada ou parte do estoque baixada.
- Estoque nunca pode ficar negativo. Operações repetidas são protegidas contra efeitos duplicados.
- Cancelamento exige Administrador e justificativa; mantém a venda e a baixa originais e registra o estorno uma única vez.
- Movimentos e histórico comercial não são editados ou apagados. Cadastros com histórico são desativados.
- Dinheiro é calculado internamente em centavos inteiros, com conversão exata na entrada/saída. Preço com fração de centavo é rejeitado.
- Indicadores comerciais excluem a contribuição de vendas canceladas; o valor original continua visível no histórico.
- Nos filtros de período, a data inicial é **inclusiva** e a final é **exclusiva**, com intervalo máximo de 366 dias e fuso `America/Cuiaba` na instalação atual.
- Confirmar uma venda não representa recebimento de pagamento ou emissão de documento fiscal.

## Tecnologias e organização

| Componente | Tecnologia |
| --- | --- |
| Plataforma | C# / .NET 10 LTS |
| API | ASP.NET Core Web API |
| Interface | Blazor Web App com Interactive Server |
| Persistência | MongoDB Atlas e MongoDB.Driver oficial; transações em topologia compatível |
| Autenticação | ASP.NET Core Identity com armazenamento MongoDB e sessões opacas; Blazor atua como intermediário da API (BFF) |
| Testes | xUnit, MongoDB real e Testcontainers no perfil de integração do CI |
| Integração contínua | GitHub Actions: arquitetura, restore, build, testes, auditoria de dependências e scanner de segredos |

```text
GestaoComercial.slnx
backend/
  GestaoComercial.Domain/          # Entidades e regras de negócio
  GestaoComercial.Application/     # Casos de uso e interfaces
  GestaoComercial.Infrastructure/  # MongoDB, Identity, transações e arquivos
  GestaoComercial.Api/             # Endpoints HTTP e composição
frontend/
  GestaoComercial.Web/             # Interface Blazor e comunicação com a API
shared/
  GestaoComercial.Contracts/       # DTOs e contratos HTTP
build/                            # Configuração compartilhada dos hosts
scripts/                          # Verificação de arquitetura
tests/
  GestaoComercial.UnitTests/
  GestaoComercial.IntegrationTests/
docs/                             # Requisitos, arquitetura e evidências
```

As dependências seguem **Domain ← Application ← Infrastructure**, com composição na API. O frontend referencia somente os contratos compartilhados; não acessa as coleções de negócio. Seu acesso MongoDB é restrito ao armazenamento técnico das sessões Web. Tokens da API permanecem nos servidores, e os formulários de conta usam proteção antifalsificação.

## Como executar localmente

### 1. Pré-requisitos

- **SDK .NET 10.0.401**, versão fixada em [global.json](global.json), com `rollForward` desabilitado.
- Navegador atualizado, como Microsoft Edge, Chrome ou Firefox.
- Internet e acesso permitido ao cluster Atlas existente.
- Arquivo `.env` privado na raiz, com a conexão já configurada.
- Certificado HTTPS de desenvolvimento confiável na máquina.

**Docker não é necessário para executar a apresentação com o Atlas.** Ele é utilizado pelo perfil técnico de testes de integração do CI. Node.js também não é necessário para iniciar a aplicação.

Os comandos abaixo são para PowerShell, executados na raiz do repositório:

```powershell
cd D:\Projetos\MVPMilton
dotnet --version
dotnet dev-certs https --trust
dotnet restore --locked-mode
dotnet build --configuration Release --no-restore
```

Se o projeto estiver em outra pasta, ajuste apenas o caminho do primeiro comando.

### 2. Configuração do banco e dos arquivos

Em `Development`, os hosts leem automaticamente o `.env` da raiz. **Não sobrescreva o arquivo existente nem copie suas credenciais para o README, comandos de linha ou Git.** A aplicação e os tickets Web utilizam o mesmo banco `gestao_comercial_dev`.

| Configuração | Finalidade |
| --- | --- |
| `MONGODB_URI` | Conexão privada com o Atlas existente, lida do `.env` |
| `Mongo__ConnectionString` | Sobrescrita opcional da conexão por variável de processo; não é necessária na configuração local atual |
| `Mongo__Database` | Nome lógico do banco; o padrão atual é `gestao_comercial_dev` nos dois hosts |
| `Images__RootPath` | Diretório privado e persistente das imagens; definido no terminal da API abaixo |
| `Api__BaseUrl` | Endereço da API; o perfil `https` do frontend já configura `https://localhost:7143/` |
| `Api__HealthBaseUrl` | Verificação interna da API; o perfil do frontend já configura `https://localhost:8081/` |
| `DataProtection__KeysPath` | Diretório persistente das chaves que protegem as sessões Web; por padrão, `.local/keys` no diretório do projeto frontend |

Variáveis de processo prevalecem sobre os valores do `.env`. Os nomes com `__` representam a configuração por variável de ambiente dos hosts.

Para uma cópia nova do repositório, [.env.example](.env.example) é apenas uma referência de formato: os valores são ilustrativos e não permitem acesso ao banco. Obtenha a configuração privada do responsável pelo projeto e utilize o **Atlas existente**, mantendo o mesmo banco nos dois hosts. O comentário histórico sobre banco Web separado nesse exemplo não descreve a configuração vigente; prevalecem os padrões acima e a ADR-37.

O banco e o administrador da instalação de apresentação **já foram inicializados**. A execução normal não exige repetir bootstrap, inicializadores ou restauração da carga. Preparação de uma instalação ainda não inicializada é uma tarefa explícita, descrita no [guia da fundação](docs/execution/07-EXECUCAO-LOCAL-FUNDACAO.md) e no [registro de decisões](docs/execution/04-DECISOES-TECNICAS.md); não ocorre automaticamente ao iniciar os hosts.

### 3. Iniciar a API — terminal 1

```powershell
cd D:\Projetos\MVPMilton
$env:Images__RootPath = Join-Path (Get-Location) '.local/images'
dotnet run --project backend/GestaoComercial.Api --configuration Release --no-build --launch-profile https
```

Mantenha esse terminal aberto. Use sempre o mesmo diretório de imagens entre as execuções: os metadados ficam no MongoDB, mas os arquivos ficam nesse diretório local.

### 4. Iniciar o Blazor — terminal 2

```powershell
cd D:\Projetos\MVPMilton
dotnet run --project frontend/GestaoComercial.Web --configuration Release --no-build --launch-profile https
```

Mantenha esse terminal aberto e acesse **[https://localhost:7243](https://localhost:7243)**.

| Serviço | Endereço local |
| --- | --- |
| Interface Blazor | [https://localhost:7243](https://localhost:7243) |
| API | [https://localhost:7143](https://localhost:7143) — endpoints em `/api/v1` |
| Processo da API ativo | [https://localhost:7143/health/live](https://localhost:7143/health/live) |
| API pronta e persistência inicializada | [https://localhost:8081/health/ready](https://localhost:8081/health/ready) |
| Web pronta e persistência técnica inicializada | [https://localhost:8082/health/ready](https://localhost:8082/health/ready) |

As portas 8081 e 8082 são destinadas às verificações internas de prontidão. `/health/live` confirma o processo ativo; `/health/ready` confere a prontidão da persistência. Consultar readiness na porta pública retorna 404 por projeto.

### 5. Encerrar ou recompilar

Pressione **Ctrl+C nos dois terminais** para encerrar. Para atualizar o código e recompilar, pare os hosts, execute novamente o build e reinicie API e Blazor. Encerrar os processos não apaga os dados do Atlas nem as imagens.

## Login e contas de acesso

Não existe cadastro público nem senha padrão publicada. Use a conta administrativa já provisionada pelo responsável.

Na máquina preparada para a apresentação, os dados privados de acesso estão em `.local/demo-access.json`; contas fictícias adicionais da homologação estão em `.local/block10-roles.json`. Esses arquivos são **locais e ignorados pelo Git**, portanto não acompanham um clone novo. Não exiba seu conteúdo durante a apresentação.

Para conceder acesso a outra pessoa fictícia da demonstração:

1. Entre como Administrador e cadastre um funcionário ativo.
2. Na ficha, escolha **Criar conta de acesso**.
3. Informe email de login, perfil e senha temporária.
4. No primeiro login, conclua a troca obrigatória de senha e entre novamente.

Senhas têm de 12 a 128 caracteres, com maiúscula, minúscula, número e símbolo. Após cinco tentativas incorretas, a conta é bloqueada por 15 minutos. A sessão expira após 30 minutos de inatividade ou 8 horas totais; nesse caso, faça login novamente.

Desativar um funcionário também desativa a conta vinculada e revoga suas sessões. Reativar o funcionário **não reativa automaticamente a conta**; o Administrador faz isso separadamente em Usuários. Redefinição administrativa de senha é feita pelo sistema, sem envio automático de email.

## Dados de demonstração

Entre como Administrador e abra **Administração → Dados de Demonstração**. A tela informa o banco atual e a última carga concluída.

| Conjunto gerado | Quantidade da carga-base |
| --- | --- |
| Funcionários e respectivas contas | 12 de cada, com os três perfis e estados ativo/inativo |
| Clientes fictícios | 40, dos quais 5 sem compras |
| Produtos | 100, incluindo 5 inativos e 10 com saldo zero |
| Categorias e marcas | 5 de cada |
| Vendas com dois itens | 250: 225 confirmadas e 25 canceladas |
| Movimentações de estoque | 850, incluindo baixas e estornos |

Essas são as quantidades **da carga**, não o total obrigatório do banco. O administrador inicial e os registros manuais de homologação, identificados com `HOMO10`, também permanecem na instalação.

- **Gerar carga:** cria ou retoma o cenário; repetir não duplica os registros já gerados.
- **Restaurar carga:** exige confirmação, verifica a procedência dos dados e recria o cenário sintético. Alterações, dependências manuais ou sessões de usuários demonstrativos podem impedir a restauração; nesse caso, nenhum registro é removido pela etapa de restauração.
- A geração/restauração pode levar vários minutos. Aguarde o resultado; não inicie outra execução para tentar acelerar.
- As senhas dos usuários gerados são aleatórias e não são exibidas. Para usar essas contas, redefina a senha pelo fluxo administrativo.
- Para conferir todo o histórico da carga, utilize **01/08/2026 a 08/11/2026**, com a data final exclusiva. As datas são fixas do cenário, não são atualizadas automaticamente para o mês corrente.

Para a banca, utilize a carga já disponível e um produto de teste próprio nas movimentações. Alterar produtos demonstrativos pode impedir sua restauração posterior. Não é necessário restaurar o cenário a cada início da aplicação.

## Fluxo sugerido para usar o sistema

1. Entre como Administrador e confira funcionários e contas de acesso.
2. Cadastre um cliente, uma categoria, uma marca e um produto de teste.
3. Registre **entrada de 20 unidades** e confira o saldo.
4. Crie uma venda de **3 unidades**, revise os valores e confirme.
5. Confira o saldo **17** e a movimentação de baixa.
6. Cancele a venda com justificativa e confirme o saldo **20**, com o histórico preservado.
7. Abra Dashboard e Relatórios no mesmo período; compare os totalizadores.
8. Exporte um CSV e confira os dados.

O [relatório de homologação](docs/execution/07-RELATORIO-HOMOLOGACAO.md) contém o roteiro de apresentação de 12 minutos e o checklist para a banca: internet, Atlas, certificado, hosts iniciados, login válido, carga disponível e navegador preparado.

## Testes e validação

Para executar apenas os testes unitários localmente, após o build:

```powershell
dotnet test tests/GestaoComercial.UnitTests --configuration Release --no-build
```

Para verificar as dependências entre as camadas:

```powershell
./scripts/verify-architecture.ps1
```

A suíte completa inclui testes transacionais com **MongoDB real**, concorrência, idempotência, rollback, permissões e regras monetárias. O [workflow existente](.github/workflows/fundacao.yml) usa Docker/Testcontainers com replica set, separado do banco da apresentação, e executa:

```powershell
dotnet restore --locked-mode
dotnet build --no-restore --configuration Release -warnaserror
dotnet test --configuration Release --no-build --logger trx --results-directory artifacts/tests
```

Nesse workflow, `TEST_MONGO_CONTAINER=true` é definido antes dos comandos. **Não execute a suíte completa indiscriminadamente contra o Atlas da apresentação:** sem essa opção, as fixtures podem recorrer à URI do `.env` e criar bancos descartáveis no cluster. Para repetir o perfil completo localmente, é necessário Docker e a seleção explícita do perfil técnico; isso não é requisito para usar o sistema ou apresentá-lo.

A homologação registrou 242 testes aprovados no [CI final](https://github.com/argemon/MVPMilton/actions/runs/42429804917), além das jornadas no navegador e da conferência real do Atlas. As evidências, os limites dos ensaios e as correções estão no [relatório](docs/execution/07-RELATORIO-HOMOLOGACAO.md). Nenhuma nova execução de testes é presumida por este README.

## Solução de problemas

| Sintoma | O que conferir |
| --- | --- |
| SDK solicitado não encontrado | Instale a versão exata de `global.json` e confira `dotnet --version` na raiz |
| Aviso de certificado HTTPS | Execute `dotnet dev-certs https --trust`, conclua a confirmação do sistema operacional e reabra o navegador |
| API indisponível na tela inicial | Confira se os dois terminais continuam abertos, se a API iniciou e se o endereço/portas correspondem ao perfil `https` |
| Atlas inacessível | Confira internet, acesso de rede permitido no Atlas e validade da configuração privada existente; não publique a URI para diagnosticar |
| Readiness retorna 503 | Verifique conexão e inicialização explícita da persistência; não gere outro banco nem aplique evoluções automaticamente para contornar |
| Readiness retorna 404 na porta 7143 ou 7243 | Use a porta interna correta: 8081 para API e 8082 para Web |
| Porta já em uso ou build não substitui arquivos | Encerre os hosts anteriores com Ctrl+C antes de reiniciar ou recompilar |
| Login falha ou sessão encerra | Confira a conta, troca obrigatória, estado ativo e eventual bloqueio; se a sessão expirou, entre novamente |
| Página ou operação negada | Verifique o perfil: nem todos os módulos estão disponíveis para Vendedor ou Estoquista |
| Dashboard vazio apesar da carga | Aplique o período fixo de demonstração; o mês atual pode não conter vendas desse cenário |
| Imagem cadastrada não aparece | Confira se a API está usando o mesmo `Images__RootPath` e se o arquivo privado continua disponível |
| Restauração recusada | Há uso/alteração manual ou sessão demonstrativa que impede remover a carga com segurança; preserve os dados e siga a mensagem da tela |

Credenciais, `.env`, imagens privadas e chaves de sessão não são publicados no repositório. Preserve os diretórios locais usados pela apresentação; copiar apenas o código não transporta imagens, contas privadas ou chaves já existentes.

## Limites do projeto

O escopo é acadêmico, voltado à demonstração local. Não inclui:

- Deploy público, hospedagem ou operação de produção.
- Emissão fiscal, pagamentos online, caixa financeiro ou conciliação bancária.
- Compras, fornecedores, comissões ou cálculo de lucro.
- Múltiplas empresas, depósitos, estoque fracionado ou reserva de estoque.
- Descontos, frete, devoluções parciais ou funcionamento offline.
- Catálogo público, pedidos online, aplicativo móvel nativo ou integrações externas de comércio.
- Exportação PDF, recuperação de senha por email ou edição arbitrária dos perfis de permissão.

## Documentação

| Documento | Conteúdo |
| --- | --- |
| [Índice oficial](docs/README.md) | Módulos e referências normativas |
| [Estado do projeto](docs/execution/03-ESTADO-PROJETO.md) | Entrega atual, comandos, resultados e histórico |
| [Homologação e roteiro da banca](docs/execution/07-RELATORIO-HOMOLOGACAO.md) | Jornadas, correções, evidências, limites e apresentação |
| [Requisitos funcionais](docs/requirements/02-REQUISITOS-FUNCIONAIS.md) | Funcionalidades RF-01 a RF-20 |
| [Matriz de permissões](docs/requirements/05-MATRIZ-PERMISSOES.md) | Operações autorizadas por perfil |
| [Arquitetura](docs/architecture/02-ARQUITETURA.md) | Camadas e referências permitidas |
| [Banco de dados](docs/architecture/05-BANCO-DE-DADOS.md) | Documentos, índices, transações e integridade |
| [Segurança](docs/architecture/07-AUTENTICACAO-SEGURANCA.md) | Login, sessões, autorização e proteção de dados |
| [Decisões técnicas](docs/execution/04-DECISOES-TECNICAS.md) | Decisões vigentes e histórico de mudanças |
| [Carga demonstrativa](docs/modules/09-CARGA-DEMONSTRACAO.md) | Cenário fictício, repetição e restauração |

Documentos antigos preservam o histórico e podem citar decisões superadas. Para o escopo atual, prevalecem o índice, o estado vigente e as ADRs mais recentes. Os blocos 00–10 encerram a entrega; não há um bloco posterior de publicação.
