# Gestão Comercial

Sistema web de gestão comercial desenvolvido como Trabalho de Conclusão de Curso de Engenharia de Software. Integra cadastros, controle de estoque, frente de caixa, vendas, indicadores e relatórios em uma interface em português do Brasil.

O projeto acadêmico está concluído. A aplicação é executada localmente, com API e interface separadas e conexão ao MongoDB Atlas existente. Os dados de demonstração são fictícios e o acesso ao banco requer internet e configuração privada.

**[Executar localmente](#como-executar-localmente)** · **[Documentação completa](Documentação/README.md)** · **[Manual do usuário](Documentação/entrega/manual-do-usuario.pdf)** · **[Baixar documentação](Documentação/Pacote-documentacao.zip)**

## Funcionalidades

| Área | Recursos entregues |
| --- | --- |
| Acesso e equipe | Login, logout, troca e redefinição de senha, sessões com expiração/revogação, funcionários e contas com três perfis de acesso |
| Clientes | Pessoas físicas e jurídicas, validação de CPF/CNPJ quando informados, contatos, pesquisa, ativação e histórico de vendas |
| Produtos | Categorias, marcas, SKU único, preço, estoque mínimo, imagens privadas, filtros e ativação/desativação |
| Estoque | Saldos, alerta de estoque baixo, entradas, saídas, ajustes justificados e histórico de movimentações |
| Frente de caixa e vendas | Seleção de cliente e produtos, cupom com múltiplos itens, rascunho, revisão de preços, confirmação, consultas, descarte e cancelamento com estorno |
| Dashboard | Receita líquida, ticket médio, vendas, evolução diária, produtos mais vendidos, vendas recentes e estoque baixo |
| Relatórios | Vendas por período e operador, produtos mais vendidos, posição do estoque e movimentos; filtros, paginação, totalizadores e CSV |
| Demonstração | Geração e retomada de cenário fictício, restauração protegida da carga e limpeza explícita com confirmação |

Imagens de produtos aceitam JPEG/PNG de até 2 MiB e 4096×4096 pixels, com validação e conversão para armazenamento privado. A exportação CSV usa UTF-8, separador ponto e vírgula e proteção de texto contra fórmulas; inclui o filtro completo, limitado a 10.000 linhas.

## Perfis de acesso

| Perfil | Responsabilidades |
| --- | --- |
| **Administrador** | Gerenciar equipe e contas, cadastros, estoque, todas as vendas, cancelamentos, dashboard, relatórios e demonstração |
| **Vendedor** | Manter clientes, consultar produtos e saldos e criar, editar, confirmar, descartar e consultar as próprias vendas |
| **Estoquista** | Manter categorias, marcas, produtos e imagens; registrar entradas/saídas e consultar histórico, saldos e relatórios de estoque/movimentos |

A API verifica as permissões em cada operação. Ajustes de estoque e cancelamentos de vendas são exclusivos do Administrador. A consulta do Vendedor permanece limitada às vendas que criou, inclusive no histórico de clientes. Consulte a [matriz de permissões](docs/requirements/05-MATRIZ-PERMISSOES.md).

## Consistência e segurança

- Valores monetários em BRL, calculados em centavos inteiros, com conversão exata e verificação de limites.
- Estoque em unidades inteiras, sem saldo negativo. Produto novo começa com saldo zero; rascunho não reserva mercadoria.
- Confirmação da venda, baixa de estoque, movimentos e registros associados na mesma transação; cancelamento com estorno integral.
- Controle de concorrência e idempotência para impedir efeitos duplicados ao repetir operações.
- Preservação de snapshots da venda e histórico de movimentações. No uso comercial normal, correções são novos registros; a limpeza administrativa da demonstração é uma exceção explícita, descrita abaixo.
- Autorização por perfil e propriedade, sessões revogáveis, tokens mantidos nos servidores e proteção antifalsificação nos formulários de conta.

## Tecnologias e arquitetura

| Componente | Tecnologia |
| --- | --- |
| Plataforma | C# e .NET 10 LTS |
| Backend | ASP.NET Core Web API |
| Interface | Blazor Web App com Interactive Server |
| Persistência | MongoDB Atlas e MongoDB.Driver oficial |
| Autenticação | ASP.NET Core Identity com armazenamento MongoDB, sessões opacas e intermediação pelo servidor Web |
| Testes | xUnit, MongoDB real e Testcontainers no perfil de integração do CI |
| Integração contínua | GitHub Actions: arquitetura, compilação, testes, auditoria de dependências e verificação de segredos |

```text
GestaoComercial.slnx
backend/
  GestaoComercial.Domain/          # Regras e validações do domínio
  GestaoComercial.Application/     # Casos de uso e interfaces
  GestaoComercial.Infrastructure/  # MongoDB, Identity, transações e arquivos
  GestaoComercial.Api/             # Endpoints HTTP e composição
frontend/
  GestaoComercial.Web/             # Interface Blazor e clientes da API
shared/
  GestaoComercial.Contracts/       # DTOs e contratos HTTP
tests/                            # Testes unitários e de integração
build/                            # Configuração compartilhada dos hosts
scripts/                          # Verificação de arquitetura
docs/                             # Especificações e histórico técnico
Documentação/                     # TCC, documentação técnica, manual e diagramas
```

As referências do backend seguem **Domain ← Application ← Infrastructure**, com composição na API. Entre os projetos da solução, o frontend referencia somente os contratos compartilhados. As operações de negócio passam pela API; o armazenamento técnico de sessões Web é separado das responsabilidades de negócio. O [documento de arquitetura](docs/architecture/02-ARQUITETURA.md) e os [diagramas](Documentação/diagramas/) detalham essa organização.

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

Use uma conta fornecida pelo responsável pela instalação. O repositório não inclui credenciais privadas, cadastro público ou senha padrão publicada.

O Administrador cadastra um funcionário ativo e cria sua conta, informando email, perfil e senha temporária. No primeiro acesso, o usuário conclui a troca obrigatória de senha. Funcionário sem conta não acessa o sistema; desativar o funcionário revoga o acesso vinculado. Reativação e manutenção da conta são operações administrativas próprias.

O [manual do usuário](Documentação/entrega/manual-do-usuario.pdf) descreve acesso, navegação e operações de cada perfil.

## Dados de demonstração

A página **Administração → Dados de Demonstração** oferece três ações distintas, disponíveis ao Administrador:

| Ação | Efeito |
| --- | --- |
| **Gerar carga** | Cria ou retoma o cenário fictício, sem duplicar registros já gerados |
| **Restaurar carga** | Remove o conjunto demonstrativo identificado quando as verificações permitirem, protegendo alterações e dependências manuais; exige confirmação |
| **Limpar dados do sistema** | Exclui também dados comerciais manuais, histórico e demais contas, preservando o administrador executor, vínculo, sessão atual e estrutura técnica; exige digitar **APAGAR DADOS** e confirmar |

A carga-base define 12 funcionários com contas, 40 clientes, 100 produtos e 250 vendas, das quais 25 canceladas. Esses números descrevem o cenário gerado, não a contagem atual do banco. As datas do cenário são fixas; para consultar seu histórico completo, utilize **01/08/2026 a 08/11/2026**, com o fim exclusivo.

A limpeza não gera uma nova carga automaticamente nem remove arquivos de imagens externos ao banco. Gerar, restaurar e limpar não são rotinas necessárias para iniciar a aplicação. Consulte o [manual](Documentação/entrega/manual-do-usuario.pdf) e a [especificação da demonstração](docs/modules/09-CARGA-DEMONSTRACAO.md) antes de operar esses recursos.

## Testes e validação

O [workflow de integração contínua](.github/workflows/fundacao.yml) verifica as dependências entre projetos, restaura e compila a solução, executa testes unitários e de integração, audita dependências e procura segredos. No CI, os testes transacionais usam MongoDB real em replica set com Docker/Testcontainers.

Para executar os testes unitários após compilar em Release:

```powershell
dotnet test tests/GestaoComercial.UnitTests --configuration Release --no-build
```

A suíte de integração depende de seleção explícita do ambiente técnico e isolamento de bancos; seu procedimento está na [documentação técnica](Documentação/entrega/documentacao-tecnica.pdf). Executar a aplicação para uso local não exige Docker nem execução dos testes.

Os resultados registrados de homologação conservam a data e a versão em que foram obtidos. O [estado do projeto](docs/execution/03-ESTADO-PROJETO.md) e o [relatório de homologação](docs/execution/07-RELATORIO-HOMOLOGACAO.md) apresentam evidências e limites; os resultados de cada commit estão no [GitHub Actions](https://github.com/argemon/MVPMilton/actions).

## Solução de problemas

| Situação | Orientação |
| --- | --- |
| SDK ou projeto não encontrado | Confira a versão exigida em `global.json` e execute os comandos na raiz que contém `GestaoComercial.slnx` |
| Falha ao baixar dependências | Confira internet e acesso ao NuGet; conclua a restauração e compilação antes de usar `--no-build` |
| Configuração ou conexão MongoDB inválida | Confira `.env` privado, URI, nome do banco, internet e liberação de IP no Atlas com o responsável |
| Aviso HTTPS | Confie no certificado de desenvolvimento conforme o guia de execução e reabra o navegador |
| API indisponível ou porta ocupada | Confira os dois terminais e os perfis HTTPS; encerre processos anteriores com Ctrl+C antes de reiniciar |
| Login recusado ou operação negada | Confira conta ativa, senha, troca obrigatória, bloqueio temporário e permissões do perfil |
| Imagem não aparece | Preserve `Images__RootPath` e os arquivos privados usados pela instalação |
| Dashboard sem dados da demonstração | Selecione o período fixo do cenário; relatórios usam data final exclusiva |
| Restauração recusada | Leia o impedimento informado e preserve os dados; a limpeza explícita possui alcance diferente |

Outras mensagens e procedimentos estão no [manual do usuário](Documentação/entrega/manual-do-usuario.pdf). Código e documentação não transportam contas privadas, imagens existentes ou chaves de sessão da instalação.

## Documentação

O pacote acadêmico contém monografia, documentação técnica e manual em Word/PDF, textos Markdown, 22 diagramas em PlantUML/SVG/PNG, referências consultadas e registros de verificação.

| Material | PDF | Word |
| --- | --- | --- |
| Monografia | [Ler](Documentação/entrega/monografia.pdf) | [Editar](Documentação/entrega/monografia.docx) |
| Documentação técnica | [Ler](Documentação/entrega/documentacao-tecnica.pdf) | [Editar](Documentação/entrega/documentacao-tecnica.docx) |
| Manual do usuário | [Ler](Documentação/entrega/manual-do-usuario.pdf) | [Editar](Documentação/entrega/manual-do-usuario.docx) |

- [Índice da entrega acadêmica](Documentação/README.md) e [pacote completo](Documentação/Pacote-documentacao.zip).
- [Diagramas editáveis e exportados](Documentação/diagramas/), [fontes dos textos](Documentação/fontes/) e [referências verificadas](Documentação/fontes/referencias.json).
- [Índice técnico do projeto](docs/README.md): requisitos, regras, permissões, arquitetura, persistência, segurança e decisões.
- [Estado e evidências](docs/execution/03-ESTADO-PROJETO.md): registros de implementação, validação e publicação.

A documentação acadêmica está identificada como versão para revisão: os dados pessoais/institucionais e a adequação às exigências do curso permanecem a preencher e conferir com o orientador. Isso não representa uma etapa de implementação pendente do sistema.

## Escopo acadêmico

O sistema atende uma empresa e um estoque, com quantidades inteiras e apresentação local conectada ao Atlas. Não inclui emissão fiscal, processamento de pagamentos, caixa financeiro, compras, fornecedores, múltiplos depósitos, reserva de estoque, devoluções parciais ou funcionamento offline. Catálogo público, pedidos online, aplicativo móvel e deploy de produção não integram a entrega.

Confirmar uma venda registra a operação comercial e sua movimentação de estoque; não comprova recebimento de pagamento ou emissão de documento fiscal. A exportação de relatórios da aplicação é CSV; os PDFs deste repositório são os documentos acadêmicos.
