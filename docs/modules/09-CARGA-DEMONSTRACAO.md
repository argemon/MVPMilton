# Bloco 09 — Carga de dados de demonstração

## Objetivo

Disponibilizar uma página **Administração > Dados de Demonstração** para preencher **o banco MongoDB Atlas já utilizado pelo sistema**, com dados totalmente fictícios, variados e coerentes. A carga existe exclusivamente para facilitar testes e a apresentação do TCC.

## Premissas

- **Um único banco e um único ambiente**, reutilizando a conexão atual do `.env`. Não criar outro banco, instância, réplica ou ambiente Demo.
- Ferramenta disponível apenas para administrador autenticado; confirmar antes de restaurar.
- KISS: reutilizar serviços existentes de cadastros, estoque e vendas; não criar uma arquitetura paralela de importação.
- Registros gerados identificáveis (ex.: `SeedBatchId` ou mecanismo equivalente), permitindo repetir e restaurar sem duplicar indevidamente.
- Preservar dados manuais: **não utilizar `DropDatabase`, `DropCollection` nem limpeza indiscriminada**. Se houver referências manuais a dados da carga, impedir restauração destrutiva e explicar o motivo.

## Conteúdo esperado (quantidades aproximadas)

| Tipo | Conjunto representativo |
| --- | --- |
| Usuários e funcionários | 12 contas/perfis Administrador, Vendedor e Estoquista; ativos e inativos |
| Clientes | 40 clientes fictícios, com/sem compras |
| Produtos | 100 produtos, categorias e marcas diversas, ativos/inativos, preços, estoque alto/baixo/zero |
| Estoque | Entradas, saídas, ajustes, responsáveis, motivos e movimentações históricas coerentes |
| Vendas | Cerca de 250 vendas em meses diferentes, múltiplos itens, confirmadas e canceladas com estorno |
| Dashboard e relatórios | Histórico suficiente para evolução de faturamento, ticket médio, ranking, estoque baixo e exportação CSV |

Usar dados evidentemente fictícios, e-mails de exemplo e imagens ilustrativas permitidas. Senhas de usuários demonstrativos devem ser tratadas pelo mecanismo de hash atual; não versionar credenciais.

## Fluxos da interface

1. Mostrar resumo da carga, banco de destino sem segredos e última execução.
2. **Gerar carga**: não duplicar se já executada; informar contagens e resultado.
3. **Restaurar carga**: confirmar, retirar somente registros identificados da carga que possam ser removidos com segurança, recriar o cenário determinístico e conferir contagens.
4. Informar erros e permitir nova tentativa segura quando possível.

## Regras de consistência

- Vendas confirmadas baixam estoque e geram movimentos; cancelamentos estornam uma vez.
- Saldos nunca negativos; total de venda calculado de itens e preços históricos.
- Identificadores de login/SKU únicos; vínculos entre entidades válidos.
- Utilizar seed fixa e data-base documentada para reprodução; respeitar permissões de usuário.

## Testes e aceite

- [x] Gerar carga no banco existente pela interface administrativa.
- [x] Verificar contagens, dados e coerência das referências.
- [x] Repetir sem criar duplicatas.
- [x] Restaurar com confirmação sem remover dados manuais.
- [x] Verificar saldo, vendas, estorno, Dashboard e Relatórios antes e depois.
- [x] Negar execução por usuário sem permissão.
- [x] Executar build/testes aplicáveis e registrar resultados reais.

**Entrega:** carga e restauração funcionais e documentadas; não antecipar homologação do Bloco 10.

## Contrato da implementação (ADR-37)

Página /administracao/dados-demonstracao. POST /api/v1/administracao/dados-demonstracao/gerar e /restaurar (confirmar=true). Policy administrativa também na API;409 para dependência/alteração manual e400 sem confirmação. Identificação pela auditoria transacional existente, sem coleção de controle adicional. Conciliação, reexecução, recuperação e rollback obrigatórios. Não iniciar10 nesta solicitação. Evidências no relatório12.
