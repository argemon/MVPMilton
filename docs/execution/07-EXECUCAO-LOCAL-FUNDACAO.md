# Execução local — bloco00 com MongoDB Atlas

## Pré-requisitos

Implementação autorizada pelas ADR-27–29. SDK10.0.401, MongoDB.Driver3.11.2 e .env existente ignorado pelo Git. Conexão fornecida é Atlas/SRV: reutilizada, sem nova instância MongoDB. Certificado local HTTPS válido/confiado e acesso de rede Atlas permitido. Senha/URI não são registradas neste documento nem logadas. Estrutura física em backend/Projeto, frontend/Projeto, shared/Contracts e tests/UnitTests/IntegrationTests.

## Configurar e iniciar

1. Preservar .env. Para checkout sem configuração, preencher externamente a partir de .env.example ilustrativo. Loader somente Development; variáveis do processo e argumentos não secretos de configuração têm precedência. Hosts Testing/Staging/Production não leem .env. URI explícita Mongo__ConnectionString prevalece sobre alias MONGODB_URI de Development.
2. Executar dotnet restore --locked-mode e dotnet build --no-restore -c Release; conferir scripts/verify-architecture.ps1. Não há EF CLI, migrations ou SQL executável.
3. Executar dotnet run --project backend/GestaoComercial.Api -c Release --no-build -- --probe-mongo. Resultado é somente conectividade/topologia, sem URI; hello autenticado verifica primary/sessões/replica set ou mongos, sem impor rs0 a Atlas.
4. Inicialização explícita: usar --initialize-persistence com cada host, conforme README. Cria somente persistence_versions no banco técnico de desenvolvimento de cada host. Validador estrito, índice único version e catálogo com checksum/appliedAt. Reexecução preserva o registro; divergência não é corrigida silenciosamente. Produção exige Evolution__ConnectionString específico para tarefa e não oferece essa credencial ao runtime normal.
5. Executar API e Web em terminais separados. API https://localhost:7143, ready interno8081; Web https://localhost:7243, ready interno8082. Inicialização e chamadas Web→API usam HTTPS com certificado confiado. Nenhum endpoint comercial/login existe.

## Testes e gates

dotnet test -c Release --no-build executa UnitTests/IntegrationTests. A fixture usa TEST_MONGODB_URI externo ou fallback explícito da URI .env para banco descartável gc_m00_[24hex]_test de36 caracteres. Antes de escrever exige banco vazio; só o nome gerado e reservado pode ser removido. Faz commit e abort em duas coleções técnicas, init/reinit e falhas de validador/checksum. Não há testes destrutivos em bancos de desenvolvimento/produção.

TEST_MONGO_CONTAINER=true é perfil opcional com Docker/Testcontainers/rs0. Não iniciar outro banco só porque Docker existe se Atlas já atende desenvolvimento; o perfil existe para CI e ensaio isolado explícito. Ausência de Docker não bloqueia comprovação real no Atlas após ADR-28, mas resultados de Compose/Testcontainers não são alegados como executados. Não interromper o cluster externo para simular falha: teste de conexão indisponível usa cliente da API apontando para endereço de loopback sem serviço e confirma503/live200. UI também foi ensaiada com API local parada e Repetir.

Reiniciar hosts e repetir init/health sem apagar banco comprova persistência do catálogo remoto; armazenamento/eleição do provedor é responsabilidade do Atlas, não volume local a criar. Em caminho local alternativo, seguir bootstrap/volumes rs0 da arquitetura10; não é usado pela conexão atual.

## Configurações dos hosts sem Compose

Development: MONGODB_URI já fornecido; default único gestao_comercial_dev (API e Web), conforme ADR-37. Opcional Mongo__Database por processo. Api__BaseUrl=https://localhost:8081/ no perfil Web; Health__InternalPort pode ser configurado por host. DataProtection__KeysPath privado/persistente. Api__CaCertificatePath permite CA privada do canal API sem substituir as raízes públicas usadas no Atlas.

Production/Staging: Mongo__ConnectionString e Mongo__Database obrigatórios de cada host, sem ler .env/fallback; operador usa Evolution__ConnectionString na tarefa explícita. Bancos/usuários distintos e privilégio mínimo são requisitos de publicação, não comprovados pela conta compartilhada fornecida para Development. Não copiar credenciais para README, argumento CLI, imagem ou log. CERT_DIR/TLS_PASSWORD para Compose são fornecidos externamente; comandos completos e limitações estão no README.

## Resultado e retomada

Consultar o registro atual no estado03 para números e evidências realmente executados. A auditoria06 anterior permanece histórica: código/Compose antigos foram substituídos nesta implementação00. Nenhum bloco01 foi iniciado. Compose/pipeline/produção exigem suas próprias validações nos ambientes correspondentes.
