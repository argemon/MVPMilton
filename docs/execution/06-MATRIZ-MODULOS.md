# 06 — Matriz de execução dos módulos

Revisão documental em 06/11/2026. Esta matriz resume os documentos completos dos módulos; requests/responses/erros/estados/testes detalhados ficam em cada um. Nenhum software ou teste foi implementado nesta revisão.

## Convenções

API interna usa base `/api/v1`, salvo health; rotas de páginas são do host Blazor. `G/P/U/D` = GET/POST/PUT/DELETE, respectivamente. DELETE só existe para imagem substituível, nunca entidade comercial. P0/P1 integram MVP; P2 do 11 é proposta posterior condicionada a aceite. Dependências00–N significam todos os gates anteriores aprovados, não autorização para pular etapas.

## Matriz consolidada

| Módulo | Requisitos / prioridade | Endpoints e métodos previstos | Páginas / componentes principais | Dependências concluídas | Testes previstos |
| --- | --- | --- | --- | --- | --- |
| [00 Fundação](../modules/00-FUNDACAO.md) | RNF-04/07/08/09/10; P0 técnico, sem RF comercial | G `/health/live`, `/health/ready` | `/`, `/erro`, `/indisponivel`; LayoutPrincipal/EstadoApi | Nenhum bloco; SDK/Docker/configuração | xUnit configuração/erros; MongoDB/baseline/health503/reinício/referências; M00-CA01–04 |
| [01 Autenticação](../modules/01-AUTENTICACAO.md) | RF-01/02 completos e RF-03 parcial; P0 | P auth login/logout/alterar-senha; G auth/me; G usuarios e detalhe; U usuario; P redefinir-senha; POST usuários não existe ainda | `/conta/entrar`, `/conta/senha`, `/usuarios`, `/usuarios/{id}`, `/`; formulários SSR/perfil | 00; bootstrap independente de Funcionário | Expiração/Identity/CSRF/cookie/isolamento/revogação/últimoAdmin; CA-01/02/17 parcial; M01-CA01–05 |
| [02 Funcionários](../modules/02-FUNCIONARIOS.md) | RF-04 P1; concluir RF-03 P0 | G/P funcionarios; G/U detalhe; P usuarios vinculado | `/funcionarios`, `/funcionarios/novo`, `/funcionarios/{id}`, `/usuarios/novo`; formulário/vínculo | 00–01, Identity/sessões/auditoria | Contatos/CRUD/referências/índices únicos/versão/conta versus inativação/revogação; CA-03; M02-CA01–05 |
| [03 Clientes](../modules/03-CLIENTES.md) | RF-05 P1; RF-06 parcial P1 | G/P clientes; G/U detalhe; G cliente/vendas vazio tipado | `/clientes`, `/clientes/novo`, `/clientes/{id}`; DocumentoPfPj/HistoricoCliente | 00–02; nenhuma coleção Venda | CPF/CNPJ/duplicidade/versão/filters401/403/histórico vazio; CA-04 parcial; M03-CA01–05 |
| [04 Produtos](../modules/04-PRODUTOS.md) | RF-07/08 P1 | G/P categorias/marcas/produtos; G/U detalhes; G/P/D imagem | `/produtos`, `/produtos/novo`, `/produtos/{id}`, `/categorias`, `/marcas`; preço/imagem/referências | 00–03; volume privado | Preço/SKU/arquivos/escritas de guarda de referência/produto+saldo 0 atômicos; CA-05/17 parcial; M04-CA01–05 |
| [05 Estoque](../modules/05-ESTOQUE.md) | RF-09/10/11 P0; RF-12 P1 | G saldos/movimentos/detalhe; P entradas/saidas/ajustes | `/estoque`, `/estoque/movimentos`, `/estoque/entrada`, `/estoque/saida`, `/estoque/ajuste`; quantidade/delta/recibo | 00–04, produto/saldo existente | Sinais/limites/MongoDB concorrente/idempotência/rollback/razão/ausência de saldo; CA-06/07; M05-CA01–05 |
| [06 Vendas](../modules/06-VENDAS.md) | RF-13/14/16 P0; RF-15 P1; concluir RF-06 P1 | G/P vendas; G/U detalhe; P confirmacao/cancelamento/descarte; G cliente/vendas real | `/vendas`, `/vendas/nova`, `/vendas/{id}`; editor/total/confirmar/cancelar; histórico do cliente | 00–05; cliente/produto/movimento/idempotência | Centavos Int64/estado/inativo/multi-item/escritas de guarda/disputa/estorno/snapshots/permissão; CA-04 e 08–13; M06-CA01–07 |
| [07 Dashboard](../modules/07-DASHBOARD.md) | RF-17/18 P1 | G dashboard; reutiliza G estoque/saldos | `/dashboard`; filtro/cartões/evolução/ranking/recentes/baixo estoque; link `/estoque?abaixoMinimo=true` | 00–06; sem Relatórios | Fórmulas/AwayFromZero/fuso/zero/snapshot/403/limite20; CA-14/15; M07-CA01–05 |
| [08 Relatórios](../modules/08-RELATORIOS.md) | RF-19/20 P1 | G relatorios/vendas/operadores/produtos/estoque/movimentos e cada exportacao; handlerWeb G downloads/relatorios/{tipo} | `/relatorios/vendas`, `/relatorios/operadores`, `/relatorios/produtos`, `/relatorios/estoque`, `/relatorios/movimentos`; filtros/totais/CSV | 00–07; sem serviço de PDF/email | Totais versus página/período/snapshot/limite10000/CSV fórmula/403; CA-14/15/17; M08-CA01–05 |
| [09 Carga de demonstração](../modules/09-CARGA-DEMONSTRACAO.md) | Gerar dados fictícios para o TCC | Painel administrativo de carga/restauração | `/administracao/dados-demonstracao` (ou rota equivalente) | 00–08; mesmo banco Atlas | Repetição sem duplicidade, restauração seletiva, consistência de saldo/vendas/relatórios |
| [10 Homologação](../modules/10-HOMOLOGACAO.md) | Revisão final de 00–09 | Exercitar contratos existentes; sem novas APIs | Todas as páginas, incluindo carga, em navegador responsivo | 00–09 | Fluxos ponta a ponta, correções, revisão UI/UX e roteiro TCC |

## Verificação de ausência de dependência futura

- 00 não cria autenticação/coleções comerciais; replica set/primary, catálogos técnicos e inicialização idempotente validam a fundação.
- 01 usa bootstrap sem Funcionário; fixtures isoladas de perfis não são endpoints nem contas públicas alternativas. referência de funcionário com índice único parcial só em 02.
- 02 conclui conta vinculada e usa auditoria existente para comprovar preservação, sem exigir venda futura.
- 03 retorna histórico vazio com contrato estável e marca RF-06 parcial; seu gate não exige preenchimento que só chega no 06.
- 04 cria produto e saldo 0, sem operação de estoque. Gate não exige entrada para tornar produto testável.
- 05 usa somente produto/saldo e movimentos manuais; referências/índices de venda entram no 06, sem coleção futura na evolução de persistência05.
- 06 depende de Estoque concluído e comprova ciclo pela API/histórico; não precisa dashboard/relatório para reconciliar.
- 07 usa lista de estoque já entregue, sem link obrigatório para relatório futuro.
- 08 exporta CSV com recursos locais, sem PDF, email ou serviço posterior.
- 09 faz restore/carga em ensaio equivalente; não precisa aguardar host final do 10 para passar seu gate.
- 10 faz restore/smoke da topologia final e termina MVP; não precisa catálogo.
- 11 especifica proposta independente de gateway, reserva, entrega ou módulo12. Está condicionado a **decisão externa de escopo**, não a implementação futura. Q-04 não é considerada resolvida automaticamente.

**Conclusão documental:** nenhum gate exige software de bloco posterior. Dependências técnicas são exclusivamente anteriores; disponibilidade de ambiente, definições de operação/privacidade e aceite de escopo permanecem condições explícitas.

## Uso pelo executor

Ler módulo completo e fontes normativas, não implementar pela tabela resumida. Executar checklist e DoD; registrar arquivos, evoluções de persistência, comandos, resultados e evidências no estado. Estados “pendente” deste projeto continuam pendentes após esta revisão documental.

Auditoria vigente e correções de consistência: [06-AUDITORIA-MONGODB.md](06-AUDITORIA-MONGODB.md), ADR-26. O prefixo06 deste relatório não substitui esta matriz; os dois caminhos têm finalidades distintas. Blocos herdam o contrato comum MongoDB.Driver/replica set/Testcontainers e os campos/índices físicos da arquitetura05; os índices necessários para a consulta de um bloco são criados até o próprio bloco, nunca como gate de software futuro.

Revisão MongoDB: produtos incorporam saldo/imagem (04), vendas incorporam itens/snapshots (06); movimentos/auditoria/recibos são coleções separadas. Referências são verificadas na transação e índices substituem apenas unicidade, não integridade referencial. 00 precisa adaptação antes de comprovar gates; todos os testes de integridade exigem Testcontainers com replica set. A sequência e a ausência de dependências futuras continuam válidas.
