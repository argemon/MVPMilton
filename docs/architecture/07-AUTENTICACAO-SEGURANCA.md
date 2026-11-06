# 07 — Autenticação e segurança

## Escolha: BFF com cookie de referência e sessão opaca na API

Blazor Interactive Server executa no servidor. O navegador recebe apenas cookie de sessão do site e o circuito SignalR; o BFF chama a API usando uma credencial de sessão por usuário. A API usa ASP.NET Core Identity para hashes, senha, bloqueio e contas; um handler de autenticação resolve sessões opacas persistidas. Não é um servidor OAuth/OIDC e não atende clientes externos no MVP.

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
    W->>D: Ticket e token protegidos no schema web
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
- Schema web guarda ticket e token protegidos por ASP.NET Core Data Protection. Persistir key ring em volume privado, proteger acesso e cópia de backup; perder chaves invalida sessões. Sem key ring persistente, publicação não está pronta.
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
- Um perfil por conta: Administrador, Gerente, Vendedor ou Estoquista. Policies centralizadas, negação por padrão. Não permitir autoinclusão de perfil no request.
- Não desativar/rebaixar o último administrador ativo nem remover seu acesso por concorrência; lock administrativo descrito no banco. Bootstrap é a única conta sem vínculo de funcionário obrigatória.
- Usuário inativo, funcionário inativo, sessão revogada/expirada: 401 mesmo que claims antigos indiquem acesso. Falta de policy: 403. Vendedor fora do escopo de propriedade: 404.

## Proteções de entrada e dados

TLS no site e no canal API, proxy configurado com hosts confiáveis e WebSocket; encaminhamento de headers somente de proxies conhecidos. API em rede privada sem CORS liberado ao navegador. SameSite não substitui antiforgery em endpoints Web com cookie. Validar Origin/antiforgery no suporte do framework ao hub; não desabilitar proteções para resolver conexão.

Usar consultas parametrizadas, validar limites/enumerações, codificar saída HTML, não renderizar descrição como HTML arbitrário. Rejeitar redirecionamento externo em `returnUrl`. CSP inicial restritiva compatível com Blazor, HSTS em produção, cabeçalhos de tipo e proteção contra framing; validar no navegador.

Imagens: somente JPEG/PNG reais verificados por assinatura e decodificação, máximo 2 MiB/4096×4096, sem SVG, nome aleatório e metadados removidos ao reencodar. Armazenar fora do webroot e servir após autorização. Troca de imagem grava novo arquivo temporário validado, atualiza metadados com versão, e só depois remove antigo; falha mantém antigo, órfãos são limpos por rotina documentada sem apagar arquivo referenciado.

CSV deve neutralizar fórmulas iniciadas por `=`, `+`, `-`, `@`, tab ou CR nos campos textuais e escapar delimitador/aspas/quebra de linha. Campos numéricos são gerados de tipos numéricos, não de texto do usuário.

## Auditoria e privacidade

Auditar login/falha/logout, mudanças de conta/perfil, cadastros, imagens, movimentos, confirmação e cancelamento. Mutação de negócio e sua auditoria compartilham transação. Logs técnicos usam traceId; não contêm senha, Authorization, cookie, documento integral ou payload indiscriminado. Histórico funcional pode conter snapshots mínimos autorizados.

Não há decisão jurídica de retenção nesta especificação. Q-03 exige responsável definir retenção e tratamento de dados pessoais antes de produção com dados reais. Homologação e demonstração usam dados sintéticos. Não apagar registros históricos automaticamente para resolver essa questão.

## Limite da futura superfície pública

No MVP, somente login via BFF e health mínimo têm as exceções descritas acima. A ADR-18 é proposta posterior: catálogo e solicitação anônima terão rotas/DTOs próprios permitidos pelo BFF, sem abrir a rede/API interna inteira nem usar cookie como autorização na API. Nome/telefone públicos exigem decisão de tratamento em Q-03 e não aparecem no recibo nem em GET público de pedido (que não existe na proposta). Limites/rastreamento de abuso e dedupe estão especificados no módulo11; não alteram a estratégia de sessão administrativa.

## Verificação e fontes oficiais

CA-01/02/13/17 exigem CSRF, expiração, revogação, acesso direto à API, isolamento entre usuários e ausência de tokens nas ferramentas do navegador. Orientações oficiais: [segurança do Blazor](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/?view=aspnetcore-10.0), [cenários server-side](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/additional-scenarios?view=aspnetcore-10.0) e [antiforgery ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0). A sessão opaca, seus prazos e o armazenamento BFF são escolhas locais, não um recurso OAuth pronto dessas páginas.
