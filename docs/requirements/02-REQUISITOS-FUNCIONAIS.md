# 02 — Requisitos funcionais

## Identificação e rastreabilidade

Prioridade e atores devem ser lidos com a [matriz](05-MATRIZ-PERMISSOES.md). A tabela define o comportamento mínimo; regras RN e contratos completam os detalhes.

| ID | Prioridade / ator | Fluxo e resultado observável | Regras | Aceite / bloco |
| --- | --- | --- | --- | --- |
| RF-01 | P0 / todos | Entrar por email/senha, abrir sessão e mostrar nome/perfil; falha genérica | RN-15/16 | CA-01 / 01 |
| RF-02 | P0 / todos | Sair revoga sessão; expiração e alteração de acesso impedem novas operações | RN-15/16 | CA-01/02 / 01 |
| RF-03 | P0 / Admin | Criar conta vinculada, definir perfil, ativar/desativar e redefinir senha sem revelar hashes | RN-15/16/18 | CA-02/17 / 01–02 |
| RF-04 | P1 / Admin, Gerente | Cadastrar/editar/listar funcionário e desativar com revogação de acesso vinculado | RN-08/16/18 | CA-03 / 02 |
| RF-05 | P1 / Admin, Gerente, Vendedor | Cadastrar/editar/pesquisar cliente por nome/documento e desativar | RN-08/12 | CA-04 / 03 |
| RF-06 | P1 / Admin, Gerente, Vendedor | Consultar histórico paginado do cliente respeitando escopo de vendas | RN-07/17 | CA-04/13 / 03 e 06 |
| RF-07 | P1 / Admin, Gerente, Estoquista | Manter categorias/marcas, sem nome normalizado duplicado | RN-08/12 | CA-05 / 04 |
| RF-08 | P1 / Admin, Gerente, Estoquista | Manter produto com SKU, preço, imagem, categoria, marca opcional e status | RN-01/08/12/19 | CA-05/17 / 04 |
| RF-09 | P0 / Admin, Gerente, Estoquista | Registrar entrada manual com quantidade e motivo; saldo e histórico juntos | RN-02/06/09/14 | CA-06 / 05 |
| RF-10 | P0 / Admin, Gerente, Estoquista | Registrar saída manual sem gerar saldo negativo | RN-02/06/09/14 | CA-06/07 / 05 |
| RF-11 | P0 / Admin, Gerente | Ajustar saldo por delta justificado, preservando movimento anterior | RN-02/06/09/14 | CA-06/07/13 / 05 |
| RF-12 | P1 / leitores de estoque | Listar saldo e histórico por produto/período/tipo | RN-07/09/20 | CA-06/15 / 05 |
| RF-13 | P0 / Admin, Gerente, Vendedor | Criar/editar/descartar rascunho com cliente e itens; total calculado no servidor | RN-01/10/11/17 | CA-08 / 06 |
| RF-14 | P0 / Admin, Gerente, Vendedor | Confirmar rascunho, congelar snapshots e baixar estoque atomicamente | RN-02/03/04/05/10/11 | CA-08/09/10/11 / 06 |
| RF-15 | P1 / Admin, Gerente, Vendedor | Consultar venda, itens, estado e histórico do ciclo com paginação | RN-07/17 | CA-08/13/14 / 06 |
| RF-16 | P0 / Admin, Gerente | Cancelar confirmada com motivo e estorno exato uma única vez | RN-05/06/07/10 | CA-12/13 / 06 |
| RF-17 | P1 / Admin, Gerente | Ver indicadores por período conforme fórmula abaixo | RN-13/20 | CA-14/15 / 07 |
| RF-18 | P1 / Admin, Gerente | Ver produtos ativos com saldo <= mínimo e acesso à lista completa | RN-02/13 | CA-14 / 07 |
| RF-19 | P1 / Admin, Gerente | Relatório paginado/CSV de vendas com totais do filtro completo | RN-07/13/19/20 | CA-14/15/17 / 08 |
| RF-20 | P1 / Admin, Gerente, Estoquista nos relatórios de estoque | Relatórios/CSV de saldo atual e movimentações filtradas com totais | RN-09/19/20 | CA-15/17 / 08 |

## Requisitos P2 propostos — fora do MVP

ADR-18 detalha a alternativa de menor complexidade. Estes requisitos só se tornam escopo de implementação do 11 após aceite/ajuste de Q-04; não alteram RF-01–20 nem sua definição de pronto.

| ID | Prioridade / ator | Comportamento verificável | Regras / aceite local |
| --- | --- | --- | --- |
| RF-21 | P2 / Admin, Gerente | Publicar/despublicar produto ativo com versão, sem alterar cadastro/saldo | RN-21/24; M11-CA01/04 |
| RF-22 | P2 / visitante | Consultar página/detalhe/imagem de produto ativo e publicado sem dados internos | RN-21; M11-CA01/06 |
| RF-23 | P2 / visitante | Enviar solicitação com nome/telefone/itens e receber protocolo idempotente sem consumir saldo | RN-22/23/24; M11-CA02/03/04 |
| RF-24 | P2 / Admin, Gerente | Consultar/encerrar solicitação com versão/motivo e preservar contato/snapshots sob autorização | RN-22/24; M11-CA05/06 |

## Indicadores normativos RF-17 / RF-19 (MVP)

Intervalo civil `[inicio,fim)` convertido em UTC. Selecionar vendas com `confirmadoEm` no intervalo, inclusive as atualmente canceladas. Rascunhos/Descartadas não entram.

- `quantidadeConfirmada`: número atualmente Confirmada no conjunto.
- `quantidadeCancelada`: número atualmente Cancelada no conjunto.
- `receitaLiquida`: soma do total das atualmente Confirmadas; Canceladas contribuem zero.
- `ticketMedio`: receitaLiquida / quantidadeConfirmada, arredondado a duas casas com MidpointRounding.AwayFromZero; zero se denominador zero.
- Relatório contém cada venda do conjunto, total original, status atual e contribuição líquida, sem apresentar cancelamento como exclusão da linha.
- Estoque baixo: produto ativo cujo saldo <= estoqueMinimo, inclusive saldo zero/mínimo zero.

Exemplo: duas vendas de 25,00 e 15,00 no período; a segunda cancelada posteriormente. Quantidade confirmada 1, cancelada 1, receita 25,00, ticket médio 25,00. Este relatório é visão atual do período original; não é fluxo de caixa nem posição contábil histórica.

## Incremento sem dependências futuras

RF-03 no bloco 01 entrega manutenção de perfil/status/senha do administrador bootstrap e estrutura das contas. Criação de contas humanas vinculadas é concluída no 02. RF-06 no 03 entrega histórico vazio com contrato estável e teste de resposta; no 06 demonstra histórico preenchido. RF-12 usa saldo criado no produto e movimentos somente depois do 05. Nenhum bloco precisa de endpoint inexistente para passar seu gate.
