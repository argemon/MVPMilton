# 02 — Requisitos funcionais

## Identificação e rastreabilidade

Prioridade e atores devem ser lidos com a [matriz](05-MATRIZ-PERMISSOES.md). A coleção define o comportamento mínimo; regras RN e contratos completam os detalhes.

| ID | Prioridade / ator | Fluxo e resultado observável | Regras | Aceite / bloco |
| --- | --- | --- | --- | --- |
| RF-01 | P0 / todos | Entrar por email/senha, abrir sessão e mostrar nome/perfil; falha genérica | RN-15/16 | CA-01 / 01 |
| RF-02 | P0 / todos | Sair revoga sessão; expiração e alteração de acesso impedem novas operações | RN-15/16 | CA-01/02 / 01 |
| RF-03 | P0 / Admin | Criar conta vinculada, definir perfil, ativar/desativar e redefinir senha sem revelar hashes | RN-15/16/18 | CA-02/17 / 01–02 |
| RF-04 | P1 / Admin | Cadastrar/editar/listar funcionário e desativar com revogação de acesso vinculado | RN-08/16/18 | CA-03 / 02 |
| RF-05 | P1 / Admin, Vendedor | Cadastrar/editar/pesquisar cliente por nome/documento e desativar | RN-08/12 | CA-04 / 03 |
| RF-06 | P1 / Admin, Vendedor | Consultar histórico paginado do cliente respeitando escopo de vendas | RN-07/17 | CA-04/13 / 03 e 06 |
| RF-07 | P1 / Admin, Estoquista | Manter categorias/marcas, sem nome normalizado duplicado | RN-08/12 | CA-05 / 04 |
| RF-08 | P1 / Admin, Estoquista | Manter produto com SKU, preço, imagem, categoria, marca opcional e status | RN-01/08/12/19 | CA-05/17 / 04 |
| RF-09 | P0 / Admin, Estoquista | Registrar entrada manual com quantidade e motivo; saldo e histórico juntos | RN-02/06/09/14 | CA-06 / 05 |
| RF-10 | P0 / Admin, Estoquista | Registrar saída manual sem gerar saldo negativo | RN-02/06/09/14 | CA-06/07 / 05 |
| RF-11 | P0 / Admin | Ajustar saldo por delta justificado, preservando movimento anterior | RN-02/06/09/14 | CA-06/07/13 / 05 |
| RF-12 | P1 / leitores de estoque | Listar saldo e histórico por produto/período/tipo | RN-07/09/20 | CA-06/15 / 05 |
| RF-13 | P0 / Admin, Vendedor | Criar/editar/descartar rascunho com cliente e itens; total calculado no servidor | RN-01/10/11/17 | CA-08 / 06 |
| RF-14 | P0 / Admin, Vendedor | Confirmar rascunho, congelar snapshots e baixar estoque atomicamente | RN-02/03/04/05/10/11 | CA-08/09/10/11 / 06 |
| RF-15 | P1 / Admin, Vendedor | Consultar venda, itens, estado e histórico do ciclo com paginação | RN-07/17 | CA-08/13/14 / 06 |
| RF-16 | P0 / Admin | Cancelar confirmada com motivo e estorno exato uma única vez | RN-05/06/07/10 | CA-12/13 / 06 |
| RF-17 | P1 / Admin | Ver indicadores por período conforme fórmula abaixo | RN-13/20 | CA-14/15 / 07 |
| RF-18 | P1 / Admin | Ver produtos ativos com saldo <= mínimo e acesso à lista completa | RN-02/13 | CA-14 / 07 |
| RF-19 | P1 / Admin | Relatório paginado/CSV de vendas com totais do filtro completo | RN-07/13/19/20 | CA-14/15/17 / 08 |
| RF-20 | P1 / Admin, Estoquista nos relatórios de estoque | Relatórios/CSV de saldo atual e movimentações filtradas com totais | RN-09/19/20 | CA-15/17 / 08 |

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

## Persistência e unidade monetária — revisão técnica

RF-01–20 mantêm o comportamento e RF-21–24 [históricos, fora do escopo atual] foram retirados do escopo atual. Implementação alvo usa MongoDB.Driver e transações MongoDB para efeitos multi-documento. RF-08/13/14/16/17/19 preservam preços/totais/snapshots em centavos Int64 internos, com mesma apresentação em reais e arredondamento final da média. RF-09–12 usam saldo incorporado em products e movimentos separados; RF-03 mantém vínculo único e revogação atômica. Fórmulas, filtros, endpoints e limites acima continuam válidos.

## Incremento autorizado07–08 — ADR-36

RF-17 inclui produtos cadastrados/ativos, evolução diária, vendas recentes e ranking por quantidade confirmada. RF-19 é ampliado no08 com visão por operador criador e ranking paginado/CSV, além de vendas por período. Identificação histórica vem dos snapshots; funcionário/operador é o usuário criador, inclusive bootstrap sem vínculo, nunca vínculo atual inferido. Permissões e fórmulas permanecem iguais. Sem PDF.
