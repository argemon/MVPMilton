# M01 — Autenticação e autorização

## 1. Identificação, objetivo e valor

**Bloco 01, P0, implementado; evidências em estado03.** Entregar acesso seguro e manutenção inicial de contas, com sessão API/BFF isolada por usuário e autorização no backend. Valor: nenhum cadastro futuro precisa criar mecanismo próprio de acesso.

## 2. Pré-requisitos e dependências concluídas

Normas transversais: arquitetura05/09 e ADR-20–30, MongoDB.Driver oficial restrito à infraestrutura, transações sequenciais em topologia compatível e testes MongoDB reais isolados (Atlas ou perfil Testcontainers explícito) conforme ADR-28. Classes/serviços de módulo futuro não são pré-requisitos ocultos.

00 aprovado: API/Web/MongoDB replica set, adaptadores de persistência, configuração, TLS local, build e testes. Ler arquitetura 06/07, matriz e ADR-02/09/14. Este bloco não depende de Funcionário: somente bootstrap cria conta operacional; outras contas em fixtures de teste não são um recurso público.

## 3. Requisitos e prioridades

RF-01/02 (P0) completos; RF-03 (P0) parcial: consulta, perfil/status e redefinição da conta inicial; criação humana vinculada termina no 02. RNF-01/04/07/08/10; CA-01/02 e subconjunto de CA-17 de sessões/CSRF/último Admin. Não exigir CA-03 nem tela de funcionário.

## 4. Escopo e exclusões

Dentro: Identity, bootstrap único seguro, login/logout SSR, troca obrigatória, redefinição, sessões opacas, tickets protegidos, policies fixas e auditoria. Fora: cadastro público, JWT/OAuth próprio, refresh token, editor de permissões, criação normal de usuário e vínculo de funcionário.

## 5. Atores e permissões

Todos os perfis autenticados: me/logout/alterar senha. Administrador: Usuarios.Gerenciar. Vendedor/Estoquista não administram contas. Perfil único e negação por padrão; troca pendente permite somente me/logout/senha. Perfis diferentes podem ser construídos em fixtures isoladas para testar policies sem depender do 02.

## 6. Entidades e evoluções de persistência

Criar no banco comercial `users`, `api_sessions`, `audit_events` e singleton `administration_control`; no banco Web `tickets`. Conta guarda nome de exibição, nomeNormalizado para busca/ordenação, email normalizado único, hash Identity versionado, perfil fixo, ativo, versão, securityStamp, troca obrigatória, contador/lockout e timestamps. employeeId nulo somente no bootstrap; coleção employees e índice parcial de vínculo surgem no 02. Implementar stores mínimos de ASP.NET Core Identity via MongoDB.Driver, sem provedor implícito e sem hash próprio, conforme arquitetura 07. Token API de 32 bytes: somente hash no backend; ticket e token protegidos por Data Protection no Web, cookie de referência Secure/HttpOnly/Lax. Evolução explícita das coleções/validadores/índices, sem antecipar funcionários.

## 7. Fluxos principais e alternativos

1. Tarefa bootstrap verifica inexistência de Admin, cria conta com troca obrigatória e audita; execução repetida não recria nem redefine senha.
2. GET login → POST Web com antiforgery → API valida Identity → BFF persiste ticket/token protegido → cookie e redirecionamento local.
3. Senha temporária → só tela de troca → API revoga todas as sessões após mudança → Web limpa ticket/cookie → novo login.
4. Cada chamada API revalida sessão/usuário/perfil. Revalidação Web a cada 60 s não estende inatividade.
5. Logout revoga sessão API e remove ticket/cookie. API fora: remover acesso local mesmo assim e registrar falha segura; risco residual já documentado na arquitetura 07.
6. Alterar perfil/status/redefinir senha revoga sessões na transação; tentativa de remover último Admin recebe 409.

## 8. Regras e validações

RN-15/16/18, ADR-02/09/14. Email obrigatório válido; senha 12–128, maiúscula/minúscula/dígito/símbolo. Cinco falhas bloqueiam por 15 min; resposta genérica. Sessão absoluta 8 h/inatividade 30 min; me/health não renovam. returnUrl somente local. A API não aceita identidade do ator no corpo. Alteração efetiva de perfil/status revoga sessões; PUT sem mudança não precisa revogar.

## 9. Endpoints REST e HTTP do BFF

Base API `/api/v1`; todas as falhas não-health usam ProblemDetails. `UsuarioResumo={id,nome,email,perfil,ativo,versao,deveAlterarSenha,funcionarioId:null}` nas consultas administrativas, conforme arquitetura06; nunca retorna hash/senha. Resumo de sessão/login contém somente os campos de autenticação autorizados de seu contrato, sem reutilizar o documento users.

| Método / caminho | Request | Response | Erros principais |
| --- | --- | --- | --- |
| POST `/auth/login` | `{email,senha}` | 200 `{accessToken,expiraEm,usuario}` só BFF | 400 formato; 401 genérico; 429 taxa; 503 DB |
| GET `/auth/me` | Sem corpo | 200 `{usuario,expiraEm}` | 401 sessão inválida |
| POST `/auth/logout` | Sem corpo | 204 | 401; BFF ainda limpa ticket local |
| POST `/auth/alterar-senha` | `{senhaAtual,novaSenha}` | 204 e revogação total | 400 senha/política; 401 |
| GET `/usuarios` | `page,pageSize,search,ativo` | 200 página UsuarioResumo | 400; 401; 403 |
| GET `/usuarios/{id}` | UUID | 200 UsuarioResumo | 401/403/404 |
| PUT `/usuarios/{id}` | `{perfil,ativo,versao}` | 200 UsuarioResumo | 400; 409 VERSAO_DESATUALIZADA/ULTIMO_ADMINISTRADOR |
| POST `/usuarios/{id}/redefinir-senha` | `{senhaTemporaria,versaoEsperada}` | 204 | 400 política; 404; 409 versão |

GET/PUT/redefinição exigem Usuarios.Gerenciar. POST `/usuarios` ainda não é mapeado/anunciado neste bloco; chega no 02. Endpoints HTTP Web, distintos da API: GET/POST `/conta/entrar`, POST `/conta/sair`, GET/POST `/conta/senha`. POSTs validam antiforgery; sucesso usa redirect 303 para rota local, nunca devolve token. CSRF inválido → 400 seguro, excesso de taxa → 429, API indisponível → 503 com possibilidade de tentar novamente.

Credenciais inválidas no POST Web devolvem formulário com status401 e mensagem genérica, sem ecoar senha. Troca de senha inválida devolve400 com erro associado ao campo; nenhum redirect de sucesso. Falhas anônimas de acesso auditam evento com ator ausente/origem anônima e dados mínimos; mutação administrativa exige ator autenticado.

## 10. Páginas, componentes e estados

| Rota | Componentes propostos | Vazio / carregando / sucesso / erro |
| --- | --- | --- |
| `/conta/entrar` | FormularioLogin SSR, ResumoValidacao | Campos vazios; enviando sem duplo submit; redirect local; erro genérico de acesso |
| `/conta/senha` | FormularioSenha SSR | Solicita senha atual/nova; enviando; troca e novo login; erro sem eco da senha |
| `/usuarios` | ListaUsuarios, Filtros, Paginacao | Nenhum resultado; carregando; lista autorizada; erro/Repetir |
| `/usuarios/{id}` | DetalheUsuario, EditorPerfilStatus, RedefinicaoSenha | Não localizado; carregando; confirmação da mudança; 409 pede reler |
| `/` | InicioAutenticado, AtalhosPermitidos | Sem atalhos futuros; carregando sessão; nome/perfil; expiração redireciona |

Senhas não persistem em estado serializado. Login/logout não alteram headers dentro de circuito. Não exibir botão de criar usuário ainda.

## 11. Erros e concorrência

Atualizar efetivamente guardVersion do singleton administration_control no início da transação administrativa; ler/revalidar usuários depois, incluindo contagem de Admins e versão esperada. Toda criação/ativação/desativação/troca de perfil de conta usa o mesmo controle: conflito de escrita reinicia a transação com snapshot novo. PUT com versão antiga retorna 409, sem overwrite. Login e revogação escrevem o mesmo documento de usuário/securityStamp, evitando sessão criada com stamp obsoleto. Stores e auditoria compartilham a sessão interna da operação. Chamadas BFF usam credencial por request, sem headers/handlers compartilhados. API indisponível não concede autorização por cookie isolado.

## 12. Testes unitários e de integração

Unidade: expiração absoluta/inativa com relógio controlado, restrição de returnUrl, política de senha/perfil e isolamento do handler. Integração: Identity/MongoDB, bootstrap repetido, login inválido/bloqueado, troca temporária, 401/403, CSRF, revogação, reconexão, dois tickets sem troca de credencial, logout offline, proteção concorrente do último Admin. Inspecionar browser/HTML/storage/logs sem token da API. Fixtures de dois Admins são somente teste.

## 13. Cenários Given/When/Then

| ID local / rastreio | Given | When | Then |
| --- | --- | --- | --- |
| M01-CA01 / CA-01 | Bootstrap ativo e senha válida | Login, troca obrigatória e novo login | Cookie de referência; API token só no servidor; home acessível |
| M01-CA02 / CA-02 | Dois tickets ativos | Alterar senha/perfil ou avançar relógio 30 min sem uso | Próxima chamada 401 e UI revalida em até 60 s |
| M01-CA03 / CA-17 | Dois Admins em fixtures independentes | Ambos tentam desativar/rebaixar o último acesso simultaneamente | Pelo menos um Admin ativo, uma operação 409 quando necessária |
| M01-CA04 / CA-17 | Cookie válido | POST logout sem antiforgery | 400 e sessão não é encerrada pelo pedido forjado |
| M01-CA05 / CA-01 | Sessão Web válida e API fora | Logout | Ticket/cookie locais removidos; falha API registrada sem token |

## 14. Checklist de execução

- [x] Aplicar evoluções de persistência de Identity/sessões/auditoria/controle e gestao_web.tickets do zero e sobre baseline 00.
- [x] Revisar segredos/key ring e executar bootstrap somente no banco explicitamente configurado.
- [x] Implementar contratos/handlers/policies/SSR e testes sem funcionário futuro.
- [x] Executar `dotnet restore`, `dotnet build --no-restore -c Release`, `dotnet test -c Release --no-build` com MongoDB isolado.
- [x] Registrar contratos OpenAPI deste bloco, inspeção de cookies, cenários e falhas.
- [x] Atualizar estado: RF-03 parcial; POST usuários permanece para 02.

## 15. DoD e próximo bloco

DoD global e M01-CA01–05 aprovados; nenhum token exposto; expiração/revogação/último Admin testados. Desbloquear **02**, que acrescenta vínculo e criação humana. Nenhum gate exige cadastrar funcionário ou dashboard.

## Registro da entrega01

Backend e Web utilizam MongoDB Atlas existente, configuração externa/.env preservada e bancos separados. Entregues Identity/stores mínimos, users e índice de email, sessões opacas, hash oficial, bootstrap CLI, login/logout/me/troca obrigatória, manutenção de perfil/status/redefinição, auditoria atômica e guarda do último Admin. Web usa ITicketStore cifrado, cookie de referência, SSR/antiforgery, proteção de rotas, listas/edição de contas e revalidação do circuito. Nenhum cadastro humano/funcionário ou recurso02 foi acrescentado.

Os contratos com senha/token são exclusivos de transporte interno dos hosts; Contracts contém somente DTOs públicos de sessão/usuários. Inicializadores explícitos aplicam00+01; runtime não altera coleções. Bootstrap de aplicação no desenvolvimento aguarda senha externa escolhida pelo responsável; o executor não reutilizou a senha de banco nem definiu credencial padrão. Inicialização e bootstrap idempotentes foram comprovados em bancos descartáveis. Resultados, arquivos e limitações reais estão em03.
