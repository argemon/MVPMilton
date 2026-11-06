# GestaoComercial — índice da documentação

Documentação inicial em português do Brasil, preparada em 06/11/2026. A pasta estava vazia e sem repositório Git ao iniciar. Nenhum sistema, teste ou ambiente foi implementado nesta etapa.

## Como usar

Comece pela visão geral, escopo, decisões técnicas e plano. Cada módulo referencia os contratos e critérios que o executor deve satisfazer. RF identifica requisito funcional; RNF, requisito não funcional; RN, regra de negócio; CA, cenário de aceite; ADR, decisão técnica; H, hipótese; Q, questão aberta.

## Arquitetura

1. [Visão geral](architecture/01-VISAO-GERAL.md)
2. [Arquitetura](architecture/02-ARQUITETURA.md)
3. [Estrutura monorepo](architecture/03-ESTRUTURA-MONOREPO.md)
4. [Modelo de domínio](architecture/04-MODELO-DE-DOMINIO.md)
5. [Banco de dados](architecture/05-BANCO-DE-DADOS.md)
6. [Contratos da API](architecture/06-CONTRATOS-API.md)
7. [Autenticação e segurança](architecture/07-AUTENTICACAO-SEGURANCA.md)
8. [Padrões de código](architecture/08-PADROES-CODIGO.md)
9. [Testes e qualidade](architecture/09-TESTES-QUALIDADE.md)
10. [Deploy e ambientes](architecture/10-DEPLOY-AMBIENTES.md)

## Requisitos

1. [Escopo](requirements/01-ESCOPO.md)
2. [Requisitos funcionais](requirements/02-REQUISITOS-FUNCIONAIS.md)
3. [Requisitos não funcionais](requirements/03-REQUISITOS-NAO-FUNCIONAIS.md)
4. [Regras de negócio](requirements/04-REGRAS-DE-NEGOCIO.md)
5. [Matriz de permissões](requirements/05-MATRIZ-PERMISSOES.md)
6. [Critérios de aceite](requirements/06-CRITERIOS-ACEITE.md)

## Módulos sequenciais

0. [Fundação](modules/00-FUNDACAO.md)
1. [Autenticação](modules/01-AUTENTICACAO.md)
2. [Funcionários](modules/02-FUNCIONARIOS.md)
3. [Clientes](modules/03-CLIENTES.md)
4. [Produtos](modules/04-PRODUTOS.md)
5. [Estoque](modules/05-ESTOQUE.md)
6. [Vendas](modules/06-VENDAS.md)
7. [Dashboard](modules/07-DASHBOARD.md)
8. [Relatórios](modules/08-RELATORIOS.md)
9. [Homologação](modules/09-HOMOLOGACAO.md)
10. [Publicação](modules/10-PUBLICACAO.md)
11. [Catálogo e pedidos — posterior ao MVP](modules/11-CATALOGO-PEDIDOS.md)

## Execução

0. [Plano de execução](execution/00-PLANO-EXECUCAO.md)
1. [Dependências](execution/01-DEPENDENCIAS.md)
2. [Definition of Done](execution/02-DEFINITION-OF-DONE.md)
3. [Estado do projeto](execution/03-ESTADO-PROJETO.md)
4. [Decisões técnicas, hipóteses e questões](execution/04-DECISOES-TECNICAS.md)
5. [Prompt do executor](execution/05-PROMPT-EXECUTOR.md)
6. [Matriz consolidada dos módulos e dependências](execution/06-MATRIZ-MODULOS.md)

Instrução permanente: [AGENTS.md](../AGENTS.md).

Revisão dos módulos em 06/11/2026: todos os blocos00–11 contêm contrato de execução, fluxos/erros/UI/testes/aceites/checklist/DoD. ADR-15–18 estão marcadas como propostas; ADR-01–14 foram preservadas. Requisitos P2 RF-21–24/RN-21–24 detalham alternativa para11 sem liberar implementação. Consulte a matriz para a demonstração de que nenhum gate depende de software posterior.

## Precedência e mudanças

A solicitação do usuário prevalece. Dentro desta especificação, decisões técnicas definem escolhas; requisitos definem comportamento; documentos de banco, API e segurança detalham mecanismos; módulos definem a entrega. Divergência entre documentos bloqueia a implementação daquele ponto: corrigir a especificação e registrar a decisão, sem escolher silenciosamente uma versão.
