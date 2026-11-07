# 06 — Auditoria de arquitetura .NET e MongoDB

> Registro histórico anterior à implementação atual00. A autorização e decisões ADR-27–29 permitem Atlas existente, testes em bancos isolados e nova estrutura física; código/Compose foram adaptados. O resultado vigente e limitações estão no estado03. As afirmações e contagens abaixo descrevem a auditoria daquela etapa e não substituem os resultados atuais.


## Resultado: BLOQUEADO

Auditoria de 06/11/2026, referência America/Cuiaba. Escopo: todos os40 Markdown existentes no início desta revisão, com correções exclusivamente documentais; este relatório é o41º. Nenhum módulo foi implementado, nenhum código/pacote/Compose/workflow foi alterado e nenhum teste de software foi apresentado como executado.

As inconsistências documentais identificadas foram corrigidas e a sequência de implementação é coerente a partir da futura adaptação do00. **A aprovação global solicitada permanece BLOQUEADA pelo item6:** o artefato `deploy/compose.yaml` ainda configura PostgreSQL e os hosts ainda recebem conexões da stack anterior; não há MongoDB/rs0 configurado nesse Compose. A previsão documental de replica set foi detalhada, mas isso não converte o artefato nem demonstra sua funcionalidade. Essa divergência material de aderência impede uma aprovação incondicional. O bloqueio não significa dependência circular entre módulos.

Para remover o bloqueio: futura adaptação explicitamente autorizada do00 deve alinhar Compose/hosts/pacotes/configurações/testes à arquitetura MongoDB, fixar versões e demonstrar bootstrap autenticado, descoberta/resolução de hostname, conexão dos dois hosts com menor privilégio, commit/abort, readiness200/503 e persistência do volume. Reauditar depois.01 e blocos posteriores continuam aguardando os gates anteriores. Esta auditoria não autoriza iniciar esse trabalho.

## Método e fontes

Inventário e leitura cruzada de AGENTS, README,10 arquivos de arquitetura,6 de requisitos,12 módulos e9 arquivos de execução preexistentes; varredura integral dos Markdown por tecnologias removidas, dinheiro, sessões, campos/referências, transações, índices, gates e cenários. Registros históricos foram distinguidos de instruções vigentes. Inspeção somente leitura do Compose existente confirmou a divergência relatada, sem executar infraestrutura. Comparação de referências/IDs/rotas/estrutura e hashes SHA-256 complementa a revisão semântica.

Fontes primárias para os mecanismos: [transações do driver oficial .NET](https://www.mongodb.com/docs/drivers/csharp/current/crud/transactions/), [retry de transação/commit](https://www.mongodb.com/docs/v8.0/core/transactions-in-applications/), [unicidade multikey](https://www.mongodb.com/docs/manual/core/index-unique/), [localhost exception](https://www.mongodb.com/docs/v8.0/core/localhost-exception/). As decisões do projeto estão na ADR-26 e nas normas05/07/09/10; as fontes não comprovam a implementação local.

## Verificação dos15 itens solicitados

| Item | Resultado após correções | Evidência documental/limite |
| --- | --- | --- |
|1. Ausência de exigência de stack removida | Conforme nas normas vigentes | AGENTS/arquitetura/módulos usam MongoDB.Driver; menções antigas restritas a histórico, proibições e incompatibilidades, sem comando normativo de execução relacional |
|2. Driver e decisões comuns | Conforme | Seção2 dos12 módulos explicita herança de arquitetura05/09 e ADR-20–26; não existe provedor alternativo ou Unit of Work genérica |
|3. Domínio independente | Conforme | AGENTS e arquitetura02/04/08 proíbem BSON/driver/ASP.NET Core/Blazor no domínio; long em centavos é tipo .NET, mapeamento BSON exclusivo da infraestrutura |
|4. Identificadores/campos/referências | Conforme após correção | UUID Standard para entidades/itens, strings técnicas delimitadas, hashes binários de32 bytes, ticket com _id distinto de handleHash; datas físicas de sales explícitas |
|5. Índices e consultas | Conforme na especificação após correção | Matriz consulta→índice→bloco na arquitetura05 cobre listagens gerais, próprias/cliente, movimentos globais, unicidades/índices parciais; explain/p95 ainda precisam execução |
|6. Compose/replica set/conexão | **Não conforme no artefato existente; bloqueante** | Plano10/roteiro07 corrigidos para rs0/primary/TLS/authSource/loopback; Compose atual ainda define outro banco e conexões antigas, sem réplica MongoDB; não executado |
|7. Transações comerciais | Conforme na especificação | Confirmação/cancelamento/movimento+saldo+auditoria+recibo na mesma sessão snapshot/majority/primary; sem chamadas paralelas/externas |
|8. Estoque concorrente | Conforme na especificação | products.saldo com filtros de saldo mínimo/capacidade máxima, resultado conferido e rollback inteiro; guardas efetivas de referências contra write skew |
|9. Testes pedidos | Conforme como previsão | Matriz explícita na arquitetura09 cobre os8 casos, com preparação/ação/assert/rastreio; nenhuma execução MongoDB alegada |
|10. Dependências/ordem | Conforme | Plano00 e grafo01 mantêm00→01→…→10;11 somente após escopo P2; adapta00 antes de usar evidência antiga |
|11. Sem software futuro obrigatório | Conforme |01 bootstrap sem employees;03 histórico vazio até06;04 saldo0 sem movimentos;05 sem sales;07 reusa estoque sem depender de08;09 ensaio sem destino10 |
|12. AGENTS/plano | Conforme após esclarecimento | Mesma baseline e limites de camada; somente Markdown nesta tarefa e nenhuma liberação automática de implementação |
|13. Dinheiro uniforme | Conforme | Long centavos internos/Int64 persistido, BRL; decimal em reais só HTTP/CSV/formatação, conversão exata, checked e média final AwayFromZero |
|14. Autenticação BFF/API | Conforme após correção | Cookie de referência, token opaco só servidor, stores Identity sobre driver, SSR+antiforgery, revalidação API, exceções de troca pendente/bootstrap explícitas |
|15. Aceites objetivos | Conforme como especificação | IDs globais/locais preservados; contratos HTTP e efeitos no banco verificáveis, testes de falha/concorrência/recibo e critérios operacionais do00 explícitos |

“Conforme na especificação” não significa implementação/teste aprovado. Sem resolver o item6, o resultado global permanece BLOQUEADO, mesmo com os demais itens documentalmente consistentes.

## Problemas identificados e correções realizadas

| ID | Problema e risco | Correção / estado |
| --- | --- | --- |
| A01 | Índice em confirmadoEm sem campo físico explícito em sales; módulo usava estado e dicionário status | Canonizado status e datas na raiz; metadados de ator/motivo separados, mapeamento HTTP explícito em05/06 |
| A02 | Índices por produto/criador/cliente não cobriam listagens gerais de eventos/vendas; busca de usuários sem nomeNormalizado | Adicionados nomes/campos e índices globais com direção/desempate, matriz por consulta/bloco, sem impor unicidade ao nome do produto |
| A03 | Tickets usavam `_id/handleHash`, ambíguo entre UUID e hash; convenção genérica de versão/timestamps conflitava com documentos técnicos | _id UUID e hash separado; hashes SHA-256 binários de32 bytes; schemaVersion comum e metadados comerciais/técnicos diferenciados |
| A04 | Recibo Preparando exigia recursoId apesar de resultado ainda não completo; cardinalidade mínima de itens por estado não explícita | recursoId opcional só enquanto Preparando e obrigatório em Concluido; commit somente completo; validator com mínimo1 em Confirmada/Cancelada e0–100 em Rascunho/Descartada |
| A05 | Texto REST ainda mandava ler recibo em nova transação sem esclarecer tentativa abortada; exceção de readiness não explícita | Consulta fora da transação abortada no primary/majority; nova tentativa completa apenas quando cabível; health interno excepcionado expressamente |
| A06 | Menções a saldo sob bloqueio, filtro bsonType em índice, termo corrompido de substituição antiga e componentes UI renomeados por engano | Atualizações condicionais, filtro parcial $type, texto de homologação legível e nomes TabelaVendasRelatorio/TabelaEstoque/TabelaMovimentos restaurados; tabela UI não é modelagem relacional |
| A07 | Norma de dinheiro citava BSON no domínio; trechos de04/08/11 podiam confundir valor interno com transporte | Long .NET no domínio/aplicação, BSON só infraestrutura, unidades explicitadas nos módulos e testes de conversão; sem mudança de significado dos contratos |
| A08 | Proposta11 guardava dedupe mas não garantia recibo original depois de encerrar pedido | reciboOriginal imutável incorporado em online_orders, índice unique em chavePublicaHash e comparação de hashRequest, sem ator fictício; retry após encerramento preserva resposta Recebido; versão0 da publicação definida |
| A09 | Bootstrap rs0/usuários podia ser interpretado como localhost exception de outro container | Ordem local isolada e autenticada especificada na arquitetura10/roteiro07, com hostname anunciado, SAN/CA e authSource por credencial; sem ciclo com readiness |
| A10 | Matriz exigia troca de senha concluída para a própria troca; exigia funcionário ativo para bootstrap e poderia antecipar employees em01 | Exceções explícitas me/logout/alterar-senha e bootstrap; vínculo humano começa02, sem coleção futura em01 |
| A11 | Previsão dos8 testes estava dispersa; DoD/counts e índice documental ainda usavam referências antigas incompletas | Matriz objetiva de testes na arquitetura09, DoD por inventário atual, índice completo dos relatórios/roteiro e norma comum em todos módulos |
| A12 | Compose/hosts atuais continuam incompatíveis com a nova arquitetura | **Permanece aberto e bloqueante para aprovação global**; somente documentado e verificado por leitura, sem alteração de código/infraestrutura nesta tarefa |

Decisões corretivas registradas na ADR-26; requisitos funcionais/limites/prioridades/rotas/ordem preservados. As contagens/verificações da revisão anterior foram preservadas como históricas, não atualizadas retroativamente para parecerem atuais.

## Decisões em aberto

- Fixar patches/digests compatíveis de servidor MongoDB, driver oficial, Testcontainers e Database Tools no futuro00 e comprovar restore/build/testes; não foi inventada uma compatibilidade já demonstrada.
- Q-01: confirmar premissas comerciais com responsável; seguir baseline UN inteira/cliente ativo/sem descontos enquanto isso, sem ampliar escopo.
- Q-02: destino, TLS/domínio/custo/operação e adequação da disponibilidade de um nó antes de10. Nó único habilita transações e não atende HA.
- Q-03: finalidade/retenção/tratamento de dados pessoais antes de dados reais; auditoria usa somente especificação e dados sintéticos.
- Q-04/ADR-18: aceitar ou ajustar a proposta P2 antes de11; nenhuma revisão técnica libera catálogo/pedido automaticamente.
- Índices adicionais e otimização de estoque baixo dependem de explain/carga, mantendo consultas/resultados e metas. Não são dependência de módulo futuro para funcionar.

## Riscos conhecidos

- MongoDB não fornece integridade referencial automática; esquecer uma escrita de guarda real pode permitir write skew mesmo com transação snapshot. Stores e adaptadores precisam testes de disputa específicos.
- Commit incerto, expiração de orçamento e resposta perdida exigem conservar chave e resolver recibo; repetir corpo/chave nova sem protocolo duplica intenção.
- Implementação dos stores Identity e coordenação da mesma sessão é trabalho real, sem provedor automático; testar lockout, último Admin, login/revogação e isolamento BFF.
- Nó único perde disponibilidade com host/primary indisponível; majority não protege contra perda física de disco. Backup/restore consistente, key ring e imagens precisam ensaio medido.
- Agregação monetária não pode promover overflow para double; exportações/totais precisam snapshot e acumulação exata. Planos/latências não foram medidos.
- Histórico de tecnologias/commands anteriores permanece identificado; executar esses comandos como se fossem o novo00 produziria falsa evidência. O estado e o roteiro alertam para isso.

## Validações executadas nesta auditoria

Somente verificações documentais/arquivos: links locais/cercas, IDs e seções, referências de rotas, dependências de módulos e hashes dos arquivos não-Markdown. Não foram executados restore/build, testes de software, Compose, inicialização MongoDB, transações ou backup/restore. A tabela dos8 testes documenta previsão, não resultado observado.

Resultado efetivamente observado: **41 Markdown verificados, 113 links locais válidos, zero links quebrados, zero cercas sem par e zero erros na verificação estrutural**. Os12 módulos mantêm15 seções cada;62 cenários locais e24 RF (20 MVP+4 P2),12 RNF,24 RN (20 MVP+4 P2) e18 CA globais preservados, sem rota original removida. Árvore de monorepo preservada. **66 arquivos não-Markdown preexistentes com SHA-256 inalterado**, sem novo arquivo não-Markdown visível ao Git. Diff de whitespace: exit0. Foram alterados32 Markdown existentes e criado este relatório; os demais8 foram revisados sem mudança necessária. Verificação estrutural bem-sucedida não remove o bloqueio material do item6.

## Arquivos revisados

Todos os40 Markdown preexistentes, inclusive históricos/relatório anterior; o relatório atual também foi validado ao final.

- `AGENTS.md`
- `README.md`
- `docs/README.md`
- `docs/architecture/01-VISAO-GERAL.md`
- `docs/architecture/02-ARQUITETURA.md`
- `docs/architecture/03-ESTRUTURA-MONOREPO.md`
- `docs/architecture/04-MODELO-DE-DOMINIO.md`
- `docs/architecture/05-BANCO-DE-DADOS.md`
- `docs/architecture/06-CONTRATOS-API.md`
- `docs/architecture/07-AUTENTICACAO-SEGURANCA.md`
- `docs/architecture/08-PADROES-CODIGO.md`
- `docs/architecture/09-TESTES-QUALIDADE.md`
- `docs/architecture/10-DEPLOY-AMBIENTES.md`
- `docs/execution/00-PLANO-EXECUCAO.md`
- `docs/execution/01-DEPENDENCIAS.md`
- `docs/execution/02-DEFINITION-OF-DONE.md`
- `docs/execution/03-ESTADO-PROJETO.md`
- `docs/execution/04-DECISOES-TECNICAS.md`
- `docs/execution/05-PROMPT-EXECUTOR.md`
- `docs/execution/06-MATRIZ-MODULOS.md`
- `docs/execution/07-EXECUCAO-LOCAL-FUNDACAO.md`
- `docs/execution/08-REVISAO-MONGODB.md`
- `docs/modules/00-FUNDACAO.md`
- `docs/modules/01-AUTENTICACAO.md`
- `docs/modules/02-FUNCIONARIOS.md`
- `docs/modules/03-CLIENTES.md`
- `docs/modules/04-PRODUTOS.md`
- `docs/modules/05-ESTOQUE.md`
- `docs/modules/06-VENDAS.md`
- `docs/modules/07-DASHBOARD.md`
- `docs/modules/08-RELATORIOS.md`
- `docs/modules/09-HOMOLOGACAO.md`
- `docs/modules/10-PUBLICACAO.md`
- `docs/modules/11-CATALOGO-PEDIDOS.md`
- `docs/requirements/01-ESCOPO.md`
- `docs/requirements/02-REQUISITOS-FUNCIONAIS.md`
- `docs/requirements/03-REQUISITOS-NAO-FUNCIONAIS.md`
- `docs/requirements/04-REGRAS-DE-NEGOCIO.md`
- `docs/requirements/05-MATRIZ-PERMISSOES.md`
- `docs/requirements/06-CRITERIOS-ACEITE.md`

## Arquivos documentais alterados nesta auditoria

- `AGENTS.md`
- `README.md`
- `docs/README.md`
- `docs/architecture/02-ARQUITETURA.md`
- `docs/architecture/04-MODELO-DE-DOMINIO.md`
- `docs/architecture/05-BANCO-DE-DADOS.md`
- `docs/architecture/06-CONTRATOS-API.md`
- `docs/architecture/07-AUTENTICACAO-SEGURANCA.md`
- `docs/architecture/09-TESTES-QUALIDADE.md`
- `docs/architecture/10-DEPLOY-AMBIENTES.md`
- `docs/execution/00-PLANO-EXECUCAO.md`
- `docs/execution/01-DEPENDENCIAS.md`
- `docs/execution/02-DEFINITION-OF-DONE.md`
- `docs/execution/03-ESTADO-PROJETO.md`
- `docs/execution/04-DECISOES-TECNICAS.md`
- `docs/execution/05-PROMPT-EXECUTOR.md`
- `docs/execution/06-AUDITORIA-MONGODB.md`
- `docs/execution/06-MATRIZ-MODULOS.md`
- `docs/execution/07-EXECUCAO-LOCAL-FUNDACAO.md`
- `docs/execution/08-REVISAO-MONGODB.md`
- `docs/modules/00-FUNDACAO.md`
- `docs/modules/01-AUTENTICACAO.md`
- `docs/modules/02-FUNCIONARIOS.md`
- `docs/modules/03-CLIENTES.md`
- `docs/modules/04-PRODUTOS.md`
- `docs/modules/05-ESTOQUE.md`
- `docs/modules/06-VENDAS.md`
- `docs/modules/07-DASHBOARD.md`
- `docs/modules/08-RELATORIOS.md`
- `docs/modules/09-HOMOLOGACAO.md`
- `docs/modules/10-PUBLICACAO.md`
- `docs/modules/11-CATALOGO-PEDIDOS.md`
- `docs/requirements/05-MATRIZ-PERMISSOES.md`

Os demais Markdown foram revisados e preservados por não exigir correção. Arquivos não-Markdown existentes foram somente lidos/comparados.

## Encerramento

Resultado global: **BLOQUEADO** pelo A12/item6. Documentos corrigidos permitem a implementação sequencial da nova baseline, começando pela adaptação autorizada do00; não há módulo futuro exigido para concluir um predecessor. Essa coerência de plano não aprova o Compose/hosts existentes nem libera01. Nenhuma implementação será iniciada como parte desta auditoria.
