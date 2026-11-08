# GestaoComercial

Sistema web de gestão comercial desenvolvido como projeto de TCC de Engenharia de Software. A proposta é centralizar cadastros, estoque e vendas de uma empresa, com controle de acesso, histórico auditável, indicadores e relatórios.

> **Estágio atual:** blocos00–09 aprovados,242 testes no CI integral (126 unitários+116 integrações MongoDB),0 falhas/skips; build Release sem avisos/erros. Carga administrativa gerada, restaurada e repetida no único Atlas existente, sem duplicações ou divergências de saldo. Evidências no [estado](docs/execution/03-ESTADO-PROJETO.md). Bloco10 em homologação; resultados atuais no [relatório](docs/execution/07-RELATORIO-HOMOLOGACAO.md).

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
| Qualidade e entrega | Carga de demonstração no banco existente, homologação funcional, correções visuais e apresentação local |

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
- Catálogo público e pedidos online: excluídos do TCC.

## Tecnologias e arquitetura previstas

| Componente | Escolha |
| --- | --- |
| Linguagem e plataforma | C# e .NET 10 LTS |
| Backend | ASP.NET Core Web API |
| Frontend | Blazor Web App, inicialmente Interactive Server |
| Persistência | MongoDB e MongoDB.Driver oficial; documentos, índices/validadores e transações em replica set |
| Testes | xUnit e Testcontainers com MongoDB real em replica set para integração, integridade e concorrência |
| Execução | Aplicação local conectada ao MongoDB Atlas existente |
| Organização | Monorepo com `frontend/` e `backend/` fisicamente separados |

A API segue Clean Architecture com **Domain, Application, Infrastructure e Api**. O frontend consome contratos HTTP e referencia somente contratos de transporte compartilhados, sem acessar o banco comercial. A autenticação usa o servidor Blazor como intermediário (BFF), com cookie de referência no navegador e credenciais da API protegidas no servidor.

A implementação seguirá SOLID, KISS, DRY e injeção de dependências. Microsserviços, CQRS/MediatR, mensageria, Event Sourcing e Generic Repository não fazem parte da arquitetura definida.

## Planejamento final

Blocos 00–08 implementados conforme [evidências](docs/execution/03-ESTADO-PROJETO.md); **09: carga de dados fictícios no banco MongoDB Atlas existente**; **10: testes completos, correção de bugs e acabamento visual**. Não haverá publicação, outro ambiente ou catálogo online.

- [Plano oficial](docs/execution/00-PLANO-EXECUCAO.md)
- [Carga de demonstração](docs/modules/09-CARGA-DEMONSTRACAO.md)
- [Homologação final](docs/modules/10-HOMOLOGACAO.md)

A demonstração será feita na máquina do apresentador; acesso ao Atlas requer conexão à internet. Não versionar `.env` ou credenciais.

## Demonstração acadêmica — Bloco09

Aplicação e sessões Web usam o único banco `gestao_comercial_dev` no Atlas e a conexão do `.env` existente. Inicializar explicitamente API e Web após build (sem executar migração no startup). O registro de tickets Web é compatibilizado pelo inicializador para catálogo compartilhado (ADR-37); nenhum banco antigo é apagado.

Entre como Administrador e abra **Administração > Dados de Demonstração**. **Gerar carga** cria/retoma o cenário; **Restaurar carga** exige confirmação e recusa alterações/referências manuais antes de excluir. Senhas dos usuários sintéticos são aleatórias e não publicadas; redefina pelo fluxo administrativo existente se precisar apresentar login com outro perfil. Cenário-base fixo07/11/2026: para visualizar todo o histórico, filtre01/08/2026–08/11/2026. Não utilizar dados pessoais reais.

## Executar para a apresentação local

Na raiz do repositório, com o `.env` existente e SDK.NET10 instalado:

```powershell
dotnet restore --locked-mode
dotnet build --configuration Release --no-restore
```

Abra dois terminais na raiz, mantendo ambos em execução:

```powershell
# Terminal1 — API
dotnet run --project backend/GestaoComercial.Api --configuration Release --no-build --launch-profile https
```

```powershell
# Terminal2 — Blazor
dotnet run --project frontend/GestaoComercial.Web --configuration Release --no-build --launch-profile https
```

Abra [Gestão Comercial local](https://localhost:7243). Os hosts usam Development e leem o `.env` existente sem alterá-lo. Se necessário, confie no certificado local com `dotnet dev-certs https --trust`. O banco e o administrador já foram inicializados no Bloco09; não repetir bootstrap/evoluções para uma apresentação normal. Não imprimir credenciais.

Acesso inicial privado permanece em `.local/demo-access.json`, ignorado pelo Git; contas fictícias adicionais da homologação estão em `.local/block10-roles.json`, também privado. O sistema não publica senhas na página de carga. Dashboard/Relatórios:01/08/2026–08/11/2026 (fim exclusivo). Internet é necessária para o Atlas. [Roteiro de12 minutos e checklist](docs/execution/07-RELATORIO-HOMOLOGACAO.md).
