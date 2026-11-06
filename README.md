# GestaoComercial

Sistema web de gestão comercial desenvolvido como projeto de TCC de Engenharia de Software. A proposta é centralizar cadastros, estoque e vendas de uma empresa, com controle de acesso, histórico auditável, indicadores e relatórios.

> **Estágio atual:** documentação de arquitetura, requisitos e módulos concluída para orientar a implementação sequencial. O sistema ainda não foi implementado; não há aplicação executável, migrations ou ambiente publicado neste repositório.

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
| Dashboard | Indicadores comerciais por período, ticket médio, vendas canceladas e produtos com estoque baixo |
| Relatórios | Vendas por período, posição atual do estoque e movimentações, com paginação e exportação CSV |
| Qualidade e entrega | Testes, homologação, publicação, backup e restauração demonstrada |

Os requisitos estão identificados como **RF-01 a RF-20**. As prioridades P0 e P1 fazem parte da entrega completa do MVP.

## Usuários e permissões

| Perfil | Responsabilidade principal |
| --- | --- |
| Administrador | Gerenciar usuários, perfis e todas as operações disponíveis |
| Gerente | Manter cadastros, supervisionar vendas, realizar ajustes e cancelamentos, consultar indicadores e relatórios |
| Vendedor | Manter clientes, consultar produtos e saldos e operar as próprias vendas |
| Estoquista | Manter produtos, registrar entradas e saídas manuais e consultar histórico e relatórios de estoque |

As permissões são verificadas no backend. O acesso completo do administrador não permite ignorar regras de saldo, estados da venda ou preservação de histórico. Consulte a [matriz de permissões](docs/requirements/05-MATRIZ-PERMISSOES.md) para os limites de cada ação.

## Premissas e regras críticas

- Uma empresa por instalação e um saldo de estoque por produto.
- Estoque em unidades inteiras (`UN`), moeda BRL e interface administrativa responsiva em português do Brasil.
- Cliente identificado e ativo para a venda; produto inativo não pode ser adicionado a um novo rascunho nem confirmado.
- Valores monetários em `decimal`, com duas casas. Preços com precisão excedente são rejeitados; médias usam arredondamento final `MidpointRounding.AwayFromZero`.
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
| Persistência | PostgreSQL, Entity Framework Core e migrations |
| Testes | xUnit, com PostgreSQL real nos testes de integração de integridade e concorrência |
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

Ainda não há comandos de instalação ou inicialização de uma aplicação disponível. A estrutura de código, os arquivos de configuração e o Docker Compose serão criados no bloco 00, junto com instruções reproduzíveis de execução local.

Antes de publicar com dados reais, será necessário definir o ambiente de hospedagem, os responsáveis pela operação e o tratamento e a retenção de dados pessoais, além de demonstrar backup e restauração.
