# 07 — Autenticação e segurança

## Escolha: BFF com cookie de referência e sessão opaca na API

Blazor Interactive Server executa no servidor. O navegador recebe apenas cookie de sessão do site e o circuito SignalR; o BFF chama a API usando uma credencial de sessão por usuário. A API reutiliza ASP.NET Core Identity para hash, políticas de senha e lockout, com stores próprios mínimos sobre MongoDB.Driver (ADR-24); um handler de autenticação resolve sessões opacas persistidas. Não é um servidor OAuth/OIDC e não atende clientes externos no MVP.

Essa decisão evita emitir JWT próprio e evita token em JavaScript, localStorage, sessionStorage, HTML, URL ou estado serializado do Blazor. O token necessariamente trafega entre servidores, e a senha trafega do formulário por HTTPS durante login; o objetivo é não armazená-los nem disponibilizar o token da API ao navegador. Nunca afirmar que a senha não passa pelo navegador.

## Fluxo obrigatório

```mermaid
sequenceDiagram
    participant N as Navegador
    participant W as Blazor / BFF
    participant A as API / Identity
    participant D as Armazenamento
    N->>W: POST /conta/entrar (senha + antiforgery)
    W->>A: POST /api/v1/auth/login por TLS
    A->>D: Validar Identity; salvar hash da sessão
    A-->>W: Token opaco uma única vez
    W->>D: Ticket e token protegidos no banco único gestao_comercial_dev
    W-->>N: Set-Cookie de referência + redirect local
    N->>W: Abrir circuito autenticado
    W->>A: Chamada de negócio com token do usuário
    A->>D: Validar sessão e permissões atuais
    A-->>W: DTO sem credenciais
    W-->>N: Interface atualizada
```

Login/logout e troca de senha devem usar endpoints HTTP do host Web e navegação completa, não `SignInAsync`/alteração de headers dentro de circuito já iniciado. Formulários SSR protegem com antiforgery. Não depender de `HttpContext` vivo nos eventos de componentes.

## Armazenamento e ciclo de vida

- Token API: 32 bytes aleatórios criptograficamente seguros, Base64URL; banco API guarda somente SHA-256, usuário, timestamps e revogação. Comparação e consulta pelo hash; nunca usar hash simples de senha.
- Cookie `__Host-GestaoComercial`: Secure, HttpOnly, SameSite=Lax, Path=/, sem Domain; referência aleatória a ticket persistido via `ITicketStore`. Ticket não contém token em claro.
- Banco único gestao_comercial_dev guarda ticket e token protegidos por ASP.NET Core Data Protection. Persistir key ring em volume privado, proteger acesso e cópia de backup; perder chaves invalida sessões. Sem key ring persistente, publicação não está pronta.
- API: 8 h absolutas e 30 min de inatividade. Chamadas comerciais autorizadas atualizam último uso de modo atômico; `/auth/me`, health e revalidação não estendem inatividade. Expiração não é prorrogada além de 8 h. Sem refresh token no MVP.
- Web: ticket expira em no máximo a expiração absoluta API. Revalidar a sessão API a cada 60 s sem estender inatividade; 401/403 por revogação encerra uso e redireciona para novo login com navegação completa. Durante indisponibilidade, não autorizar operações com base apenas no cookie.
- Claims do circuito servem à apresentação. A API consulta usuário/perfil atuais em toda requisição. Mudança de perfil, senha ou inativação revoga todas as sessões em transação.
- Logout normal revoga sessão API, remove ticket e limpa cookie. Se API indisponível, limpar ticket/cookie mesmo assim e registrar falha de revogação sem token; token residual fica sem acesso pelo navegador e expira em até 8 h. Esse risco operacional é aceito no MVP e deve constar na evidência de falha.

## Isolamento entre usuários

Handler HTTP do BFF obtém credencial do contexto de sessão explicitamente a cada chamada. Não manter token em `DefaultRequestHeaders` de cliente compartilhado, singleton, CookieContainer compartilhado ou handler pooled. Usar `HttpRequestMessage` com Authorization próprio. Serviço scoped de circuito conserva somente identificador da sessão; a credencial é recuperada do armazenamento técnico protegido.

Prerenderização não pode renderizar tokens/estado de autenticação sensível nem enviar comandos de negócio duplicados. Testar dois usuários simultâneos e SSR → circuito → reconexão. Abrir nova conexão exige ticket válido; circuitos existentes continuam sujeitos à autorização da API.

## Contas e autorização

- Sem cadastro público. Bootstrap por tarefa administrativa única, usando segredo externo, somente quando não existe administrador; não há endpoint público de bootstrap nem senha padrão.
- Email de login único, senha mínima 12 caracteres, maiúscula/minúscula/dígito/símbolo; máximo 128; Identity para hash e validação. Senha temporária exige troca; troca revoga sessões e pede novo login.
- Cinco tentativas incorretas bloqueiam conta por 15 min. Limitar login também por IP e identificador normalizado; registrar falhas sem senha. Resposta genérica evita enumeração.
- Um perfil por conta: Administrador, Vendedor ou Estoquista. Policies centralizadas, negação por padrão. Não permitir autoinclusão de perfil no request.
- Não desativar/rebaixar o último administrador ativo nem remover seu acesso por concorrência; protocolo de guarda administrativa descrito no banco. Bootstrap é a única conta sem vínculo de funcionário obrigatória.
- Usuário inativo, funcionário inativo, sessão revogada/expirada: 401 mesmo que claims antigos indiquem acesso. Falta de policy: 403. Vendedor fora do escopo de propriedade: 404.

Na regra acima, funcionário só é exigido quando há vínculo humano, introduzido no02; o bootstrap é a exceção sem funcionário.01 não consulta coleção employees futura. Após02, inativação do funcionário revoga/desativa a conta na mesma transação e o backend valida usuário/stamp atuais; nenhuma claim do circuito substitui essa garantia. Troca obrigatória não bloqueia me/logout/alterar-senha, conforme matriz. Fixtures sintéticas de perfis em01 não constituem criação pública de contas humanas nem requisito de módulo futuro.

## Proteções de entrada e dados

TLS no site e no canal API, proxy configurado com hosts confiáveis e WebSocket; encaminhamento de headers somente de proxies conhecidos. API em rede privada sem CORS liberado ao navegador. SameSite não substitui antiforgery em endpoints Web com cookie. Validar Origin/antiforgery no suporte do framework ao hub; não desabilitar proteções para resolver conexão.

Usar filtros tipados e campos/operadores permitidos, sem concatenar consultas nem aceitar filtros BSON do cliente, validar limites/enumerações, codificar saída HTML, não renderizar descrição como HTML arbitrário. Rejeitar redirecionamento externo em `returnUrl`. CSP inicial restritiva compatível com Blazor, HSTS em produção, cabeçalhos de tipo e proteção contra framing; validar no navegador.

Imagens: somente JPEG/PNG reais verificados por assinatura e decodificação, máximo 2 MiB/4096×4096, sem SVG, nome aleatório e metadados removidos ao reencodar. Armazenar fora do webroot e servir após autorização. Troca de imagem grava novo arquivo temporário validado, atualiza metadados com versão, e só depois remove antigo; falha mantém antigo, órfãos são limpos por rotina documentada sem apagar arquivo referenciado.

CSV deve neutralizar fórmulas iniciadas por `=`, `+`, `-`, `@`, tab ou CR nos campos textuais e escapar delimitador/aspas/quebra de linha. Campos numéricos são gerados de tipos numéricos, não de texto do usuário.

## Auditoria e privacidade

Auditar login/falha/logout, mudanças de conta/perfil, cadastros, imagens, movimentos, confirmação e cancelamento. Mutação de negócio e sua auditoria compartilham transação. Logs técnicos usam traceId; não contêm senha, Authorization, cookie, documento integral ou payload indiscriminado. Histórico funcional pode conter snapshots mínimos autorizados.

Não há decisão jurídica de retenção nesta especificação. Q-03 exige responsável definir retenção e tratamento de dados pessoais antes de produção com dados reais. Homologação e demonstração usam dados sintéticos. Não apagar registros históricos automaticamente para resolver essa questão.

## Limite da futura superfície pública

No MVP, somente login via BFF e health mínimo têm as exceções descritas acima. A ADR-18 é proposta posterior: catálogo e solicitação anônima terão rotas/DTOs próprios permitidos pelo BFF, sem abrir a rede/API interna inteira nem usar cookie como autorização na API. Nome/telefone públicos exigem decisão de tratamento em Q-03 e não aparecem no recibo nem em GET público de pedido (que não existe na proposta). Limites/rastreamento de abuso e dedupe estão especificados no módulo11; não alteram a estratégia de sessão administrativa.

## Verificação e fontes oficiais

CA-01/02/13/17 exigem CSRF, expiração, revogação, acesso direto à API, isolamento entre usuários e ausência de tokens nas ferramentas do navegador. Orientações oficiais: [segurança do Blazor](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/?view=aspnetcore-10.0), [cenários server-side](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/additional-scenarios?view=aspnetcore-10.0) e [antiforgery ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0). A sessão opaca, seus prazos e o armazenamento BFF são escolhas locais, não um recurso OAuth pronto dessas páginas.

## Stores Identity e consistência MongoDB

Persistir users com passwordHash gerado e verificado por IPasswordHasher do ASP.NET Core Identity (formato versionado, salt e parâmetros do framework), sem algoritmo caseiro, senha reversível ou provider comunitário presumido. Fixar política e testar rehash necessário. Implementar apenas interfaces de store exigidas por UserManager/validadores/políticas efetivamente usados: usuário, senha, email, security stamp e lockout; perfil único é campo permitido, sem editor/coleção dinâmica de roles. Integrar concorrência do store à versão do documento. Tokens de redefinição, se usados, são de Identity/Data Protection e não tokens de sessão API. A persistência de Identity precisa ser implementada explicitamente, sem troca automática de store.

Mutação administrativa coordena explicitamente uma sessão MongoDB compartilhada pelos stores, sessões e auditoria: incremento real de administration_control, revalidação de Admin/versões/vínculo, updates e revogação, um commit. Implementações internas scoped podem receber a sessão da operação; não abrir sessão própria por store nem fazer commit antecipado em UserManager. Isso é coordenação concreta de caso de uso, sem Unit of Work artificial. Bootstrap também usa o controle e índice de email; somente o bootstrap admite employeeId ausente.

Login precisa serializar leitura de estado com alteração de senha/perfil/inativação: atualizar efetivamente user/guardVersion e conferir securityStamp antes de inserir api_session com o mesmo stamp na mesma transação. Lockout e contador usam updates condicionais, sem perda de tentativas simultâneas. Rehash, se necessário, é atualização versionada; não reativar conta/sessão como efeito colateral. Revogação global por alteração de senha/perfil/inativação altera securityStamp/estado no usuário e marca sessões na mesma transação; logout normal revoga somente a sessão atual e não troca o stamp global. Sessão com stamp divergente é inválida mesmo antes de limpeza física.

Toda requisição valida sessão, expiração e usuário atual no primary. Para cumprir negação de requisições iniciadas após commit de revogação, consulta de sessão pelo tokenHash único e leitura de estado de usuário por _id usam readConcern linearizable no primary, fora de transação e com maxTimeMS, em sessão explícita não causal (CausalConsistency=false), seguidas da comparação de securityStamp; não usar cache/secondary como autoridade. Falha de leitura atual bloqueia acesso, sem fallback para claims antigos. Leituras/comandos já iniciados antes da revogação podem concluir como antes; não prometer cancelamento retroativo. Testar a fronteira temporal com sessões independentes.

MongoDB.Driver não é mecanismo de autenticação HTTP: cookie BFF e bearer opaco permanecem. TTL de api_sessions/tickets é limpeza eventual; expiração absoluta/inativa é validada no backend em toda chamada. Não aplicar TTL em recibos comerciais/auditoria. RBAC MongoDB limita API ao banco comercial e Web a tickets, sem leitura cruzada, sem bypassDocumentValidation; segredos URI/keyfile/key ring externos e nunca logados.

Referências oficiais da implementação: [stores personalizados Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-custom-storage-providers?view=aspnetcore-10.0), [hash de senha Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-configuration?view=aspnetcore-10.0), [read concern linearizable](https://www.mongodb.com/docs/manual/reference/read-concern-linearizable/). Stores e sessões foram implementados no bloco01; resultados dos gates no estado03.

## Implementação01 — parâmetros operacionais

Somente Administrador, Vendedor e Estoquista (ADR-30). A identificação Identity é o email normalizado único, incluindo inativos. Os seis stores mínimos implementados são usuário, senha, email, stamp e lockout (IUserStore mais cinco interfaces); confirmação por email/exclusão física estão fora do MVP e são recusadas explicitamente. PasswordHasher oficial mantém salt/formato versionado e rehash testado; a política é validada por Identity. Intervalos UTC usam TimeProvider.

API valida o token a cada request com leitura linearizável; comandos revalidam o ator na transação. Login/contador escrevem a guarda do usuário e alterações administrativas escrevem o singleton antes das leituras. Até duas repetições transitórias, backoff crescente e orçamento total5s; commit incerto repete somente commit. Login com resultado perdido pode deixar sessão inacessível até expirar, sem criar acesso no navegador.

API de login:300 chamadas/minuto por IP (o IP visto pela API privada normalmente é do BFF) e10 por minuto por hash do identificador; Web limita30/minuto por IP de origem. Cinco senhas erradas bloqueiam15min. Limites em memória por instância são adequados à única réplica do MVP; múltiplas réplicas exigem limitador coordenado. Nunca confiar em X-Forwarded-For não configurado.

Logout Web dispensa consulta remota ao validar o cookie somente nessa rota, mantendo antiforgery e remoção local. Assim a indisponibilidade da API não impede sair. Outras rotas/circuitos falham fechados. Cookie revogado remove ticket local antes do novo login; conta já conectada deve sair para trocar de identidade. O circuito recarrega a página ao perder autorização na revalidação de60s. Origin cruzado é recusado nas rotas /_blazor; HTTPS e política SameSite permanecem ativos.

Api__BaseUrl usa a porta de negócio7143 local/8443 na rede Compose; Api__HealthBaseUrl usa8081 privado. As duas URLs exigem HTTPS. A Web referencia somente Contracts e seu banco técnico; tokens não compõem claims nem estado serializado. Não há coleção employees nem consulta a módulo futuro. Não há TTL habilitado: índices de expiração preparam limpeza operacional posterior e autorização sempre verifica prazos.

## Incremento02–04

A descrição de01 acima é histórica quanto à ausência de employees. Criação humana agora exige funcionário ativo, vínculo único inclusive inativo e senha temporária por formulário HTTP SSR/antiforgery. API verifica funcionário vinculado na leitura atual da sessão; desativação revoga conta/sessões no mesmo commit. Reativar funcionário não reativa conta. Administrador pode desativar outro Admin somente preservando o último ativo (ADR-31). Clientes não têm login. Produtos.Ler aceita os três perfis; Produtos.Gerenciar somente Administrador/Estoquista. Imagens privadas reencodadas são entregues ao navegador por /imagens/produtos/{id}, com cookie validado e bearer somente entre servidores; não há diretório público ou token em URL.

Exceção acadêmica ADR-39 (09/11/2026): o Administrador pode limpar explicitamente os dados comerciais, inclusive histórico/manual, na página de demonstração, preservando sua conta/vínculo/sessão. Demais sessões são removidas na mesma transação. Confirmação textual obrigatória e UUID de operação reconhecido em recibo auditado impedem repetição destrutiva após resposta perdida. Não modifica autorização ou preservação histórica das operações normais; não executar automaticamente.
