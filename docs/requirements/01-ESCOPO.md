# 01 — Escopo oficial do TCC

## Objetivo

Aplicação web de gestão comercial apresentada localmente à banca de Engenharia de Software. Executa na máquina do apresentador, consumindo **um único MongoDB Atlas já configurado**. O objetivo é comprovar o funcionamento das funcionalidades e apresentar uma interface bonita e organizada, não operar um produto de produção.

## Escopo funcional

| Blocos | Entrega |
| --- | --- |
| 00–01 | Fundação, autenticação, perfis e permissões |
| 02–04 | Funcionários, clientes, categorias, marcas e produtos |
| 05–06 | Estoque e vendas com histórico e cancelamento |
| 07–08 | Dashboard, relatórios e exportações CSV |
| 09 | Carga de dados fictícios e restauração controlada **no mesmo banco** |
| 10 | Vistoria funcional, testes, correção de bugs, layout responsivo e preparação da apresentação |

RF-01 a RF-20 continuam correspondendo às funcionalidades implementadas. 09 e 10 são etapas de demonstração/qualidade, sem novos recursos comerciais.

## Exclusões definitivas

**Não haverá** publicação/deploy, servidor público, catálogo público, pedidos online, segundo ambiente ou segundo banco, alta disponibilidade, observabilidade avançada, testes massivos de carga, pagamento online ou integração fiscal. O repositório conserva histórico técnico, mas esses itens não fazem parte dos critérios de conclusão do TCC.

## Regras preservadas

- Uma empresa, um estoque por produto, BRL e interface pt-BR.
- Saldo não negativo e movimentações auditáveis.
- Vendas confirmadas baixam saldo atomicamente; cancelamentos estornam uma única vez.
- Cadastro lógico inativo preserva históricos; privilégios são validados na API.
- Na carga, usar apenas dados fictícios e preservar registros manuais.

## Aceite

A banca deve conseguir ver os principais módulos funcionando na máquina do apresentador: login, cadastros, estoque, venda/cancelamento, Dashboard e Relatórios. O Bloco 10 também verifica carga, restauração e aparência do Web App.
