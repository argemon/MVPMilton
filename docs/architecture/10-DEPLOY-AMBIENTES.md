# 10 — Deploy e ambientes

> Referência arquitetural histórica de publicação; o escopo acadêmico vigente não inclui deploy. O Bloco10 atual é homologação, não autorizado nesta solicitação. Fonte vigente: plano00 e ADR-37.

> Atualização vigente00 — ADR-27–29: implementação autorizada com Atlas já configurado em .env, sem sobrescrever segredos. Development reutiliza MONGODB_URI; Production/Staging exigem configuração externa específica. Integração pode executar transações reais em bancos efêmeros exclusivos no mesmo cluster ou perfil Testcontainers explícito; não criar outro MongoDB quando Atlas atende. rs0 é nome apenas do perfil local, não do Atlas. As exigências universais de Docker/rs0, somente Markdown e adaptação ainda não autorizada nas seções históricas anteriores ficam superadas nesses pontos.


## Topologia planejada

```mermaid
flowchart LR
    B[Navegador] -->|HTTPS / WebSocket| P[Proxy TLS]
    P --> W[Web Blazor / BFF]
    W -->|TLS interno| A[API privada]
    W -->|Somente tickets| DB
    A --> DB[(MongoDB Atlas / banco único gestao_comercial_dev)]
    A --> IMG[Volume privado imagens]
```

O Compose de desenvolvimento atual executa Web/API e reutiliza MongoDB Atlas externo, sem serviço de banco. A topologia local alternativa pode incluir MongoDB e o proxy de publicação pertence ao10. Apenas proxy publica porta em produção; MongoDB e API não são públicos. Uma réplica Web/API no MVP: evita coordenação de circuitos; não prometer escalabilidade horizontal. Persistir dados, imagens e key ring em volumes separados. Chamadas servidor → API usam certificado confiável; não desabilitar validação TLS. Desenvolvimento pode usar certificados locais confiados por rotina explícita.

## Ambientes

| Ambiente | Configuração | Dados |
| --- | --- | --- |
| Desenvolvimento | SDK fixado; Compose local; User Secrets/variáveis locais | Sintéticos descartáveis |
| Teste | Banco isolado, credenciais efêmeras, relógio controlável | Fixtures reproduzíveis |
| Homologação | Igual à topologia de produção, TLS e volumes reais | Sintéticos, sem dados pessoais reais |
| Produção | Secrets externos, mínimo privilégio, logs e backup | Reais somente após Q-03 resolvida |

Fuso comercial proposto `America/Cuiaba`; locale pt-BR. Configurar `APP_TIME_ZONE` equivalente no host Web/API e testar a conversão; não alterar relógio UTC do banco para resolver relatórios. Nome/URL/host, provedor, domínio e certificados são Q-02 antes do bloco 10.

## Segredos e configuração

Nomes previstos (não implementados nesta revisão): Mongo__ConnectionString, Mongo__Database (API e Web gestao_comercial_dev, ADR-37), Mongo__ReplicaSet=rs0, Mongo__ExpectedPersistenceVersion; Evolution__ConnectionString somente na tarefa operacional; Api__BaseUrl HTTPS e configurações TLS/key ring/fuso. Credenciais por secrets ou variáveis externas, nunca valores válidos no exemplo. Em URI, escapar usuário/senha com regras URI, sem concatenar segredo cru; authSource explícito, replicaSet=rs0, readPreference=primary, writeConcern majority e TLS em produção. Não logar URI, cabeçalho ou comandos com credenciais.

Contas separadas: administrador do cluster para instalação/rotação; operador de evolução com createCollection/createIndex/collMod apenas nos bancos necessários; API runtime com ações por coleção, sem update/remove em stock_movements/audit_events; Web runtime somente tickets e leitura do catálogo técnico próprio. Privilégios de backup/restore em contas operacionais próprias. Nenhuma role root/readWrite ampla para runtime por conveniência. keyfile de autenticação interna e certificados montados como secrets com owner/permissões exigidas pelo mongod; arquivos não entram em Git/imagem. Docker secrets não eliminam necessidade de proteger host e backups.

### Replica set local de um nó — alternativa ao Atlas, não usada na execução atual

Descrever/futuramente preparar serviço mongo com imagem oficial e patch/digest fixados, mongod --replSet rs0 --keyFile externo, volume persistente /data/db, rede privada com nome mongo:27017 e TLS confiável. Uma tarefa de bootstrap configura rs0 somente se não inicializado, aguarda eleição do primary e provisiona usuários/roles. Instalação inicial autenticada segue procedimento oficial keyfile/localhost exception ou provisionamento controlado do administrador; não iniciar rede pública sem autenticação. Reexecução lê configuração existente e falha se divergente, sem rs.initiate repetido ou reconfiguração destrutiva. init scripts de imagem executados apenas em volume novo não substituem uma tarefa idempotente verificável.

Health do container verifica hello autenticado, setName=rs0 e isWritablePrimary; readiness API/Web verifica ping autenticado, primary, acesso ao banco próprio e versão das coleções/índices esperada. Standalone responde a ping mas deve ficar unavailable. Blazor consulta readiness interno da API, liveness permanece independente do banco. API/DB/readiness não são publicados em produção. Em desenvolvimento, eventual porta DB só em loopback; hostname anunciado precisa ser resolvível pelo cliente. Ferramentas do host podem ser executadas na rede Compose ou usar configuração de teste direta para um nó, sem reutilizá-la em produção.

Bootstrap de replica set/usuários e evolução de coleções são tarefas técnicas explícitas e distintas do bootstrap do Admin de aplicação no01. Nenhuma delas cria entidades de módulos futuros. Volume preserva documentos/índices/configuração do replica set; nunca down -v como rotina de atualização. Nó único habilita transações mas não oferece redundância/HA: falha do host interrompe o sistema. Q-02 decide se essa limitação atende o destino ou se exigirá topologia com redundância; não presumir que majority em um nó protege contra perda do disco.

## Deploy e rollback

1. Selecionar commit/tag e imagens imutáveis com patch fixo. Validar pipeline e evoluções de persistência em homologação.
2. Colocar sistema em manutenção e encerrar tráfego/circuitos que escrevem; confirmar ausência de operações em andamento.
3. Fazer backup consistente de MongoDB, imagens e key ring; registrar versão de schema e versão da aplicação.
4. Executar evoluções de persistência revisadas com operador de evolução em tarefa única. Inicializar primeiro administrador apenas no primeiro deploy com segredo externo e remover segredo de bootstrap após uso.
5. Iniciar serviços, validar readiness e executar smoke autenticado: entrada, confirmação e cancelamento com dados de teste segregados de registros reais.
6. Abrir tráfego e observar erros/escritas de guarda/expiração de sessão. Não expor detalhes de health.

Rollback de binário só é permitido se schema permanecer compatível. Se evolução de persistência é destrutiva/incompatível, manter manutenção e restaurar conjunto de backup em ambiente isolado para validar antes de substituir; registrar perda de alterações posteriores. Nunca executar downgrade destrutivo automático.

## Backup, restauração e observabilidade

Meta H-06 preservada: backup diário criptografado externo, retenção 30 dias, RPO<=24 h e RTO<=4 h medidos. Conjunto recuperável: bancos comercial e Web, imagens privadas, key ring, versões de aplicação/servidor/driver/Tools e configuração de segurança. Não copiar /data/db ativo como se fosse backup consistente. Registrar janela, tamanho, integridade/checksum e responsáveis, sem segredos no relatório.

Procedimento inicial simples: manutenção, drenar circuitos/escritas e tarefas de background que persistem sessões, impedir evolução/índices concorrentes; executar mongodump com ferramentas compatíveis e conta de backup, capturando ambos os bancos no mesmo ponto operacional estável; copiar imagens/key ring na mesma janela, cifrar e enviar fora do host. Alternativa online só após ensaio: dump completo de membro do replica set com --oplog e restore com --oplogReplay, respeitando limitações das ferramentas (sem limitar --db/--collection nesse modo) e sincronização com arquivos externos. Não chamar dump parcial com oplog de snapshot consistente.

Restaurar primeiro em replica set isolado compatível, sem sobrescrever volume real; mongorestore com credenciais de restore fornecidas de forma segura, validar índices/validadores/roles e schemaVersion, então arquivos/chaves e aplicação da versão registrada. Conta/roles do cluster podem precisar reprovisionamento seguro conforme conteúdo do dump; não assumir que somente dados restauram RBAC/keyfile. Validar login/sessões, imagens, razão/saldo, snapshots monetários e jornada. Medir RTO/RPO; eliminar cópias efêmeras somente conforme procedimento autorizado. Binário incompatível não sobe; rollback exige versão compatível ou restore ensaiado, nunca exclusão improvisada de documentos.

Logs com traceId/status/duração, falhas de sessão/conflito e commit incerto, sem BSON/payload/URI sensível. Métricas: 5xx/p95, pool/conexões, primary/estado do replica set, transações abortadas/retries, espaço/duração do oplog e último backup válido. Alertas por falha de readiness/backup ao responsável Q-02. Sem afirmar que arquivo gerado equivale a restauração demonstrada.

Fontes: [autenticação interna keyfile](https://www.mongodb.com/docs/manual/core/security-internal-authentication/), [mongorestore e limitações de oplogReplay](https://www.mongodb.com/docs/database-tools/mongorestore/), [exemplos oficiais de restore](https://www.mongodb.com/docs/database-tools/mongorestore/mongorestore-examples/). Patches/imagens/Tools serão fixados e testados no futuro00; esta revisão só altera Markdown.

## Gate de publicação

CA-16 a CA-18, RNF-01/04/05/06 e [módulo 10](../modules/10-HOMOLOGACAO.md). Sem backup restaurável, TLS, Q-02/Q-03 resolvidas e credenciais de bootstrap removidas, publicação com dados reais permanece bloqueada. Ambiente de demonstração sintético pode ser publicado após Q-02 e gates técnicos.

## Ordem verificável de inicialização e conexão (ADR-26)

Em volume novo: (1) iniciar mongod com rs0, keyfile de owner/permissão corretos e TLS, sem publicar a porta para fora da rede de instalação; (2) executor controlado no próprio container mongod conecta por loopback enquanto não existem usuários/roles, inicializa membro anunciado mongo:27017 e aguarda primary; (3) pela mesma origem loopback cria primeiro administrador externo, encerrando localhost exception; (4) autentica como administrador e provisiona roles/contas distintas API/Web/evolução; (5) operador de evolução inicializa os dois catálogos técnicos; (6) somente então readiness e hosts. Localhost exception não funciona a partir de container auxiliar por acesso remoto. Manter essa janela isolada; se administrador já existe, usar autenticação existente, sem depender da exceção.

Em volume existente: autenticar e conferir rs.conf/hello, usuários e versões; divergência de rs0/membro/roles interrompe com diagnóstico seguro. Não reiniciar configuração, abrir bypass ou trocar senha como efeito de reinício. Bootstrap não depende do readiness da API nem de coleções futuras, evitando ciclo de inicialização.

Formato alvo de URI na rede Compose, sem credencial válida: `mongodb://USUARIO:SENHA_ESCAPADA@mongo:27017/gestao_comercial?authSource=gestao_comercial&replicaSet=rs0&readPreference=primary&w=majority&tls=true`. Conta API criada em gestao_comercial; Web acessa somente tickets no mesmo banco conforme ADR-37; o projeto acadêmico reutiliza a conexão Atlas existente. Instalação pode ter authSource=admin; não reutilizar essa conta nos hosts. api/web resolvem mongo; certificado do banco contém SAN mongo e CA confiada. tlsCAFile/certificados são externos. Não usar hostname db/porta5432 do Compose anterior como MongoDB. Sem directConnection=true nos hosts normais; fixture de um nó com porta dinâmica tem exceção explícita na arquitetura09.

Aceite do futuro00: inicialização vazia/reinício sem reconfigurar, hello autenticado rs0/primary, conexão API/Web com credenciais próprias, commit/abort técnico em fixture e ready200; perda de primary/conexão dá503 sem segredo e live200; versão técnica incompatível ou standalone dá503. Runtime não escreve no catálogo nem lê o outro banco. O deploy/compose.yaml atual implementa o caminho Atlas externo autorizado, sem criar replica set local. O procedimento de bootstrap local acima é somente alternativa futura explícita; não deve substituir a conexão existente. Docker não estava disponível para executar esse artefato nesta implementação.

Fonte: [localhost exception em replica sets](https://www.mongodb.com/docs/v8.0/core/localhost-exception/).
