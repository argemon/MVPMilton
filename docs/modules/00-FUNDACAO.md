# M00 — Fundação

> Atualização vigente00 — ADR-27–29: implementação autorizada com Atlas já configurado em .env, sem sobrescrever segredos. Development reutiliza MONGODB_URI; Production/Staging exigem configuração externa específica. Integração pode executar transações reais em bancos efêmeros exclusivos no mesmo cluster ou perfil Testcontainers explícito; não criar outro MongoDB quando Atlas atende. rs0 é nome apenas do perfil local, não do Atlas. As exigências universais de Docker/rs0, somente Markdown e adaptação ainda não autorizada nas seções históricas anteriores ficam superadas nesses pontos.


## 1. Identificação, objetivo e valor

**Bloco 00, prioridade P0, implementação MongoDB Atlas entregue; validações atuais no estado03.** Criar a base para compilar, testar e executar API e Blazor separadamente, sem introduzir funcionalidades comerciais. Entrega verificável: instalação local reproduzível com health e falhas seguras.

## 2. Pré-requisitos e dependências concluídas

Normas transversais: arquitetura05/09 e ADR-20–29, MongoDB.Driver oficial restrito à infraestrutura, transações sequenciais em topologia compatível e testes MongoDB reais isolados (Atlas ou perfil Testcontainers explícito) conforme ADR-28. Classes/serviços de módulo futuro não são pré-requisitos ocultos.

Nenhum bloco de software anterior. Ler AGENTS, arquitetura 01–10, requisitos e ADR-01/02/20–25 e histórico aplicável. Ter SDK .NET10, conexão Atlas existente e certificados locais confiados. Docker/Compose somente no perfil opcional local/CI; a integração real em bancos isolados do Atlas atende o caminho autorizado, sem memória/standalone.

## 3. Rastreabilidade e prioridades

Sem RF comercial entregue. RNF-04/07/08/09/10 (P0/P1 conforme catálogo); RN-15 apenas como negação por padrão, sem login. CA-18 aplica-se inicialmente ao build/layout; CA-16 não é gate deste bloco.

## 4. Dentro e fora do escopo

Dentro: inicializar Git, solução e projetos da arquitetura 03, DI, configuração, adaptadores de persistência backend/web, trilha de evoluções de persistência, Compose local, logs/ProblemDetails, health, layout, pipeline e testes básicos. Fora: Identity, usuários, funcionário, produto, saldo, movimentações, vendas, catálogo e qualquer autenticação simulada.

## 5. Atores e permissões

Executor local administra ambiente com segredos externos. Navegador acessa somente home técnica, erro e liveness; readiness fica na rede interna. Rotas comerciais ainda não existem; nenhum dado de negócio é publicado. Criar fallback de autorização sem exigir que o login do 01 exista para visitar a home técnica.

## 6. Entidades e banco

Nenhuma entidade comercial. Preparar acesso separado aos bancos lógicos `gestao_comercial` e `gestao_web`, IMongoClient singleton por credencial e inicialização explícita/idempotente das coleções técnicas `persistence_versions` e de seus índices/validadores. Não criar coleções de negócio, usuários de aplicação ou tickets. Em produção exigir usuários separados API/Web/operador; desenvolvimento pode reutilizar a única credencial fornecida conforme ADR-27. Atlas gerencia persistência e nome da topologia; volume /data/db e rs0 aplicam-se só ao perfil local. Readiness exige ping autenticado, primary, sessões/topologia transacional e baseline esperada; ping isolado não basta. Exercitar commit/abort em coleção técnica efêmera exclusivamente no banco de testes, sem coleção fictícia na aplicação.

## 7. Fluxos principais e alternativos

1. Checkout limpo → fornecer configuração externa → restore/build → conectar Atlas existente e conferir primary/topologia → aplicar inicialização explícita e idempotente → iniciar API/Web → verificar health e layout.
2. Parar DB → readiness 503; liveness permanece 200 enquanto processo responde; UI informa indisponibilidade quando consultar API.
3. Configuração obrigatória ausente → falhar na inicialização com nome da configuração e sem valor secreto. Não cair para senha padrão.
4. API indisponível → página técnica mostra erro recuperável; não inventa status saudável.

## 8. Regras e validações

Respeitar dependências de camada (RNF-09), segredos externos (RNF-04), traceId sem payload sensível (RNF-10). Validar URL API absoluta, conexão e ambiente conhecidos. Nenhum formulário comercial/monetário neste bloco; RN-01/02 não têm implementação aqui.

## 9. Contratos REST

| Método e caminho | Request | Response de sucesso | Erros e acesso |
| --- | --- | --- | --- |
| GET `/health/live` | Sem corpo | 200 `{status:"ok"}` | Público, sem versão/host/conexão detalhados; timeout se processo não responde |
| GET `/health/ready` | Sem corpo | 200 `{status:"ready"}` | Interno; DB inacessível → 503 `{status:"unavailable"}` sem diagnóstico sensível |

Health usa payload mínimo acima; demais erros HTTP usarão ProblemDetails com `status`, `code`, `traceId`, `errors` quando aplicável. Não criar endpoint que provoca falha em produção apenas para testar middleware; falha é injetada no host de testes.

## 10. Páginas, componentes e estados da UI

| Rota | Componentes propostos | Vazio / carregando / sucesso / erro |
| --- | --- | --- |
| `/` | LayoutPrincipal, InicioTecnico, EstadoApi | Sem indicadores comerciais; consulta em andamento; API disponível; indisponível com Repetir |
| `/erro` | PaginaErro, ResumoErro | Mensagem segura e traceId, sem stack trace; acessível sem sessão |
| `/indisponivel` | PaginaIndisponivel | Orientação e botão Repetir; sucesso retorna à origem local |

Layout pt-BR com teclado e 360/1280 px. Não exibir links para páginas de módulos futuros.

## 11. Erros e concorrência

Sem transação comercial. Inicialização de coleções/índices ocorre por tarefa operacional única antes dos hosts; reexecução confere definições e registra versão/checksum, sem apagar dados. Aplicação não muda schema no startup. IMongoClient é singleton, sessões de driver são curtas por operação, nunca singleton nem compartilhadas entre requisições/circuitos. Sem acesso ao banco comercial pelo Web. Standalone ou replica set sem primary produz readiness 503.

## 12. Testes unitários e de integração

Unidade: validação de configuração e transformação segura de erros quando houver lógica própria. Integração xUnit: WebApplicationFactory com MongoDB real via Testcontainers/replica set, inicialização do zero, readiness com/sem DB, contrato health, erro injetado sem stack trace. Inspecionar referências: frontend somente Contracts; Domain sem BSON/driver/HTTP; Application sem Infrastructure. Não criar testes de classes vazias.

## 13. Cenários Given/When/Then

| ID local | Given (Dado) | When (Quando) | Then (Então) |
| --- | --- | --- | --- |
| M00-CA01 | Checkout e configuração externa válidos | Subir serviços/primary e executar inicialização | Build passa, Web abre e readiness responde 200 |
| M00-CA02 | API em execução | Parar MongoDB | Readiness 503, liveness 200, sem credencial no retorno |
| M00-CA03 | DB com inicialização/índices aplicados | Reiniciar hosts sem apagar bancos Atlas (perfil local: sem remover volumes) | Documentos técnicos, índices e catálogo de versões persistem; evolução de persistência não reaplica efeito |
| M00-CA04 | Configuração de conexão ausente | Iniciar host | Inicialização falha explicitamente e log não revela segredos |

## 14. Checklist de execução, build, evoluções de persistência e testes

Executar na implementação, nunca nesta revisão documental:

- [ ] Registrar SDK/pacotes/imagens fixados e configuração local sem segredos versionados.
- [ ] Criar estrutura e conferir referências de projetos.
- [ ] Executar `dotnet restore` e `dotnet build --no-restore -c Release` na raiz da solução.
- [ ] Iniciar Compose com os serviços documentados; revisar/aplicar inicialização dos bancos técnicos backend e web por tarefa explícita.
- [ ] Executar `dotnet test -c Release --no-build` com DB isolado e registrar resultados.
- [ ] Conferir health, reinício de volumes e layout em 360/1280 px.
- [ ] Registrar arquivos, comandos exatos de Compose/inicializador MongoDB, evidências e pendências no estado.

## 15. Definition of Done e desbloqueio

DoD global satisfeita; M00-CA01 a M00-CA04 demonstrados; zero segredo e zero dependência de projeto indevida; build/testes realmente executados. Desbloquear **01**. Nenhum teste deste gate depende de autenticação, produto ou infraestrutura de publicação futura.
