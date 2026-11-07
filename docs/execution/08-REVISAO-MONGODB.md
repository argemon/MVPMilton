# 08 — Auditoria e revisão documental MongoDB

> Registro histórico anterior à implementação atual00. A autorização e decisões ADR-27–29 permitem Atlas existente, testes em bancos isolados e nova estrutura física; código/Compose foram adaptados. O resultado vigente e limitações estão no estado03. As afirmações e contagens abaixo descrevem a auditoria daquela etapa e não substituem os resultados atuais.


Registro da primeira revisão, preservado com suas contagens/evidências. A auditoria posterior [06-AUDITORIA-MONGODB.md](06-AUDITORIA-MONGODB.md) e ADR-26 prevalecem para o resultado vigente. “Pronta para revisão” abaixo não significa aprovação global do repositório.

## Escopo, autorização e estado

Revisão de 06/11/2026, referência America/Cuiaba, solicitada exclusivamente para Markdown. Os 39 documentos existentes foram auditados e atualizados nos próprios caminhos; este relatório é o único Markdown novo. A estrutura dos documentos, monorepo, módulos 00–11, prioridades e escopo funcional foram preservados. Não houve implementação de bloco, conversão de código, execução de banco ou importação de dados.

Baseline vigente: .NET10, ASP.NET Core API, Blazor Interactive Server, MongoDB, MongoDB.Driver oficial, Clean Architecture/SOLID/KISS/DRY, xUnit, Testcontainers com replica set e Docker Compose. Decisões novas em [ADR-20–25](04-DECISOES-TECNICAS.md). Código preexistente da fundação ainda depende da stack anterior e não satisfaz essa baseline. [Estado vigente](03-ESTADO-PROJETO.md) e [roteiro alvo](07-EXECUCAO-LOCAL-FUNDACAO.md) registram a incompatibilidade sem apagar evidências anteriores.

## Auditoria dos pontos afetados

| Ponto encontrado | Adaptação documental | Regras preservadas |
| --- | --- | --- |
| ORM/contextos/migrations e SQL | Driver oficial, documentos de persistência restritos à infraestrutura, inicialização/evolução explícita de coleções/validadores/índices | Camadas, monorepo, configuração externa, gates e testes |
| Tipos/tabelas/referências relacionais | UUID Standard, BSON Int32/Int64/Date; referência validada em transação, sem FK/cascade presumidos | Unicidade inclusive inativos, vínculo único, preservação de autoria/histórico |
| Produto/saldo/imagem separados | products incorpora saldo obrigatório e metadados opcionais de imagem | Saldo inicial0, sem entrada fictícia, imagens privadas e troca versionada |
| Venda/itens separados | sales incorpora até100 itens e snapshots; movimento/auditoria fora do agregado | Estados, preço servidor, total exato, uma baixa/estorno por item, histórico imutável |
| Locks e isolamento anteriores | Sessão snapshot/majority/primary, updates condicionais e escritas efetivas contra write skew | Saldo nunca negativo, confirmação/cancelamento atômicos, preço/ativo válidos |
| Retry e falha após commit | TransientTransactionError repete corpo; UnknownTransactionCommitResult repete commit; chave/recibo resolve resultado | Mesma intenção não duplica efeito, timeout não significa rollback |
| Dinheiro decimal interno | Centavos long/BSON Int64, BRL, checked, conversão exata na borda, média final AwayFromZero | Limites e valores comerciais, contratos/CSV em reais, terceira casa rejeitada |
| Identity e sessões | Hash/políticas Identity com stores próprios mínimos sobre driver; transação compartilhada | Cookie de referência/BFF, sessão opaca, perfis, lockout, CSRF, revogação e último Admin |
| Infra/testes | Replica set local de um nó, primary/versões no readiness, Testcontainers com sessões independentes | Separação API/Web/banco, health mínimo, evidência real, sem skips silenciosos |
| Operação/recuperação | Roles por coleção/banco, keyfile/segredos externos, volume, backup consistente e restore isolado | RPO/RTO/retenção, TLS, mínimo privilégio, Q-02/Q-03 e gate de publicação |

Coleções mínimas sugeridas foram confrontadas com o escopo: users, employees, customers, categories, products, stock_movements e sales são pertinentes. brands é necessária pelo CRUD/marca opcional já exigidos; api_sessions, audit_events, idempotency_receipts, administration_control, persistence_versions e tickets cobrem requisitos técnicos existentes. Não se criam coleções por entidade automaticamente: saldo/imagem/itens são incorporados. online_orders e publicação incorporada permanecem proposta P2 do11, sem implementação/liberação.

## Alterações incompatíveis e limites

1. Pacotes, hosts, factories, contexts, migrations/SQL, testes executáveis, Compose e pipeline atuais não foram convertidos. Evidências antigas de build/teste/UI não aprovam MongoDB. Futura adaptação começa em00, sem pular para 01.
2. Não existe substituição automática entre ORM e driver: futuras transações/referências/índices/stores precisam implementação e provas novas. Snapshot isolado não garante validade de referências; guardVersion exige mudança efetiva. Índice multikey único não impede duplicata dentro de um mesmo array.
3. Valores persistidos/domínio passam a centavos Int64. Importação futura precisa conversão exata, rejeições documentadas, backup e reconciliação; nenhuma importação realizada. API/CSV permanecem em reais e não devem trocar unidade sob nomes antigos.
4. Stores de Identity sobre driver são trabalho futuro do01. Modelo BFF e políticas não mudam; não foi introduzido JWT, provider comunitário ou hash próprio.
5. MongoDB standalone não atende transações. Replica set de um nó habilita atomicidade, mas não oferece HA nem proteção contra perda do disco. Majority/journal/backup e limites de disponibilidade precisam validação operacional; Q-02 continua aberta.
6. Evolução de índices/validadores não é uma transação única de schema: tarefa explícita, checkpoint, checksum, verificação e retomada segura são necessários. Runtime não administra schema nem altera histórico. Nenhuma evolução no startup.
7. Referências às tecnologias removidas permanecem apenas no histórico delimitado, nas proibições e no registro de incompatibilidades. Preservar o que foi realmente executado evita atribuir provas inexistentes à stack nova.

## Verificações documentais

Validação automatizada de todos os Markdown: caminhos/links locais, cercas de código, sequência de15 seções em cada módulo, preservação dos IDs de requisitos e dos 62 cenários locais em comparação com a versão Git inicial. Conferência da árvore de monorepo e hashes SHA-256 de todos os arquivos não-Markdown visíveis ao Git no início desta revisão, incluindo os já não rastreados da fundação. Varredura de referências antigas diferencia normas atuais de histórico/proibições. Resultados efetivos são registrados no fechamento abaixo; não equivalem a testes MongoDB.

Fontes primárias foram consultadas e vinculadas nos documentos normativos: transações/atomicidade/validadores/índices/RBAC e ferramentas MongoDB; stores Identity Microsoft; módulo Testcontainers .NET. As fontes explicam mecanismos; modelagem, limites e protocolo são decisões do projeto que precisam ensaio de implementação.

## Pendências reais

- Revisão humana das ADR-20–25 e desta documentação; especialmente centavos internos com compatibilidade HTTP, stores Identity e guarda contra write skew.
- Em implementação futura autorizada do00: fixar versões/patches/digests compatíveis de MongoDB, MongoDB.Driver, Testcontainers e Tools; adaptar código/pacotes/infra/tests e executar todos os gates novamente. Nenhuma versão nova foi alegada como testada.
- Validar inicialização replica set/keyfile/roles, índices/validadores, idempotência do inicializador, TLS/hostnames, readiness, commit/abort e persistência de volume com ambiente real.
- Em módulos posteriores, comprovar concorrência, commits incertos, limites/overflow, stores/lockout/revogação e recuperação consistente. Prazos/metas não foram reduzidos.
- Q-01–04 e decisões de produção/privacidade permanecem abertas. Proposta11 continua P2, sem implementação automática.

Nenhum restore/build/teste de software, Compose ou backup/restore foi executado nesta tarefa documental. Documentação pronta para revisão não significa sistema pronto nem bloco liberado.

## Arquivos existentes modificados

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

Arquivo novo desta revisão: `docs/execution/08-REVISAO-MONGODB.md`. São 39 arquivos existentes atualizados e 1 relatório novo; nenhum documento anterior removido ou renomeado.

## Resultado do fechamento

Verificação executada por leitura/expressões de estrutura e comparação SHA-256 em 06/11/2026: **40 Markdown, 103 links locais verificados, zero links quebrados e zero erros**. Os 12 módulos mantêm 15 seções cada e os 62 cenários locais originais; catálogos mantêm 24 RF (20 MVP+4 P2 propostos), 12 RNF, 24 RN (20 MVP+4 P2 propostas) e 18 CA globais. Árvore de monorepo preservada literalmente em relação ao Git inicial; rotas originais nos módulos preservadas. **66 arquivos não-Markdown preexistentes preservaram seus hashes**, sem novo arquivo não-Markdown visível ao Git.

Varredura das normas atuais não encontrou PostgreSQL/Npgsql/DbContext/DbSet como tecnologia de implementação; termos de stack removida aparecem somente em histórico, proibições, comparação de mecanismo e incompatibilidades declaradas. Checagem de whitespace do diff executada após os ajustes finais. Não se executou teste de software; não há prova de instalação/compatibilidade MongoDB. **Documentação pronta para revisão. Gate 00 e avanço para 01 não aprovados.**
