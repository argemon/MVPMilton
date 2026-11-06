# 01 — Escopo

## Entrega desta etapa

Somente documentação Markdown para execução sequencial por agentes. Não implementar código-fonte, projetos .NET, migrations, testes executáveis, Dockerfiles ou Compose agora. A árvore técnica descrita é futura.

## MVP interno

| Bloco | Entrega | Prioridade |
| --- | --- | --- |
| 00–01 | Fundação, login/logout, usuários, perfis e sessão segura | P0 |
| 02–04 | Funcionários, clientes, categorias, marcas e produtos com imagem | P1 |
| 05–06 | Estoque auditável e vendas transacionais com cancelamento | P0 |
| 07–08 | Dashboard e relatórios de vendas/estoque/movimentos com CSV | P1 |
| 09–10 | Homologação, backup/restore e publicação | P0 |

P0 é obrigatório para segurança e integridade; P1 também integra o MVP e não pode ser omitido na entrega final. P2 significa posterior ao MVP.

## Exclusões

Catálogo/pedido público, carrinho e consumidor autenticado pertencem ao bloco 11, P2. Fora do escopo atual: fiscal, gateway, caixa financeiro, contas, comissões, compras/fornecedores, reserva, múltiplos estoques, multiempresa, fracionamento, descontos, trocas parciais, sincronização offline e integrações externas.

## Hipóteses adotadas

- H-01: uma empresa e um estoque por instalação; sem tenantId ou depósito.
- H-02: produtos vendidos em `UN` inteira; nenhuma quantidade fracionada no MVP.
- H-03: venda exige cliente identificado e ativo; sem consumidor genérico implícito.
- H-04: BRL, interface pt-BR e fuso inicial America/Cuiaba configurável por instalação.
- H-05: preço positivo sem desconto/frete/tributo e sem registro de recebimento; confirmar representa operação comercial, não comprovação de pagamento.
- H-06: uma réplica e backup diário; metas de desempenho/recuperação são propostas a medir.
- H-07: perfis fixos e acesso de vendedor às próprias vendas; relatórios comerciais ficam com gerente/admin.

Hipóteses são decisões de projeto propostas, não informações extraídas do TCC. Mudança que afete domínio exige revisar banco, contratos e testes antes de executar o bloco correspondente.

## Questões abertas e impacto

| Questão | Definição necessária | Até quando | Comportamento provisório |
| --- | --- | --- | --- |
| Q-01 | Confirmar UN inteira, cliente obrigatório, dados mínimos e ausência de desconto | Antes de validar 03/04/06 com orientador | Seguir H-02/03/05 e documento opcional |
| Q-02 | Host, domínio, TLS, custo e responsável por operação/backup | Antes de 10 | Desenvolver/homologar localmente |
| Q-03 | Responsável, retenção e tratamento de dados pessoais | Antes de produção real | Somente dados sintéticos |
| Q-04 | Escopo de catálogo: pedido, reserva, entrega e pagamento | Antes de 11 | Nenhum endpoint público |

A revisão detalhou em módulo11/ADR-18 uma proposta P2 de catálogo e solicitação anônima sem reserva/baixa/pagamento/entrega, com RF-21–24 e RN-21–24 propostos. Q-04 continua aberta até aceite/ajuste; a proposta não autoriza implementar11 nem amplia o MVP.

## Limites de aceite

MVP completo exige todos os RF-01 a RF-20, RNF e CA-01 a CA-18. Catálogo não compensa pendências do MVP. Não declarar conformidade legal ou prontidão produtiva apenas pela existência desta documentação.

