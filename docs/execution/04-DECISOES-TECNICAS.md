# 04 — Decisões técnicas, hipóteses e questões

## Baseline vigente — revisão MongoDB, 06/11/2026

A solicitação explícita do usuário substitui a persistência anterior e autoriza **somente documentação** nesta revisão. ADR-20–25 abaixo prevalecem nos mecanismos afetados; ADR-01/02/04/08–14 preservam suas regras funcionais. As ADR-03/05/06/07/16/19 e detalhes de armazenamento das demais ficam **superados nesses pontos**, sem apagar o histórico. Nenhuma evidência de software anterior comprova a nova stack.

| ADR | Decisão vigente e motivo | Impactos e incompatibilidades |
| --- | --- | --- |
| ADR-20 | MongoDB com MongoDB.Driver oficial, IMongoClient singleton por configuração/credencial; MongoDB 8.0 como linha de referência, patches de servidor/driver/Testcontainers a fixar e validar na futura adaptação 00 | Remove PostgreSQL/Npgsql/EF Core/DbContext/DbSet, scripts SQL e migrations EF da arquitetura futura. Código, manifests e infraestrutura existentes não são alterados nesta tarefa e permanecem incompatíveis. Tarefa operacional explícita e versionada cria/valida coleções, validadores e índices, sem migrador genérico nem Unit of Work artificial. |
| ADR-21 | Modelagem documental: products incorpora saldo e imagem; sales incorpora até 100 itens e snapshots; users/employees/customers/categories/brands permanecem referências, assim como movimentos, sessões, auditoria e recibos | Sem FKs/cascades/checks relacionais. Integridade entre coleções é validada no backend dentro da mesma transação com escrita efetiva nos documentos de referência quando necessária contra write skew. UUID preservado; mapeamento BSON somente em Infrastructure e persistência técnica Web. |
| ADR-22 | Transações em replica set, ReadConcern.Snapshot, WriteConcern.WMajority e ReadPreference.Primary; sessão por operação; operações sequenciais e filtros condicionais; retry por rótulos oficiais | Leitura de snapshot isolada não impede referência ficar obsoleta. Escritas de guarda efetivas em referências e controle administrativo, saldo condicionado e comparação de versão substituem locks relacionais. Commit incerto não autoriza repetir efeitos com nova chave. Detalhamento normativo em arquitetura 05; sem reservas no MVP. |
| ADR-23 | Dinheiro no domínio/aplicação em centavos long, mapeado na infraestrutura para BSON Int64, moeda BRL. DTOs HTTP continuam números decimais em reais com até duas casas, conversão explícita somente na borda | Representação interna incompatível com o código/especificação anterior; sem mudar significado, nomes ou unidades dos campos HTTP. Limites comerciais preservados; operações checked e agregações sem promoção para double. Arredondamento AwayFromZero apenas na média final. Qualquer futura importação exige conversão exata, reconciliação e ensaio, nunca alteração silenciosa de tipo. |
| ADR-24 | Preservar BFF, sessão API opaca e cookie de referência. Reutilizar ASP.NET Core Identity para hash/políticas/lockout com stores próprios mínimos sobre MongoDB.Driver | Não existe troca automática de provedor: implementar e testar stores necessários, compartilhando sessão interna na transação administrativa. Sem provider comunitário presumido, hash próprio ou JWT por conveniência. Segurança/autoria/perfis e vínculo incremental 01→02 preservados. |
| ADR-25 | Compose local com replica set de um nó rs0, autenticação/keyfile externo, volume e primary verificável; Testcontainers com replica set real; backup/restore consistente e privilégio mínimo | Nó único habilita transações, não alta disponibilidade. Sem standalone para testes transacionais. Produção permanece condicionada a Q-02/Q-03, durabilidade e restore medidos; não inventar SLA. Runbook anterior do bloco 00 substituído por roteiro documental futuro; gate 00 volta a exigir adaptação e novas evidências. |

Decisões desta tabela registradas antes das alterações normativas. Requisitos comerciais, prioridades, IDs, monorepo e ordem 00–11 preservados. Q-01–04 não são resolvidas por esta revisão. Referências oficiais usadas e limites de implementação constam nos documentos de arquitetura 05, 07, 09 e 10.

## ADR-26 — Fechamento da auditoria de arquitetura (06/11/2026)

Sem alteração funcional: fixar campos físicos temporais de sales, chaves/formatos técnicos, metadados por tipo de documento e matriz mínima consulta→índice; explicitar inicialização local rs0 via loopback do próprio mongod, URI por rede/credencial e readiness. Em proposta P2, guardar recibo público original imutável dentro de online_orders para que encerramento não altere retry antigo. Corrigir terminologia remanescente de locks/campos/centavos e leitura de recibo após abort. Todos os módulos usam o mesmo driver/transações/testes por herança normativa de arquitetura05/09, sem dependências de software futuro. Estes esclarecimentos não adicionam features nem liberam11.

O Compose e os hosts existentes continuam incompatíveis com MongoDB e foram somente inspecionados. A auditoria solicitada inclui aderência desse artefato à previsão arquitetural (item6); enquanto ele ainda configura outro banco e não rs0, a aprovação global é BLOQUEADA, mesmo com correções documentais realizadas. A remoção desse bloqueio exige futura adaptação autorizada do00 e reauditoria, não edição de produção nesta tarefa. Relatório: [06-AUDITORIA-MONGODB.md](06-AUDITORIA-MONGODB.md).

## ADR-27–29 — Implementação autorizada00 com Atlas e configuração local

- **ADR-27:** conexão já fornecida em .env identifica Atlas SRV e será preservada. Desenvolvimento carrega .env local apenas em Development, com variáveis do processo prevalecendo. URI nunca é logada ou copiada para Markdown. Mongo__ConnectionString específico do host prevalece sobre MONGODB_URI; bancos gestao_comercial_dev/gestao_web_dev separados. Production/Staging não leem .env nem usam fallback da credencial de desenvolvimento; exigem configuração externa específica e usuários de menor privilégio separados. O único usuário fornecido pode servir aos dois bancos somente no desenvolvimento, risco explícito aceito para esta etapa; nenhum usuário/role Atlas é criado automaticamente.
- **ADR-28:** Atlas válido substitui a exigência universal de replica set local rs0 e Docker. Detectar hello autenticado (replica set com sessões ou mongos compatível) e comprovar commit/abort em banco exclusivo da execução, sem coleções comerciais. Não fixar rs0 em Atlas. Integração usa TEST_MONGODB_URI externo quando presente; senão reutiliza URI de desenvolvimento em banco com prefixo gc_m00_ e sufixo _test, UUID exclusivo. Limpeza somente do nome reservado pela fixture; sem apagar banco externo existente. Testcontainers continua disponível como perfil local/CI explícito, sem iniciar outro servidor quando Atlas atende. Reinício do host+reexecução do inicializador validam persistência remota; testes de volume/eleição local só se houver perfil local, não justificam uma segunda instância de desenvolvimento.
- **ADR-29:** a árvore física solicitada agora é backend/GestaoComercial.{Domain,Application,Infrastructure,Api}, frontend/GestaoComercial.Web, shared/GestaoComercial.Contracts e tests/GestaoComercial.{UnitTests,IntegrationTests}. Mover projetos anteriores e substituir baselines/factories/SQL/pacotes por MongoDB.Driver3.11.2 oficial e inicializador explícito versionado. SDK10.0.401, ASP.NET10.0.12, xUnit2.9.3; Testcontainers.MongoDb4.14.0 para perfil opcional real, compatibilidade a comprovar por restore/build. Sem tabelas/entidades futuras, Generic Repository ou Unit of Work. Dois hosts têm adaptadores técnicos independentes, Web referencia somente Contracts. Código comum de bootstrap de host (.env) é fonte compilada em ambos via build, sem assembly compartilhado de regras ou dependência de Domain/Application.

Complemento técnico validado: Atlas limita nomes de banco a38 bytes em planos compartilhados; fixture usa36 caracteres com96 bits aleatórios e limpeza estritamente limitada. Referência oficial: https://www.mongodb.com/docs/atlas/reference/free-shared-limitations/ . Inicialização cria índice único de version e valida checksum/validator sem alteração no startup.

Esta autorização supera a etapa somente documental das ADR-20–26 e o bloqueio da auditoria anterior deve ser reavaliado contra os arquivos/resultados finais, sem apagar evidências históricas. Compose de desenvolvimento será de API/Web com conexão externa, sem serviço de banco substituto. Inicialização de persistence_versions é tarefa explícita --initialize-persistence, nunca no startup normal. O teste de transação escreve somente uma coleção técnica do banco isolado da fixture e remove apenas seu banco ao encerrar.


## ADR-30 — Implementação autorizada01 (06/11/2026)

A solicitação atual libera somente autenticação/autorização, incluindo manutenção de contas prevista em M01. Substitui a lista de quatro perfis da ADR-09 por **Administrador, Vendedor e Estoquista**. As permissões de Vendedor/Estoquista permanecem; permissões antigas exclusivas de Gerente ficam com Administrador. Referências históricas a Gerente não criam perfil válido. Não migrar automaticamente dados de perfil desconhecido: bloquear e tratar por evolução revisada.

Preservar ADR-24/27–29: Atlas existente, .env intocado, API com sessão opaca e Web com cookie de referência/ITicketStore, tokens e tickets cifrados por Data Protection em banco técnico próprio. Identity com stores mínimos na Infrastructure e PasswordHasher/validadores oficiais; Domain/Application sem Identity ou MongoDB. Coordenação transacional concreta por caso de uso, leitura linearizável das credenciais correntes, controle administrativo efetivo e revogação atômica. Nenhum endpoint de criação de conta humana no01.

Inicialização explícita --initialize-persistence passa a aplicar também evolução01; nunca DDL no startup normal. Bootstrap --bootstrap-admin recebe Bootstrap__Nome/Email/Senha do ambiente externo; não inventar nem gravar senha na configuração. Reexecução não redefine conta. Não provisionar conta real de desenvolvimento com senha escolhida pelo executor; demonstrar o fluxo com dados sintéticos em bancos exclusivos de teste e registrar essa distinção.

Transportes de login com segredo são tipos exclusivos internos dos hosts, com campos explícitos; Contracts compartilhado contém somente respostas públicas e comandos administrativos sem senha. Formulários de acesso/troca usam SSR e POST HTTP antiforgery; componentes interativos nunca alteram headers. O estado do circuito conserva somente referência técnica ao ticket e claims de apresentação; credencial recuperada a cada chamada e validada pela API. Cookie e revalidação negam acesso se a API estiver indisponível.

Complemento operacional01: Api__BaseUrl de negócio (7143 local/8443 Compose) é separado de Api__HealthBaseUrl (8081 privado). As URLs são validadas como HTTPS, sem credenciais/query. Readiness passa a exigir os catálogos00+01. Audit01 preserva traceId do host HTTP ou um identificador técnico do bootstrap. Cookie válido já conectado exige logout antes de nova identidade; cookie revogado é removido ao revalidar para que o próximo login não reutilize ticket antigo. O armazenamento segue o ciclo do [CookieAuthenticationHandler oficial .NET10](https://github.com/dotnet/aspnetcore/blob/v10.0.12/src/Security/Authentication/Cookies/src/CookieAuthenticationHandler.cs); nenhuma alteração de headers dentro de circuito.

## Histórico anterior à revisão MongoDB — não usar como stack vigente

## Natureza deste registro

Decisões abaixo são baseline de projeto propostas para implementação, salvo stack/restrições explicitamente obrigatórias pelo usuário. Não se atribuem ao TCC antigo. Ao alterar, registrar motivo, data, substituição e impactos nos documentos; preservar histórico da decisão anterior.

| ADR | Decisão e justificativa | Consequência / alternativa avaliada |
| --- | --- | --- |
| ADR-01 | Monólito modular Clean Architecture com API e Web separados por HTTP, monorepo | Atomicidade local e deploy simples; microsserviços/CQRS/MediatR/mensageria/Event Sourcing sem justificativa e fora de escopo |
| ADR-02 | Blazor Interactive Server como BFF, cookie de referência e sessão API opaca; Identity para contas | Sem JWT próprio/OAuth externo; token protegido em servidor, API consulta sessão; logout offline deixa token inacessível até expirar, conforme segurança |
| ADR-03 | PostgreSQL READ COMMITTED com locks explícitos ordenados para vendas/estoque | Impede overselling e preço/status obsoleto; versão otimista sozinha não protege todo o conjunto; transação única com auditoria |
| ADR-04 | Idempotency-Key por ator/escopo/chave, resultado e hash na mesma transação | Resposta perdida é recuperável; UNIQUE por item/tipo e estado reforçam garantia; nenhuma purga automática no MVP |
| ADR-05 | Portas específicas e EF em Infrastructure; contratos de transporte sem domínio | Evita Generic Repository e dependência do frontend no backend; mapeamento manual inicial |
| ADR-06 | .NET 10 LTS, EF Core 10 e Npgsql compatível; PostgreSQL 18 baseline proposta | Stack .NET obrigatória; fixar patches verificados no 00, não inventar compatibilidade de pacote |
| ADR-07 | UUID, decimal/numeric(18,2), estoque inteiro UN, instante UTC e fuso configurável | Limites protegem overflow; fracionamento demanda mudança transversal, não ajuste isolado |
| ADR-08 | Venda Rascunho/Confirmada/Cancelada/Descartada; preço servidor e snapshots | Sem reserva; preço alterado exige PUT explícito antes de confirmar; histórico não depende do cadastro atual |
| ADR-09 | Quatro perfis fixos, um por usuário; vendedor só próprias vendas | Sem editor dinâmico; API nega por padrão; último Admin protegido por lock singleton |
| ADR-10 | Cadastros inativáveis, movimentos imutáveis e estorno compensatório | Sem DELETE comercial; itens de rascunho e imagens substituíveis são exceções delimitadas |
| ADR-11 | Relatório é posição atual das vendas confirmadas no intervalo | Cancelamento posterior retira contribuição no período original; não é fechamento contábil nem fluxo de caixa |
| ADR-12 | Imagens JPEG/PNG privadas em volume; relatórios em CSV | Menor dependência e entrega rápida; sem PDF/SVG; validar conteúdo/limites e prevenir fórmula |
| ADR-13 | Uma réplica, Docker Compose, deploy em manutenção e migrations explícitas | Sem HA ou migration no startup; backup diário externo e restauração validada antes de produção |
| ADR-14 | Incrementos declarados para conta humana e histórico de cliente | 01 usa bootstrap; 02 conclui vínculo; 03 histórico vazio tipado; 06 o substitui; evita dependência circular |

## Esclarecimentos da revisão dos módulos — 06/11/2026

As ADR-01–14 permanecem preservadas. Os itens seguintes são **decisões propostas**, acrescentadas para fechar ambiguidades de execução; não representam aprovação de negócio nem autorização para produzir código nesta etapa.

| ADR | Ambiguidade e decisão proposta | Impacto e validação |
| --- | --- | --- |
| ADR-15 | Produto inativo é rejeitado já em POST/PUT do rascunho e novamente ao confirmar. Todos os itens devem estar ativos. Rascunho não reserva nem exige saldo suficiente; confirmação exige linha de saldo e quantidade disponível. Linha ausente dá409 SALDO_INEXISTENTE sem autocorreção. Saldo final int é limitado a2.147.483.647; overflow de entrada/estorno dá409 SALDO_EXCEDE_LIMITE e rollback total. | Esclarece RN-01/02/08/11 e RF-08–16; arquitetura04/05/06 e módulos04/05/06. Preço com terceira casa continua rejeitado; multiplicação por inteiro é exata; média arredonda somente no final por AwayFromZero, sem alterar regra aprovada. |
| ADR-16 | Banco é incremental:00 só schemas/trilha;01 Identity/sessão sem FK de funcionário;02 acrescenta FK/UNIQUE de vínculo;04 produto+saldo 0;05 movimentos apenas manuais sem FK futura;06 acrescenta referências/constraints de venda. Comandos de conta/vínculo/inativação adquirem controle_administracao→usuários ordenados→funcionários ordenados→sessões e revalidam após locks. | Remove dependências circulares de migrations e criação de conta versus inativação; preserva ADR-03/14. Testes locais de cada bloco usam somente tabelas existentes, exceto fixtures técnicas explicitamente isoladas. |
| ADR-17 | Fixar nomes mínimos de DTOs/campos/rotas de UI para executores; usuário tem nome de exibição (bootstrap externo não secreto; conta humana copia nome do funcionário ao criar), sem sincronização automática posterior. Listas de usuário incluem email/ativo/versão somente para Admin. Health usa JSON mínimo próprio; demais erros usam ProblemDetails. Datas opcionais são ambas presentes ou ambas ausentes; exportação não usa paginação. Estoque baixo ordena saldo/nome/id; relatório de movimentos soma positivos, absolutos negativos e delta líquido. | Detalhamento de transporte/apresentação, sem substituir BFF, camadas ou políticas existentes. Códigos complementares: EMAIL_DUPLICADO, VINCULO_OCUPADO, REFERENCIA_EM_USO e os conflitos da ADR-15. Módulos00–10 descrevem request/response/erro/estados; arquitetura06 consolida. |
| ADR-18 | Recorte P2 proposto para11: produto ativo+publicado explicitamente; catálogo público sem saldo; solicitação anônima nome/telefone/itens, preço estimado em snapshot, Recebido→Encerrado com motivo. Sem identidade de consumidor, reserva, baixa, pagamento, entrega ou conversão automática em venda. Admin/Gerente publicam/triam. Dedupe público por chave aleatória e hash separado de sessão interna; recibo sem PII, sem GET público de pedido. Limites iniciais10 envios/10 min e 60 GET/min por IP confiável; corpo64 KiB; no-store para catálogo/imagem. | RF-21–24/RN-21–24 são propostos P2, não gates MVP. Implementar11 só após aceite/ajuste registrado de Q-04 e Q-03. Modelo/rotas/testes completos em módulo11; nenhum serviço financeiro/fiscal ou módulo12 é necessário. |

Q-04 continua aberta: a proposta mínima acima oferece uma resposta concreta para aceite/ajuste, não simula decisão do responsável. Rotas e DTOs P2 não devem ser criados antecipadamente.

Detalhes complementares das propostas: ADR-15 aplica locks de cliente/produtos também ao gravar rascunho, sem reserva/lock de saldo. ADR-17 permite ator ausente somente em auditoria explicitamente anônima (falha de login e futura solicitação pública), sem usuário fictício; mutações administrativas/comerciais continuam exigindo ator. Respostas Web de credencial inválida usam401 com formulário e mensagem genérica. Esses detalhes estão propagados ao banco e módulos01/06/11.

## Hipóteses a validar (baseline preservada)

| ID | Hipótese | Impacto se mudar |
| --- | --- | --- |
| H-01 | Uma empresa/um estoque por produto na instalação | Multiempresa/depósito exige remodelagem e autorização |
| H-02 | UN inteira | Fracionado muda tipos, limites, UI, movimentos e testes |
| H-03 | Cliente ativo obrigatório | Venda anônima exige regra de histórico e privacidade |
| H-04 | BRL, pt-BR e America/Cuiaba inicial | Outra moeda exige modelagem monetária; fuso já é configuração |
| H-05 | Sem descontos/fiscal/pagamento no MVP | Total e confirmação terão novas regras/contratos |
| H-06 | Uma réplica, desempenho e backup conforme RNF | Recurso operacional precisa ser medido e financiado |
| H-07 | Perfis fixos e vendedor restrito às próprias vendas | Mudança exige matriz, consultas e testes negativos |

## Questões abertas

| ID | Questão / responsável esperado | Prazo e condição |
| --- | --- | --- |
| Q-01 | Responsável de negócio/orientador confirma UN, cliente obrigatório e dados mínimos/preço? | Antes de homologar 03/04/06; enquanto isso seguir baseline e registrar hipótese |
| Q-02 | Responsável do projeto define host/domínio/certificado/custo e operador de backup? | Bloqueia 10; não bloqueia desenvolvimento local |
| Q-03 | Responsável pelos dados define finalidade, retenção e atendimento de solicitações sobre dados pessoais? | Bloqueia produção com dados reais; demonstração sintética permitida |
| Q-04 | Responsável de negócio define catálogo, pedido, identidade, reserva, entrega e pagamento? | Bloqueia 11; sem contratos públicos aprovados |

## Registro de mudança futura

Formato: `data | ADR/Q | estado anterior | decisão e justificativa | responsável | RF/RN/CA e documentos impactados | evidência`. Questão aberta só muda para resolvida com resposta registrada; hipótese não vira fato por repetição em outro documento.

## ADR-19 — Execução autorizada do bloco 00 (06/11/2026)

A solicitação explícita do usuário libera código exclusivamente para 00 e prevalece sobre a restrição documental da etapa anterior em AGENTS/escopo/plano. Nenhum requisito de negócio é alterado. SDK observado 10.0.401; EF/ASP.NET 10.0.12 e Npgsql EF 10.0.3 serão fixados e validados por restore. Contexts vazios criam somente schemas e histórico, sem Identity. Readiness usa listener interno separado (porta 8081) sem publicação no Compose; liveness e páginas técnicas são exceções explícitas à negação por padrão. Sem handler de autenticação fictício: uma policy que sempre nega protege rotas não liberadas até 01. HTTPS local usa certificado externo confiado; nenhum bypass TLS. Compose local inclui PostgreSQL, API e Web, não constitui publicação do bloco 10. Docker ausente impede comprovar integração e persistência, sem substituição por InMemory. Testes PostgreSQL falham explicitamente se a configuração dedicada não existir.

Referências verificadas: https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL/10.0.3 e https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc.Testing/10.0.12. Versões transitivas serão registradas nos lockfiles após restore.

Complemento ADR-19: EF Relational fixado explicitamente em 10.0.12 após conflito real de build. Runtime da fundação recebe somente SELECT no histórico do próprio schema; módulos futuros concederão DML necessário. Baseline Down preserva schema; sem downgrade destrutivo. Imagem ASP.NET fixada em 10.0.12-noble, confirmada no registro oficial https://mcr.microsoft.com/v2/dotnet/aspnet/tags/list ; PostgreSQL 18.3 conforme https://www.postgresql.org/docs/release/18.3/ . Data Protection local usa .local/keys e Compose usa volume privado. Assets resolvidos pelo manifest e UseStaticWebAssets para execução local do host; cliente consulta readiness interno para não confundir processo vivo com serviço pronto.
