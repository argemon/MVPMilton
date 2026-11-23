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

Com o **SDK .NET 10.0.401**, o **`.env` privado configurado**, acesso de rede ao Atlas existente e o **certificado HTTPS confiável**, abra **dois terminais na raiz do projeto** (a pasta que contém `GestaoComercial.slnx`). Na instalação da apresentação, o banco e a conta de acesso já estão preparados.

**Backend — terminal 1:**

```powershell
dotnet run --project backend/GestaoComercial.Api --launch-profile https
```

**Frontend — terminal 2:**

```powershell
dotnet run --project frontend/GestaoComercial.Web --launch-profile https
```

Espere o backend iniciar antes de executar o frontend. Os comandos restauram as dependências e compilam automaticamente; não é necessário rodar `restore` e `build` separadamente para iniciar. Mantenha os dois terminais abertos e acesse **[https://localhost:7243](https://localhost:7243)**. Entre com a conta fornecida pelo responsável. Para encerrar, pressione **Ctrl+C** em cada terminal.

**Imagens já cadastradas:** preserve a configuração `Images__RootPath` da instalação. Sem essa configuração, a API usa `backend/GestaoComercial.Api/.local/images`. Se as imagens existentes estiverem em outra pasta, configure seu caminho absoluto uma vez no `.env` privado; por exemplo, `Images__RootPath="D:/Projetos/MVPMilton/.local/images"` para a pasta utilizada no roteiro anterior. Ajuste o caminho ao seu computador.

<details>
<summary>Primeira instalação e configuração detalhada</summary>

Este guia acompanha uma **primeira execução no Windows, usando PowerShell**, desde o download até o login. Não é necessário saber programar nem instalar Visual Studio. Você vai iniciar dois programas no terminal: a **API**, que executa as regras e acessa o banco, e o **Web**, que apresenta as telas no navegador.

**Antes de começar:** o código é público, mas o banco Atlas e as contas de acesso são privados. Baixar o projeto não fornece a conexão nem uma senha de administrador. Solicite ao responsável a configuração do banco existente, a liberação do seu acesso de rede e uma conta do sistema. Sem isso, será possível baixar e compilar, mas não utilizar as funcionalidades que dependem do banco. Esta instalação usa internet e compartilha os dados do Atlas; alterações feitas por outro computador aparecem no mesmo banco.

### 1. Instalar o que será necessário

| Item | Onde obter / o que fazer |
| --- | --- |
| SDK .NET **10.0.401** | Abra a [página oficial do .NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0), localize **SDK 10.0.401** e escolha o instalador Windows compatível com o seu computador (normalmente x64; Arm64 para equipamentos ARM). Instale o **SDK**, que inclui as ferramentas para compilar e executar. |
| Git, se escolher baixar pelo terminal | Instale pelo [site oficial do Git para Windows](https://git-scm.com/downloads/win). Quem baixar por ZIP pode pular essa instalação. |
| Navegador | Use Edge, Chrome ou Firefox atualizado. |
| Conexão e acesso ao banco | Obtenha com o responsável a URI privada do MongoDB Atlas e confirme a autorização do seu IP. |

A versão exata do SDK é exigida pelo [global.json](global.json): `rollForward` está desabilitado. Ter somente outra versão, como 10.0.402, não atende essa configuração. Várias versões do SDK podem coexistir no computador.

**Docker, Node.js, MongoDB instalado no PC e uma IDE não são necessários neste roteiro com Atlas.** Docker é usado no perfil de testes de integração, explicado mais adiante.

Depois das instalações, feche e reabra o PowerShell para que ele reconheça os programas. Abra o menu Iniciar, procure **PowerShell** e execute:

```powershell
dotnet --list-sdks
git --version
```

O primeiro comando deve listar `10.0.401`. Execute o segundo apenas se instalou o Git. Se aparecer “não é reconhecido”, reabra o terminal e confira a instalação antes de continuar.

### 2. Baixar o projeto

Escolha **uma** das opções abaixo.

#### Opção A — pelo Git

No PowerShell, execute uma linha por vez:

```powershell
New-Item -ItemType Directory -Force "$HOME\Projetos" | Out-Null
Set-Location "$HOME\Projetos"
git clone https://github.com/argemon/MVPMilton.git
Set-Location MVPMilton
```

O projeto ficará em uma pasta como `C:\Users\SeuUsuario\Projetos\MVPMilton`. Se essa pasta já existir, utilize a cópia existente ou escolha outra pasta vazia para o clone.

#### Opção B — pelo navegador, sem Git

1. Abra [argemon/MVPMilton no GitHub](https://github.com/argemon/MVPMilton).
2. Selecione a branch **main**, clique em **Code** e depois em **Download ZIP**.
3. No arquivo baixado, clique com o botão direito e escolha **Extrair Tudo**.
4. Abra a pasta extraída, normalmente chamada `MVPMilton-main`. Não execute o projeto dentro do ZIP.
5. Clique na barra de endereço do Explorador de Arquivos, digite `powershell` e pressione Enter para abrir um terminal nessa pasta.

**Confira a pasta antes de continuar:** ela deve conter `README.md`, `GestaoComercial.slnx`, `global.json`, `.env.example`, `backend` e `frontend`. É essa a **raiz do projeto**, mencionada no restante do guia.

```powershell
Get-Location
Get-ChildItem
dotnet --version
```

Na raiz, o último comando deve retornar `10.0.401`. Todos os comandos seguintes partem dessa pasta. Se você fechar o terminal, abra outro e volte a ela com `Set-Location "CAMINHO_COMPLETO_DA_PASTA"`, substituindo o texto pelo caminho real, entre aspas.

### 3. Configurar o acesso ao MongoDB Atlas

O arquivo privado **`.env` não acompanha o Git nem o ZIP**. Ele deve ficar na raiz, ao lado de `README.md`, e é lido automaticamente pelos dois programas no ambiente `Development`, definido pelo perfil `https`.

1. Solicite ao responsável a URI de conexão do Atlas existente, o nome do banco e uma conta de login do sistema. A conta do banco e a conta de login da aplicação são diferentes.
2. Peça a liberação do seu IP público na lista de acesso de rede do Atlas. O responsável deve seguir a [documentação oficial de acesso por IP](https://www.mongodb.com/docs/atlas/security/add-ip-address-to-list/). Ao trocar de rede, talvez seja necessário atualizar essa liberação.
3. Se ainda não existir `.env`, crie uma cópia do exemplo com o comando abaixo. Ele preserva um arquivo que já exista:

```powershell
if (-not (Test-Path -LiteralPath '.env')) {
    Copy-Item -LiteralPath '.env.example' -Destination '.env'
}
notepad .env
```

4. No Bloco de Notas, preencha a configuração com os valores fornecidos pelo responsável. O trecho abaixo mostra **somente o formato**; os textos em maiúsculas são marcadores e precisam ser substituídos:

```dotenv
MONGODB_URI="COLE_AQUI_A_URI_PRIVADA_COMPLETA_DO_ATLAS"
Mongo__Database="gestao_comercial_dev"
```

Use a URI completa fornecida, com as credenciais e opções corretas; não monte uma conexão a partir de senhas copiadas sem o tratamento necessário para caracteres especiais. `MONGODB_USERNAME` e `MONGODB_PASSWORD` isolados **não** preenchem automaticamente a URI. Salve como `.env`, sem acrescentar `.txt`; no Explorador, habilite a exibição de extensões para conferir o nome.

**API e Web devem usar o mesmo banco `gestao_comercial_dev`**, conforme a configuração vigente. O comentário antigo de `.env.example` sobre um banco Web separado é histórico. Não configure `gestao_web_dev` para este roteiro. Se o responsável indicar outra configuração autorizada, ela deve ser aplicada de maneira consistente aos dois hosts.

Variáveis já definidas no processo têm prioridade sobre `.env`. Se você configurou `Mongo__ConnectionString` ou `Mongo__Database` anteriormente no terminal/sistema, confira se não estão apontando para outro destino. Preserve um `.env` já preparado e não envie seu conteúdo ao GitHub, em prints ou mensagens públicas.

### 4. Preparar o HTTPS e compilar

Na raiz do projeto, execute os comandos abaixo **na ordem**, aguardando cada um terminar:

```powershell
dotnet dev-certs https --trust
dotnet restore --locked-mode
dotnet build --configuration Release --no-restore
```

Aceite a confirmação do Windows para confiar no certificado local quando ela aparecer. O certificado permite abrir o site e a comunicação entre Web e API por HTTPS. O `restore` baixa as dependências e pode demorar na primeira vez; o `build` compila o projeto. Ambos precisam terminar com sucesso antes de continuar. Se houver erro, veja [Solução de problemas](#solução-de-problemas).

### 5. Conferir a conexão com o banco

Este comando verifica conectividade e suporte a transações; não gera carga de demonstração nem inicializa a estrutura:

```powershell
dotnet run --project backend/GestaoComercial.Api --configuration Release --no-build --launch-profile https -- --probe-mongo
```

O resultado esperado é **“MongoDB: conexão e topologia transacional disponíveis”**. O comando termina e devolve o controle ao terminal. Em caso de falha, confira URI, internet e liberação de IP com o responsável antes de avançar.

### 6. Verificar se o banco já está preparado

**Para utilizar a instalação existente da apresentação, pule para o passo 7.** O banco e o administrador dessa instalação já foram inicializados; baixar o código em outro computador não exige repetir essa preparação. Obtenha uma conta com o responsável.

Os comandos a seguir são destinados **somente ao responsável pela preparação de uma instalação ainda não inicializada**, com o destino confirmado. Eles escrevem no banco indicado pelo `.env`. Não crie outro cluster ou banco para contornar erro de conexão ou de configuração.

#### Preparar as coleções, índices e validadores

Com os dois programas parados, na raiz, execute um comando por vez:

```powershell
dotnet run --project backend/GestaoComercial.Api --configuration Release --no-build --launch-profile https -- --initialize-persistence
dotnet run --project frontend/GestaoComercial.Web --configuration Release --no-build --launch-profile https -- --initialize-persistence
```

Cada comando termina após a preparação e deve informar conexão/topologia disponíveis. A inicialização é explícita; iniciar o site normalmente não a executa. Se houver divergência de estrutura, solicite revisão ao responsável em vez de apagar coleções.

#### Criar o primeiro administrador, somente se ainda não existir

No `.env` privado, acrescente as linhas abaixo, substituindo os marcadores por nome, email e senha escolhidos para essa instalação:

```dotenv
Bootstrap__Nome="NOME_DO_ADMINISTRADOR"
Bootstrap__Email="EMAIL_DO_ADMINISTRADOR"
Bootstrap__Senha="SENHA_TEMPORARIA_ESCOLHIDA_POR_VOCE"
```

Escolha uma senha de **12 a 128 caracteres**, com maiúscula, minúscula, número e símbolo, diferente dos marcadores acima. Execute:

```powershell
dotnet run --project backend/GestaoComercial.Api --configuration Release --no-build --launch-profile https -- --bootstrap-admin
```

O resultado esperado é **“Administrador provisionado; troca de senha obrigatória.”** Se já existir administrador, o comando informa isso e não altera suas credenciais; ele não é um mecanismo de recuperação de senha. Após o provisionamento, remova as três linhas `Bootstrap__...` do `.env`, preservando as configurações do banco. A conta criada permanece salva no Atlas.

### 7. Iniciar a API — primeiro terminal

Abra um PowerShell na raiz do projeto e execute:

```powershell
dotnet run --project backend/GestaoComercial.Api --launch-profile https
```

Espere aparecer **`Now listening on`**, com as portas `7143` e `8081`. **Deixe esse terminal aberto.** Ele estará ocupado executando a API; isso é esperado. Preserve o caminho de imagens configurado conforme a orientação acima.

### 8. Iniciar o site — segundo terminal

Abra **outro** PowerShell na mesma raiz do projeto. No Windows Terminal, também pode abrir uma nova aba. Confira a pasta com `Get-Location` e execute:

```powershell
dotnet run --project frontend/GestaoComercial.Web --launch-profile https
```

Espere aparecer **`Now listening on`**, com as portas `7243` e `8082`. **Deixe esse segundo terminal aberto também.** Não é preciso digitar outros comandos nesses terminais enquanto os programas estiverem rodando.

### 9. Abrir o navegador e fazer login

1. Abra **[https://localhost:7243](https://localhost:7243)** no mesmo computador em que iniciou os dois programas. O navegador não abre automaticamente.
2. Use o email e a senha da conta fornecida pelo responsável, ou do administrador provisionado no passo 6.
3. Se for solicitado, troque a senha temporária e entre novamente.
4. Ao visualizar a área de trabalho e os atalhos permitidos para seu perfil, a execução está pronta para uso.

Não existe senha padrão ou cadastro público. Os arquivos privados `.local/demo-access.json` e `.local/block10-roles.json` da máquina de apresentação não acompanham o download. Se você não recebeu uma conta e o banco já tem administrador, solicite acesso a ele.

Se o banco autorizado acabou de ser preparado, os cadastros estarão vazios. É possível cadastrá-los manualmente ou entrar como Administrador em **Administração → Dados de Demonstração** e selecionar **Gerar carga**, conforme a [explicação da carga](#dados-de-demonstração). Na instalação compartilhada, confira com o responsável se a carga já existe antes de gerar ou restaurar dados.

| Serviço | Endereço local / finalidade |
| --- | --- |
| Site que você deve abrir | [https://localhost:7243](https://localhost:7243) |
| API | [https://localhost:7143](https://localhost:7143) — endpoints em `/api/v1`; não é a interface de uso |
| Processo da API ativo | [https://localhost:7143/health/live](https://localhost:7143/health/live) |
| API pronta, com banco inicializado | [https://localhost:8081/health/ready](https://localhost:8081/health/ready) |
| Web pronta, com persistência técnica inicializada | [https://localhost:8082/health/ready](https://localhost:8082/health/ready) |

As verificações de prontidão devem retornar HTTP **200**. As portas `8081` e `8082` são internas; consultar `/health/ready` nas portas públicas `7143` ou `7243` retorna **404** por projeto. `/health/live` confirma somente o processo ativo e não substitui a verificação do banco.

### 10. Parar e abrir novamente em outro dia

Para encerrar, pressione **Ctrl+C em cada um dos dois terminais** e espere os programas terminarem. Fechar o navegador não encerra API e Web.

Para usar novamente, mantenha internet e acesso ao Atlas e repita somente os **passos 7, 8 e 9**. Não é necessário clonar, configurar `.env`, inicializar o banco ou gerar carga a cada execução. Os dados do Atlas permanecem salvos. Preserve também `.local/images` e as chaves Web: copiar somente o código para outro computador não transporta as imagens locais.

### 11. Baixar futuras atualizações

Se baixou pelo Git, pare os dois programas, abra um terminal na raiz e execute:

```powershell
git pull --ff-only
dotnet restore --locked-mode
dotnet build --configuration Release --no-restore
```

Depois repita os passos 7 a 9. Se o Git informar alterações locais ou conflito, preserve seus arquivos e resolva a situação antes de continuar; não apague configurações privadas para forçar a atualização.

Se baixou por ZIP, extraia a nova versão em outra pasta, transfira com cuidado sua configuração privada e as imagens/chaves locais necessárias, e repita a preparação e a compilação. Não sobrescreva a instalação anterior sem guardar seus arquivos privados. Mudanças de estrutura do banco devem seguir as instruções da versão e a orientação do responsável; o `git pull` não aplica evoluções automaticamente.

### Referência das configurações locais

| Configuração | Uso |
| --- | --- |
| `MONGODB_URI` | URI privada completa do Atlas, lida do `.env` em `Development` |
| `Mongo__ConnectionString` | Sobrescrita opcional da conexão; dispensa o alias `MONGODB_URI` quando definida |
| `Mongo__Database` | Mesmo banco nos dois hosts; padrão `gestao_comercial_dev` |
| `Images__RootPath` | Diretório persistente das imagens da API; pode ser definido uma vez no `.env` privado. Padrão: `backend/GestaoComercial.Api/.local/images` |
| `Api__BaseUrl` | O perfil `https` do Web define `https://localhost:7143/` |
| `Api__HealthBaseUrl` | O perfil `https` do Web define `https://localhost:8081/` |
| `DataProtection__KeysPath` | Chaves das sessões Web; padrão `.local/keys` dentro de `frontend/GestaoComercial.Web` |

Este roteiro usa os perfis `https` versionados e o ambiente `Development`. Referências históricas do [guia da fundação](docs/execution/07-EXECUCAO-LOCAL-FUNDACAO.md) descrevem etapas anteriores; portas, funcionalidades e banco compartilhado atuais são os apresentados aqui e nos perfis do código.

</details>

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
| `git` não é reconhecido | Instale o Git e reabra o PowerShell, ou escolha o download por ZIP |
| `dotnet` não é reconhecido | Instale o SDK .NET e reabra o PowerShell; instalar somente o Runtime não fornece as ferramentas de compilação |
| SDK solicitado não encontrado | Instale a versão exata de `global.json` e confira `dotnet --version` na raiz |
| Projeto ou solução não encontrado | Volte à raiz que contém `GestaoComercial.slnx`, `backend` e `frontend`; confira se o ZIP foi extraído |
| Configuração `Mongo:ConnectionString` ausente ou inválida | Confira se `.env` está na raiz, se não foi salvo como `.env.txt`, se a URI foi preenchida e se usou `--launch-profile https` |
| Erro ao baixar pacotes no `restore` | Confira internet, proxy/firewall e acesso ao NuGet; repita o restore antes do build. Não prossiga com `--no-build` se a compilação ainda não terminou com sucesso |
| Executável não encontrado ao iniciar com `--no-build` | Execute primeiro `dotnet restore --locked-mode` e `dotnet build --configuration Release --no-restore` com sucesso |
| Aviso de certificado HTTPS | Execute `dotnet dev-certs https --trust`, conclua a confirmação do sistema operacional e reabra o navegador |
| API indisponível na tela inicial | Confira se os dois terminais continuam abertos, se a API iniciou e se o endereço/portas correspondem ao perfil `https` |
| Atlas inacessível | Confira internet, acesso de rede permitido no Atlas e validade da configuração privada existente; não publique a URI para diagnosticar |
| Readiness retorna 503 | Verifique conexão e inicialização explícita da persistência; não gere outro banco nem aplique evoluções automaticamente para contornar |
| Readiness retorna 404 na porta 7143 ou 7243 | Use a porta interna correta: 8081 para API e 8082 para Web |
| Porta já em uso ou build não substitui arquivos | Encerre os hosts anteriores com Ctrl+C antes de reiniciar ou recompilar |
| Login falha ou sessão encerra | Confira a conta, troca obrigatória, estado ativo e eventual bloqueio; se a sessão expirou, entre novamente |
| Baixei o projeto, mas não tenho email/senha para entrar | Solicite uma conta ao administrador da instalação; o clone/ZIP não inclui as contas privadas e o bootstrap não redefine senhas de administradores existentes |
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

Exceção autorizada ADR-39: Limpar dados do sistema remove também registros manuais e histórico, preservando administrador executor/vínculo/sessão, estrutura técnica e recibos de limpeza. Exige APAGAR DADOS e é independente de Restaurar carga. Após sucesso, gerar uma nova carga pelo botão existente. Sem exclusão automática de arquivos de imagens.
