# M00 — Fundação

## 1. Identificação, objetivo e valor

**Bloco 00, prioridade P0, software pendente.** Criar a base para compilar, testar e executar API e Blazor separadamente, sem introduzir funcionalidades comerciais. Entrega verificável: instalação local reproduzível com health e falhas seguras.

## 2. Pré-requisitos e dependências concluídas

Nenhum bloco de software anterior. Ler AGENTS, arquitetura 01–10, requisitos e ADR-01 a ADR-14. Ter SDK .NET 10, Docker/Compose e certificados locais confiáveis disponíveis. Registrar versões realmente usadas; indisponibilidade de Docker bloqueia o gate de integração, não autoriza substituir PostgreSQL por InMemory.

## 3. Rastreabilidade e prioridades

Sem RF comercial entregue. RNF-04/07/08/09/10 (P0/P1 conforme catálogo); RN-15 apenas como negação por padrão, sem login. CA-18 aplica-se inicialmente ao build/layout; CA-16 não é gate deste bloco.

## 4. Dentro e fora do escopo

Dentro: inicializar Git, solução e projetos da arquitetura 03, DI, configuração, contexts backend/web, trilha de migrations, Compose local, logs/ProblemDetails, health, layout, pipeline e testes básicos. Fora: Identity, usuários, funcionário, produto, saldo, movimentações, vendas, catálogo e qualquer autenticação simulada.

## 5. Atores e permissões

Executor local administra ambiente com segredos externos. Navegador acessa somente home técnica, erro e liveness; readiness fica na rede interna. Rotas comerciais ainda não existem; nenhum dado de negócio é publicado. Criar fallback de autorização sem exigir que o login do 01 exista para visitar a home técnica.

## 6. Entidades e banco

Nenhuma entidade comercial. Criar contexts separados e baseline dos schemas `comercial` e `web` e histórico de migrations. Se EF exigir operação para uma migration inicial vazia, criação dos schemas é suficiente; não criar tabela fictícia. Não antecipar Identity/tickets, nem FKs para tabelas futuras. Usuários DB separados para runtime e migrations. Volume de dados persiste após reinício.

## 7. Fluxos principais e alternativos

1. Checkout limpo → fornecer configuração externa → restore/build → iniciar PostgreSQL → aplicar baseline explicitamente → iniciar API/Web → verificar health e layout.
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

Sem transação comercial. Duas execuções de migration não são permitidas pelo procedimento: usar tarefa única antes de iniciar hosts. DB indisponível não pode produzir falso readiness 200. Evitar singleton com serviço scoped e não compartilhar contexto entre requisições.

## 12. Testes unitários e de integração

Unidade: validação de configuração e transformação segura de erros quando houver lógica própria. Integração xUnit: WebApplicationFactory com PostgreSQL real, baseline do zero, readiness com/sem DB, contrato health, erro injetado sem stack trace. Inspecionar referências: frontend somente Contracts; Domain sem EF/HTTP; Application sem Infrastructure. Não criar testes de classes vazias.

## 13. Cenários Given/When/Then

| ID local | Given (Dado) | When (Quando) | Then (Então) |
| --- | --- | --- | --- |
| M00-CA01 | Checkout e configuração externa válidos | Subir serviços e aplicar baseline | Build passa, Web abre e readiness responde 200 |
| M00-CA02 | API em execução | Parar PostgreSQL | Readiness 503, liveness 200, sem credencial no retorno |
| M00-CA03 | DB com baseline aplicada | Reiniciar containers sem remover volumes | Schema e histórico persistem; migration não reaplica efeito |
| M00-CA04 | Configuração de conexão ausente | Iniciar host | Inicialização falha explicitamente e log não revela segredos |

## 14. Checklist de execução, build, migrations e testes

Executar na implementação, nunca nesta revisão documental:

- [ ] Registrar SDK/pacotes/imagens fixados e configuração local sem segredos versionados.
- [ ] Criar estrutura e conferir referências de projetos.
- [ ] Executar `dotnet restore` e `dotnet build --no-restore -c Release` na raiz da solução.
- [ ] Iniciar Compose com os serviços documentados; gerar/revisar/aplicar baseline backend e web por tarefa explícita.
- [ ] Executar `dotnet test -c Release --no-build` com DB isolado e registrar resultados.
- [ ] Conferir health, reinício de volumes e layout em 360/1280 px.
- [ ] Registrar arquivos, comandos exatos de Compose/EF, evidências e pendências no estado.

## 15. Definition of Done e desbloqueio

DoD global satisfeita; M00-CA01 a M00-CA04 demonstrados; zero segredo e zero dependência de projeto indevida; build/testes realmente executados. Desbloquear **01**. Nenhum teste deste gate depende de autenticação, produto ou infraestrutura de publicação futura.
