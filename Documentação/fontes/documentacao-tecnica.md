# Documentação técnica do Gestão Comercial

Especificação do protótipo acadêmico na versão 1940b83460ec93d8efa2011db9dc9177570b0586. Data da análise: 24 de novembro de 2026. Documento complementar à monografia, destinado à banca, à manutenção e à conferência dos requisitos. Não constitui nova homologação funcional.

<!-- toc -->

# 1 Escopo e critérios de interpretação

O sistema atende uma empresa hipotética, moeda BRL, quantidades inteiras em UN e um estoque por produto. API e Web executam localmente, com o único banco Atlas existente. Os três perfis são Administrador, Vendedor e Estoquista. Não existe cadastro público, catálogo público, pedido online, pagamento, integração fiscal, desconto, frete, custo/lucro ou devolução parcial.

A baseline funcional é RF-01 a RF-20 e suas regras RN-01 a RN-20. A ADR-36 amplia as análises e a ADR-39 delimita a limpeza explícita da demonstração. Trechos antigos sobre módulo11, perfil anterior, bancos separados ou tecnologias anteriores não são especificação vigente. Os documentos existentes são preservados; esta entrega explicita a precedência para leitura acadêmica.

## 1.1 Divergências e cuidados documentais

| Tema | Fonte histórica | Interpretação da versão analisada |
| --- | --- | --- |
| Classes de negócio | Modelo conceitual com Cliente, Produto e Venda | Nem todos são classes Domain; há funções estáticas, records Application e documentos Infrastructure |
| Nome físico de usuário | Dicionário antigo menciona nomeExibicao | AuthSchema e mapeamento usam nome; transporte usa Nome |
| Segurança por policy | Matriz lista algumas policies conceituais de leitura | Endpoints de clientes/funcionários usam policies de manutenção também nas leituras; resultado por perfil é mantido |
| Lista de usuários | Convenção geral usa totalItems e ativo textual | UsuariosResponse tem Total, JSON total; endpoint recebe bool? ativo e não a string todos |
| Metadados | Convenção genérica sugere schemaVersion universal | administration_control define somente _id e guardVersion; aplicar schema físico de cada coleção |
| Recibos | Texto antigo admite campos de conclusão ausentes | Schema exige campos; estado Preparando usa resposta vazia dentro da transação ainda invisível |
| Desempenho e backup | RNF-05/06 e partes de CA-16/18 mantêm metas antigas | Escopo ADR-37 exclui ensaios massivos e obrigações de produção; metas não são resultados demonstrados |
| Execução frontend | Proibição de acesso ao banco comercial | Web usa MongoDB.Driver para tickets técnicos no mesmo banco, sem referência aos projetos backend |

## 1.2 Convenção de evidências

Inspeção atual é leitura de código e configuração pública. Resultado histórico é o que foi registrado com data/commit anterior. Pendente é o que não foi demonstrado nesta entrega. A matriz liga a teste existente; não significa teste executado. Não houve acesso ao banco, alteração de configuração privada ou execução de jornada comercial durante a produção documental.

# 2 Requisitos e regras consolidados

Os identificadores seguintes são preservados dos documentos oficiais. Descrições reproduzem a especificação do projeto e devem ser lidas com as ressalvas do capítulo 1 e com os contratos reais. Prioridades P0/P1 são classificações do projeto, não classificação de gravidade de vulnerabilidade.

## 2.1 Requisitos funcionais

### RF-01

**Prioridade / ator:** P0 / todos

**Fluxo e resultado observável:** Entrar por email/senha, abrir sessão e mostrar nome/perfil; falha genérica

**Regras:** RN-15/16

**Aceite / bloco:** CA-01 / 01

### RF-02

**Prioridade / ator:** P0 / todos

**Fluxo e resultado observável:** Sair revoga sessão; expiração e alteração de acesso impedem novas operações

**Regras:** RN-15/16

**Aceite / bloco:** CA-01/02 / 01

### RF-03

**Prioridade / ator:** P0 / Admin

**Fluxo e resultado observável:** Criar conta vinculada, definir perfil, ativar/desativar e redefinir senha sem revelar hashes

**Regras:** RN-15/16/18

**Aceite / bloco:** CA-02/17 / 01–02

### RF-04

**Prioridade / ator:** P1 / Admin

**Fluxo e resultado observável:** Cadastrar/editar/listar funcionário e desativar com revogação de acesso vinculado

**Regras:** RN-08/16/18

**Aceite / bloco:** CA-03 / 02

### RF-05

**Prioridade / ator:** P1 / Admin, Vendedor

**Fluxo e resultado observável:** Cadastrar/editar/pesquisar cliente por nome/documento e desativar

**Regras:** RN-08/12

**Aceite / bloco:** CA-04 / 03

### RF-06

**Prioridade / ator:** P1 / Admin, Vendedor

**Fluxo e resultado observável:** Consultar histórico paginado do cliente respeitando escopo de vendas

**Regras:** RN-07/17

**Aceite / bloco:** CA-04/13 / 03 e 06

### RF-07

**Prioridade / ator:** P1 / Admin, Estoquista

**Fluxo e resultado observável:** Manter categorias/marcas, sem nome normalizado duplicado

**Regras:** RN-08/12

**Aceite / bloco:** CA-05 / 04

### RF-08

**Prioridade / ator:** P1 / Admin, Estoquista

**Fluxo e resultado observável:** Manter produto com SKU, preço, imagem, categoria, marca opcional e status

**Regras:** RN-01/08/12/19

**Aceite / bloco:** CA-05/17 / 04

### RF-09

**Prioridade / ator:** P0 / Admin, Estoquista

**Fluxo e resultado observável:** Registrar entrada manual com quantidade e motivo; saldo e histórico juntos

**Regras:** RN-02/06/09/14

**Aceite / bloco:** CA-06 / 05

### RF-10

**Prioridade / ator:** P0 / Admin, Estoquista

**Fluxo e resultado observável:** Registrar saída manual sem gerar saldo negativo

**Regras:** RN-02/06/09/14

**Aceite / bloco:** CA-06/07 / 05

### RF-11

**Prioridade / ator:** P0 / Admin

**Fluxo e resultado observável:** Ajustar saldo por delta justificado, preservando movimento anterior

**Regras:** RN-02/06/09/14

**Aceite / bloco:** CA-06/07/13 / 05

### RF-12

**Prioridade / ator:** P1 / leitores de estoque

**Fluxo e resultado observável:** Listar saldo e histórico por produto/período/tipo

**Regras:** RN-07/09/20

**Aceite / bloco:** CA-06/15 / 05

### RF-13

**Prioridade / ator:** P0 / Admin, Vendedor

**Fluxo e resultado observável:** Criar/editar/descartar rascunho com cliente e itens; total calculado no servidor

**Regras:** RN-01/10/11/17

**Aceite / bloco:** CA-08 / 06

### RF-14

**Prioridade / ator:** P0 / Admin, Vendedor

**Fluxo e resultado observável:** Confirmar rascunho, congelar snapshots e baixar estoque atomicamente

**Regras:** RN-02/03/04/05/10/11

**Aceite / bloco:** CA-08/09/10/11 / 06

### RF-15

**Prioridade / ator:** P1 / Admin, Vendedor

**Fluxo e resultado observável:** Consultar venda, itens, estado e histórico do ciclo com paginação

**Regras:** RN-07/17

**Aceite / bloco:** CA-08/13/14 / 06

### RF-16

**Prioridade / ator:** P0 / Admin

**Fluxo e resultado observável:** Cancelar confirmada com motivo e estorno exato uma única vez

**Regras:** RN-05/06/07/10

**Aceite / bloco:** CA-12/13 / 06

### RF-17

**Prioridade / ator:** P1 / Admin

**Fluxo e resultado observável:** Ver indicadores por período conforme fórmula abaixo

**Regras:** RN-13/20

**Aceite / bloco:** CA-14/15 / 07

### RF-18

**Prioridade / ator:** P1 / Admin

**Fluxo e resultado observável:** Ver produtos ativos com saldo <= mínimo e acesso à lista completa

**Regras:** RN-02/13

**Aceite / bloco:** CA-14 / 07

### RF-19

**Prioridade / ator:** P1 / Admin

**Fluxo e resultado observável:** Relatório paginado/CSV de vendas com totais do filtro completo

**Regras:** RN-07/13/19/20

**Aceite / bloco:** CA-14/15/17 / 08

### RF-20

**Prioridade / ator:** P1 / Admin, Estoquista nos relatórios de estoque

**Fluxo e resultado observável:** Relatórios/CSV de saldo atual e movimentações filtradas com totais

**Regras:** RN-09/19/20

**Aceite / bloco:** CA-15/17 / 08

### RF-ADR39 Limpeza explícita

Administrador pode limpar dados da instalação acadêmica mediante confirmação APAGAR DADOS e UUID. Preserva executor, vínculo e sessão, conforme UC29; não gera nova carga automaticamente.
## 2.2 Requisitos não funcionais

### RNF-01

**Prioridade:** P0

**Requisito e medição:** API nega requisição anônima, sem policy ou com sessão revogada; token API ausente de navegador/HTML/storage

**Gate / evidência:** 01 e 09; CA-01/02/13/17, testes BFF e HTTP

### RNF-02

**Prioridade:** P0

**Requisito e medição:** Saldo nunca negativo, confirmação/cancelamento indivisíveis, um efeito por intenção

**Gate / evidência:** 05/06; CA-07/09/10/11/12 em MongoDB

### RNF-03

**Prioridade:** P1

**Requisito e medição:** UI pt-BR responsiva em 360 e 1280 px, teclado operável, inputs com labels e erro associado

**Gate / evidência:** Cada tela e 09; CA-18 com roteiro

### RNF-04

**Prioridade:** P0

**Requisito e medição:** Nenhum segredo versionado/logado; dados pessoais mínimos; TLS e arquivos privados

**Gate / evidência:** 00/01/04/10; revisão e CA-17

### RNF-05

**Prioridade:** P1

**Requisito e medição:** Listas p95 <= 2 s e confirmação p95 <= 3 s com 20 usuários, base/medição da arquitetura 09

**Gate / evidência:** 09; relatório de medição com ambiente e consultas

**Situação documental:** meta histórica não medida nesta entrega; ensaio massivo excluído pelo escopo acadêmico ADR-37.

### RNF-06

**Prioridade:** P0

**Requisito e medição:** Backup diário externo criptografado, RPO <= 24 h, restore medido <= 4 h, retenção operacional 30 dias

**Gate / evidência:** 10; CA-16, cópia e restauração sintéticas

**Situação documental:** obrigação operacional histórica; não comprovada e fora do gate acadêmico vigente. Restaurar carga não é restaurar backup completo.

### RNF-07

**Prioridade:** P0

**Requisito e medição:** Build Release e testes xUnit passam; regras críticas possuem testes de integração reais em MongoDB isolado com suporte transacional (Atlas ou perfil Testcontainers)

**Gate / evidência:** Cada bloco; logs reais da execução

### RNF-08

**Prioridade:** P1

**Requisito e medição:** Evoluções de persistência criam do zero e atualizam base anterior sem perder histórico

**Gate / evidência:** Cada alteração DB e 09; teste upgrade/validadores e filtros condicionais

### RNF-09

**Prioridade:** P1

**Requisito e medição:** Frontend depende só de contratos; Domain sem BSON/driver/HTTP; Application sem Infrastructure

**Gate / evidência:** 00 e revisão por bloco; inspeção referências

### RNF-10

**Prioridade:** P1

**Requisito e medição:** Logs rastreiam traceId, usuário/recurso permitido e erro, sem credenciais; health sem detalhes

**Gate / evidência:** 00/01/09; CA-17 e simulação de falha

### RNF-11

**Prioridade:** P1

**Requisito e medição:** Operação sensível audita ator/data/motivo junto ao efeito, sem permitir editar/apagar histórico

**Gate / evidência:** 02–06; CA-03/06/12 e validadores e filtros condicionais

### RNF-12

**Prioridade:** P1

**Requisito e medição:** Paginação no servidor, máximo 100 por página, relatórios com período <= 366 dias e exportação <= 10 mil linhas

**Gate / evidência:** 03–08; CA-15 e limites HTTP
## 2.3 Regras de negócio

### RN-01

**Regra normativa:** Dinheiro interno em centavos Int64 BRL; preço 1–999.999.999 centavos e total <=99.999.999.999.999 centavos; HTTP/formatação em reais com até duas casas

**Exemplo / implementação verificável:** 3 × 12,50 = 37,50; terceira casa de entrada é rejeitada, não truncada

### RN-02

**Regra normativa:** Quantidade em UN inteira; saldo >= 0, sem reserva em rascunho

**Exemplo / implementação verificável:** Saldo 1, saída 2 → 409, nenhum movimento

### RN-03

**Regra normativa:** Confirmar grava estado, snapshots, saldo, movimentos, auditoria e idempotência na mesma transação

**Exemplo / implementação verificável:** Um item insuficiente mantém toda a venda Rascunho

### RN-04

**Regra normativa:** Disputas usam updates condicionais, guardas efetivas e revalidação de saldo/preço/status após conflito transacional

**Exemplo / implementação verificável:** Saldo 1, duas vendas de 1 → uma confirma

### RN-05

**Regra normativa:** Repetir intenção não duplica efeitos; estado e índices também protegem nova chave

**Exemplo / implementação verificável:** Duplo clique/retry → uma baixa; cancelar duas vezes → um estorno

### RN-06

**Regra normativa:** Cancelamento só por Administrador de Confirmada, motivo 10–500 caracteres, estorno integral por item

**Exemplo / implementação verificável:** Inativar produto não impede devolver estoque; sem devolução parcial

### RN-07

**Regra normativa:** Histórico confirmado, cancelado e movimentos é imutável; snapshots preservam dados comerciais

**Exemplo / implementação verificável:** Renomear produto não altera nome/preço histórico

### RN-08

**Regra normativa:** Cadastros com referências não são apagados; usar ativo. Inativo não é selecionável/confirmável

**Exemplo / implementação verificável:** Produto inativo continua em relatório e histórico; categoria com produto ativo não inativa

### RN-09

**Regra normativa:** Toda alteração de saldo gera movimento assinado; saldo = soma de deltas; saldo inicial zero

**Exemplo / implementação verificável:** Entrada 10, venda -2, estorno +2 → 10

### RN-10

**Regra normativa:** Estados permitidos: Rascunho → Confirmada → Cancelada ou Rascunho → Descartada

**Exemplo / implementação verificável:** Confirmada não aceita PUT de itens; Descartada não confirma

### RN-11

**Regra normativa:** Cliente ativo e pelo menos um item para confirmar, com todos os produtos ativos; POST/PUT de rascunho também rejeitam produto inativo. Preço vem do cadastro e mudança exige revisão explícita

**Exemplo / implementação verificável:** Preço 10 vira12 →409 PRECO_ALTERADO até PUT atualizar rascunho; inativo não entra em nova venda

### RN-12

**Regra normativa:** SKU, documento informado e nome de marca/categoria são únicos após normalização, inclusive inativos

**Exemplo / implementação verificável:** SKU ` abc ` e `ABC` colidem; CPF só dígitos, válido se informado

### RN-13

**Regra normativa:** Indicadores usam fórmulas RF-17/19; não contam rascunho nem receita cancelada

**Exemplo / implementação verificável:** Duas vendas 25/15, segunda cancelada → receita 25

### RN-14

**Regra normativa:** Entrada/saída manual exige quantidade positiva; ajuste delta não zero; motivo obrigatório 10–500

**Exemplo / implementação verificável:** Ajuste -3 é rejeitado com saldo 2; ajuste não reescreve histórico

### RN-15

**Regra normativa:** Conta não tem cadastro público, perfil único e autorização atual validada na API

**Exemplo / implementação verificável:** UI ocultar botão não concede permissão nem basta como proteção

### RN-16

**Regra normativa:** Inativação de usuário/funcionário, troca de perfil/senha revogam sessões; preservar último Admin

**Exemplo / implementação verificável:** Duas inativações concorrentes nunca deixam zero administradores

### RN-17

**Regra normativa:** Vendedor só consulta/edita/confirma/descarta vendas próprias; administrador acessam todas

**Exemplo / implementação verificável:** Vendedor B obtém 404 ao consultar venda de A

### RN-18

**Regra normativa:** Conta humana vinculada a funcionário ativo, uma por funcionário; bootstrap Admin é exceção única

**Exemplo / implementação verificável:** Cadastrar funcionário não cria credencial automaticamente

### RN-19

**Regra normativa:** Imagens/CSV seguem validação e tratamento seguro; sem arquivos executáveis ou fórmula injetada

**Exemplo / implementação verificável:** SVG rejeitado; texto `=1+1` exportado neutralizado

### RN-20

**Regra normativa:** Intervalo `[inicio,fim)` usa fuso da instalação; relatórios usam confirmação, listas de venda usam criação

**Exemplo / implementação verificável:** Venda às 00 h locais entra no início, instante igual ao fim fica fora

### RN-ADR39 Exceção delimitada

Somente a limpeza explícita confirmada pode remover histórico e dados manuais conforme ADR-39. Fluxos normais e restauração continuam preservando histórico e dependências. UUID concluído não apaga dados posteriores.
## 2.4 Critérios de aceite

### CA-01 — RF-01/02, RNF-01

**Dado:** Conta ativa com senha válida e sessão inexistente

**Quando:** Entrar por formulário e depois sair

**Então:** Cookie HttpOnly/Secure contém referência; token só no servidor; login abre home, logout impede próximo comando; senha inválida retorna mensagem genérica

### CA-02 — RF-02/03, RN-16

**Dado:** Usuário conectado em dois circuitos

**Quando:** Admin inativa conta ou muda perfil; outro teste avança relógio 30 min sem uso

**Então:** Próxima chamada API recebe 401; UI perde acesso em até 60 s na revalidação; nenhuma escrita é aplicada

### CA-03 — RF-04, RN-08/16/18

**Dado:** Funcionário ativo com conta vinculada

**Quando:** Editar contato e desativar

**Então:** Versão cresce; conta e sessões são revogadas na transação; autoria antiga permanece; último Admin não pode ser inativado (ADR-31)

### CA-04 — RF-05/06, RN-12/17

**Dado:** Cliente com CPF sintético válido, duas vendas de vendedores distintos

**Quando:** Pesquisar pelo documento e consultar histórico como um vendedor

**Então:** Localiza uma ficha, documento duplicado dá 409; histórico contém apenas venda própria; inativo permanece consultável com filtro

### CA-05 — RF-07/08, RN-08/12

**Dado:** Categoria e marca ativas

**Quando:** Criar produto e repetir SKU normalizado, depois inativar categoria usada

**Então:** Produto tem saldo 0; duplicata e inativação bloqueada dão 409; GET preserva vínculo e versão

### CA-06 — RF-09/10/11/12, RN-09/14

**Dado:** Produto com saldo 0

**Quando:** Entrada 10, saída 2, ajuste -1 com motivos válidos

**Então:** Saldo 7, três movimentos com ator/data/motivo/saldoAntes/Depois; soma de deltas 7; histórico paginado

### CA-07 — RF-10/11, RNF-02

**Dado:** Saldo 1

**Quando:** Saída 2 ou ajuste -2 e insert/update inválido direto no MongoDB de teste

**Então:** API 409 sem efeitos; validador MongoDB rejeita saldo negativo e filtro condicional impede baixa excessiva

### CA-08 — RF-13/14/15, RN-01/10/11

**Dado:** Cliente ativo, produto a 12,50, saldo 10

**Quando:** Criar com 2 unidades e confirmar

**Então:** Total 25,00 calculado no servidor; saldo 8; uma baixa; confirmada não aceita editar itens

### CA-09 — RF-14, RN-03

**Dado:** Dois produtos: saldo 10 e 0

**Quando:** Confirmar venda de uma unidade de cada

**Então:** 409; primeiro saldo continua 10, nenhum movimento, venda Rascunho

### CA-10 — RF-14, RN-04

**Dado:** Produto saldo 1 e duas vendas de 1, sessões independentes

**Quando:** Confirmar simultaneamente com barreira de teste

**Então:** Uma 200 e outra 409; saldo 0; uma única baixa comercial

### CA-11 — RF-14, RN-05

**Dado:** Venda válida e uma chave idempotente

**Quando:** Repetir em paralelo e simular perda da resposta após commit

**Então:** Mesmo status/corpo para a chave; uma baixa; chave com corpo diferente 409; nova chave após confirmar 409

### CA-12 — RF-16, RN-06/07

**Dado:** Venda confirmada de 2 com baixa registrada

**Quando:** Administrador cancela com motivo e repete a chave

**Então:** Um estorno +2, estado Cancelada e metadados completos; baixa anterior intacta; nova chave não estorna novamente

### CA-13 — RF-11/15/16, RN-15/17

**Dado:** Venda de A e usuário B vendedor; estoquista conectado

**Quando:** B lê/edita venda de A, vendedor cancela própria ou estoquista ajusta

**Então:** 404 para escopo alheio; 403 para policies negadas; nenhum efeito/auditoria de sucesso

### CA-14 — RF-15/17/18/19, RN-07/13

**Dado:** Vendas 25 e 15 confirmadas no período, segunda cancelada; produto saldo 0/mínimo 0

**Quando:** Renomear produto e abrir dashboard/relatório

**Então:** Receita 25, confirmadas 1, canceladas 1, ticket 25; produto aparece em estoque baixo; nome histórico permanece

### CA-15 — RF-12/19/20, RNF-12, RN-20

**Dado:** Eventos na borda inicial e final do intervalo, mais de uma página

**Quando:** Consultar e exportar filtro

**Então:** Início incluído/fim excluído no fuso; total geral independe de página; >366 dias ou >10 mil linhas é rejeitado, sem truncar

### CA-16 — RNF-06/08

**Dado:** Backup de banco, imagens, key ring e versão anterior conhecida

**Quando:** Restaurar em instalação isolada e aplicar upgrade testado

**Então:** Login e jornada comercial funcionam, imagens presentes, razão/saldo conciliam; tempo e RPO registrados dentro da meta

**Situação documental:** obrigação operacional histórica; não comprovada e fora do gate acadêmico vigente. Restaurar carga não é restaurar backup completo.

### CA-17 — RF-03/08/19/20, RNF-01/04/10

**Dado:** Dois usuários no BFF; payloads CSRF, imagem falsa e CSV com fórmula

**Quando:** Alternar chamadas, forjar POST, enviar SVG disfarçado e exportar texto malicioso

**Então:** Sem mistura de sessão; CSRF rejeitado; imagem 415/400; fórmula neutralizada; logs/Git/HTML/storage sem credenciais; último Admin protegido inclusive em disputa

### CA-18 — RNF-03/05/07

**Dado:** Build Release, ambiente medido e UI em 360/1280 px

**Quando:** Executar suíte, carga definida e jornada por teclado

**Então:** Zero falhas críticas; p95 registrado nas metas; formulários sem perda de ação por layout e erros associados ao campo

**Situação documental:** separar o resultado de interface/build dos p95 antigos; não se declara medição de desempenho.

### CA-ADR39 Limpeza isolada

401 anônimo,403 perfis negados,400 confirmação inválida sem efeito; sucesso preserva executor/vínculo/sessão e revoga demais. Falha de auditoria reverte exclusões. Gerar depois e repetir UUID antigo preserva a nova carga. Teste somente em fixtures isoladas, nunca no banco de apresentação.

# 3 Atores e autorização

Todos os comandos exigem sessão válida e troca obrigatória concluída, salvo as operações de conta destinadas a regularizar ou encerrar o acesso. Administrador não ignora estado, versão, saldo, integridade e proteção do último administrador. Vendedor recebe propriedade aplicada antes da contagem e paginação. Funcionário sem conta não é ator autenticado.

| Operação | Administrador | Vendedor | Estoquista |
| --- | --- | --- | --- |
| Conta própria e senha | Sim | Sim | Sim |
| Funcionários e contas | Sim | Não | Não |
| Clientes e histórico | Sim | Sim; histórico próprio | Não |
| Produtos e saldo atual | Sim | Leitura | Sim |
| Categorias, marcas e imagem | Sim | Leitura | Sim |
| Entrada e saída manual | Sim | Não | Sim |
| Ajuste de saldo | Sim | Não | Não |
| Histórico de movimentos | Sim | Não | Sim |
| Criar, editar, confirmar e descartar venda | Sim | Somente próprias | Não |
| Cancelar venda | Sim | Não | Não |
| Dashboard e relatórios comerciais | Sim | Não | Não |
| Relatórios de estoque e movimentos | Sim | Não | Sim |
| Carga, restauração e limpeza | Sim | Não | Não |

A autorização segue a orientação de negação por padrão e conferência por requisição (OWASP Foundation, s.d.a). A política concreta fica em EndpointsAcesso; filtros de propriedade ficam também nos adaptadores. Não há endpoint geral de auditoria no MVP. Ausência de menu não substitui recusa na rota direta.

# 4 Casos de uso

Os casos UC01–UC29 são identificadores desta entrega, relacionados aos RF existentes. São descrições dos objetivos dos operadores, não serviços novos. Sessão expirada exige autenticação novamente; indisponibilidade exige reconsulta segura. As variações específicas são descritas em cada caso.

## UC01 Entrar no sistema

**Atores:** Administrador, Vendedor, Estoquista. **Rastreio:** RF-01; RN-15/16; CA-01.

**Precondições:** Conta previamente criada; API e Web acessíveis.

1. Abrir /conta/entrar e informar email e senha.
2. Web valida antiforgery e envia credenciais à API por HTTPS.
3. API valida conta, senha e bloqueio e cria sessão.
4. Web cria ticket protegido e cookie de referência; direciona ao início ou à troca obrigatória.

**Alternativas e erros:** Credencial inválida ou conta indisponível: erro genérico, sem sessão. Limite de tentativas: 429. Serviço indisponível: mensagem segura. Retorno de navegação deve ser local.

**Pós-condição:** Sessão autenticada ou falha sem acesso concedido.

**Implementação:** backend/GestaoComercial.Api/Acesso/EndpointsAcesso.cs; frontend/GestaoComercial.Web/Security/ContasHttp.cs.

## UC02 Sair do sistema

**Atores:** Três perfis autenticados. **Rastreio:** RF-02; RN-16; CA-01/02.

**Precondições:** Sessão existente.

1. Acionar Sair por formulário HTTP.
2. Web verifica proteção antifalsificação e solicita revogação da sessão API.
3. Web remove ticket/cookie conforme fluxo e retorna ao acesso.

**Alternativas e erros:** Sessão expirada: novo login necessário. Falha de comunicação não deve ser interpretada como comprovação de revogação remota; o fluxo apresenta o resultado seguro implementado.

**Pós-condição:** Saída local processada; revogação remota quando concluída pela API.

**Implementação:** frontend/GestaoComercial.Web/Security/ContasHttp.cs.

## UC03 Alterar a própria senha

**Atores:** Três perfis autenticados. **Rastreio:** RF-02/03; RN-16; CA-02/17.

**Precondições:** Sessão reconhecida, inclusive com troca obrigatória pendente.

1. Informar senha atual e nova senha no formulário de conta.
2. API verifica senha e política Identity.
3. Atualizar hash e securityStamp e revogar sessões.
4. Solicitar novo acesso com a senha alterada.

**Alternativas e erros:** Senha atual incorreta ou nova senha inválida: operação recusada. Não registrar senha em logs, respostas ou documentação.

**Pós-condição:** Credencial alterada quando válida; sessões anteriores revogadas.

**Implementação:** backend/GestaoComercial.Infrastructure/Identity/MongoAutenticacao.cs.

## UC04 Manter contas de acesso

**Atores:** Administrador. **Rastreio:** RF-03; RN-15/16/18; CA-02/17.

**Precondições:** Troca de senha concluída; funcionário ativo para criação humana.

1. Consultar usuários ou selecionar funcionário para nova conta.
2. Informar email de login, senha temporária e perfil fixo.
3. API valida vínculo único e registra conta/auditoria.
4. Para editar, enviar versão, perfil e ativo; para redefinir, senha temporária e versão esperada.
5. Revogar sessões quando a alteração exigir.

**Alternativas e erros:** Email ou vínculo duplicado: conflito. Último Administrador não pode perder acesso. Versão antiga: reler. Senha temporária força troca e não é retornada.

**Pós-condição:** Conta criada/alterada, vínculo preservado e auditoria consistente.

**Implementação:** backend/GestaoComercial.Api/Acesso/EndpointsFuncionarios.cs; backend/GestaoComercial.Infrastructure/Identity/MongoAutenticacao.cs.

## UC05 Manter funcionários

**Atores:** Administrador. **Rastreio:** RF-04; RN-08/16/18; CA-03.

**Precondições:** Sessão administrativa regular.

1. Pesquisar por início do nome e situação.
2. Cadastrar nome e contatos opcionais.
3. Editar com versão atual ou alterar situação.
4. Na inativação, revogar conta e sessões vinculadas na mesma transação.

**Alternativas e erros:** Contatos inválidos: validação. Conflito de versão: reler. Proteção do último Administrador impede inativação incompatível.

**Pós-condição:** Cadastro e vínculo de acesso coerentes; autoria histórica preservada.

**Implementação:** backend/GestaoComercial.Infrastructure/Identity/MongoFuncionarios.cs.

## UC06 Manter clientes

**Atores:** Administrador, Vendedor. **Rastreio:** RF-05; RN-08/12; CA-04.

**Precondições:** Sessão e perfil permitidos.

1. Pesquisar por nome/documento ou abrir novo cliente.
2. Informar nome, PF/PJ e contatos/documento opcionais.
3. Validar documento informado e normalizações.
4. Salvar criação ou edição com versão; inativar sem excluir.

**Alternativas e erros:** Documento inválido: 400. Documento já registrado, inclusive em inativo: 409. Alteração concorrente: reler.

**Pós-condição:** Cliente disponível conforme situação; histórico anterior mantido.

**Implementação:** backend/GestaoComercial.Infrastructure/Clientes/MongoClientes.cs.

## UC07 Consultar histórico do cliente

**Atores:** Administrador, Vendedor. **Rastreio:** RF-06; RN-07/17; CA-04/13.

**Precondições:** Cliente consultável e sessão válida.

1. Abrir a ficha do cliente.
2. Solicitar histórico de vendas e aplicar período/paginação.
3. API filtra propriedade antes de contar e paginar.
4. Exibir resumos e permitir abrir venda autorizada.

**Alternativas e erros:** Sem vendas: lista vazia. Vendedor não recebe vendas de outros nem seus totais. Data inválida: validação.

**Pós-condição:** Consulta sem alteração comercial.

**Implementação:** backend/GestaoComercial.Api/Acesso/EndpointsClientes.cs.

## UC08 Manter categorias

**Atores:** Administrador, Estoquista. **Rastreio:** RF-07; RN-08/12; CA-05.

**Precondições:** Perfil com Produtos.Gerenciar.

1. Listar/pesquisar categorias ou abrir cadastro.
2. Informar nome e salvar.
3. Editar nome ou situação com versão atual.
4. Conferir unicidade normalizada e dependências antes da inativação.

**Alternativas e erros:** Nome duplicado, inclusive em inativo: conflito. Produto ativo vinculado impede inativar categoria. Nome fora de 2–80 caracteres: validação.

**Pós-condição:** Referência mantida sem excluir vínculos históricos.

**Implementação:** backend/GestaoComercial.Infrastructure/Produtos/MongoProdutos.cs.

## UC09 Manter marcas

**Atores:** Administrador, Estoquista. **Rastreio:** RF-07; RN-08/12; CA-05.

**Precondições:** Perfil com Produtos.Gerenciar.

1. Abrir manutenção de marcas.
2. Cadastrar nome normalizado único.
3. Editar com versão ou alterar situação.
4. Verificar produtos ativos vinculados antes de inativar.

**Alternativas e erros:** Nome duplicado ou dependência ativa: 409. Cadastro de marca é independente de categoria. Marca é opcional no produto.

**Pós-condição:** Marca atualizada e referências preservadas.

**Implementação:** backend/GestaoComercial.Infrastructure/Produtos/MongoProdutos.cs.

## UC10 Manter produtos

**Atores:** Administrador, Estoquista. **Rastreio:** RF-08; RN-01/08/12/19; CA-05/17.

**Precondições:** Categoria ativa; marca ativa quando informada.

1. Informar SKU, nome, descrição opcional, categoria, marca opcional, preço e mínimo.
2. API converte exatamente preço em reais para centavos.
3. Validar referências e SKU normalizado único.
4. Criar saldo incorporado zero ou editar campos cadastrais com versão, preservando saldo.

**Alternativas e erros:** Preço com fração de centavo ou fora do limite: 400. SKU duplicado/inativo vinculado/conflito de versão: recusar. Saldo não é editável pelo cadastro.

**Pós-condição:** Produto gravado e auditado; estoque alterado somente por operação própria.

**Implementação:** backend/GestaoComercial.Domain/Cadastros/DadosProduto.cs; backend/GestaoComercial.Infrastructure/Produtos/MongoProdutos.cs.

## UC11 Gerenciar imagem do produto

**Atores:** Administrador, Estoquista. **Rastreio:** RF-08; RN-19; CA-17.

**Precondições:** Produto e versão conhecidos.

1. Selecionar JPEG/PNG permitido e enviar arquivo e versão.
2. API valida conteúdo, dimensões e tamanho e converte para PNG.
3. Armazenar arquivo privado e atualizar metadados/auditoria.
4. Para remoção, solicitar exclusão do metadado/arquivo substituível com versão.

**Alternativas e erros:** Acima de 2 MiB: 413; tipo inválido: 415 ou validação de conteúdo 400. Dimensão acima de 4096: recusar. Falha de arquivo não deve criar metadado de sucesso.

**Pós-condição:** Imagem acessível por rota autenticada ou removida; produto/histórico permanecem.

**Implementação:** backend/GestaoComercial.Api/Acesso/EndpointsProdutos.cs.

## UC12 Consultar produtos

**Atores:** Administrador, Vendedor, Estoquista. **Rastreio:** RF-08; RN-08/12; CA-05.

**Precondições:** Produtos.Ler.

1. Aplicar busca por prefixo do nome ou SKU exato, situação e referências.
2. Consultar página de produtos.
3. Abrir ficha com preço, situação, saldo e imagem quando existente.

**Alternativas e erros:** Nenhum resultado: estado vazio. Falha da API: permitir repetir consulta. Leitura não concede manutenção ao Vendedor.

**Pós-condição:** Informação exibida sem alteração do produto.

**Implementação:** backend/GestaoComercial.Api/Acesso/EndpointsProdutos.cs.

## UC13 Consultar saldos

**Atores:** Administrador, Vendedor, Estoquista. **Rastreio:** RF-12; RN-02/09; CA-06/15.

**Precondições:** Estoque.Ler.

1. Abrir Estoque.
2. Filtrar nome/SKU, situação e opção saldo no mínimo ou abaixo.
3. Consultar página com saldo e mínimo em UN.

**Alternativas e erros:** Saldo atual não é reserva nem promessa de disponibilidade futura. Página vazia é resultado válido; indisponibilidade não deve aparentar saldo zero.

**Pós-condição:** Consulta sem movimento.

**Implementação:** frontend/GestaoComercial.Web/Components/Pages/Estoque.razor.

## UC14 Consultar movimentos de estoque

**Atores:** Administrador, Estoquista. **Rastreio:** RF-12; RN-07/09/20; CA-06/15.

**Precondições:** Estoque.Historico.

1. Abrir histórico e informar filtros de produto, tipo e período.
2. API aplica intervalo e paginação estável.
3. Exibir delta, saldos, motivo, ator, data e referência comercial.

**Alternativas e erros:** Intervalo inválido: 400. Movimento inexistente: 404. Não há ação de editar/apagar movimento no fluxo comercial.

**Pós-condição:** Histórico preservado.

**Implementação:** backend/GestaoComercial.Api/Acesso/EndpointsEstoque.cs.

## UC15 Registrar entrada manual

**Atores:** Administrador, Estoquista. **Rastreio:** RF-09; RN-02/05/09/14; CA-06/11.

**Precondições:** Produto ativo e saldo incorporado válido.

1. Selecionar produto e quantidade positiva de 1 a 1.000.000.
2. Informar motivo de 10–500 caracteres.
3. Enviar intenção com chave UUID.
4. Em transação, somar saldo, gravar movimento, auditoria e recibo.

**Alternativas e erros:** Overflow de saldo: recusar integralmente. Chave repetida compatível: recibo original. Mesmo UUID com dados divergentes: 409.

**Pós-condição:** Saldo acrescido uma vez e movimento de Entrada positivo.

**Implementação:** backend/GestaoComercial.Infrastructure/Estoque/MongoEstoque.cs.

## UC16 Registrar saída manual

**Atores:** Administrador, Estoquista. **Rastreio:** RF-10; RN-02/05/09/14; CA-06/07.

**Precondições:** Produto ativo com quantidade suficiente.

1. Selecionar produto, quantidade positiva e motivo.
2. Enviar chave da intenção.
3. Aplicar baixa condicional dentro da transação.
4. Registrar movimento SaidaManual negativo, auditoria e recibo.

**Alternativas e erros:** Saldo insuficiente ou ausente: conflito sem movimento. Saída manual não representa confirmação de venda e não deve duplicar sua baixa.

**Pós-condição:** Saldo reduzido sem ficar negativo; histórico conciliável.

**Implementação:** backend/GestaoComercial.Infrastructure/Estoque/MongoEstoque.cs.

## UC17 Ajustar estoque por delta

**Atores:** Administrador. **Rastreio:** RF-11; RN-02/09/14; CA-06/07/13.

**Precondições:** Produto ativo e divergência identificada.

1. Consultar saldo e determinar diferença a corrigir.
2. Informar delta assinado não zero entre -1.000.000 e 1.000.000.
3. Informar motivo e confirmar intenção.
4. Aplicar delta e gerar novo movimento de Ajuste na transação.

**Alternativas e erros:** Delta que tornaria saldo negativo ou excederia Int32: recusar. Estoquista/Vendedor: 403. Nunca reescrever movimento antigo.

**Pós-condição:** Correção explícita incorporada à razão e ao saldo.

**Implementação:** backend/GestaoComercial.Domain/Estoque/Movimentacao.cs.

## UC18 Criar e editar rascunho de venda

**Atores:** Administrador, Vendedor em vendas próprias. **Rastreio:** RF-13; RN-01/10/11/17; CA-08.

**Precondições:** Cliente selecionado; produtos incluídos devem estar ativos.

1. Abrir nova venda, selecionar cliente e adicionar produtos/quantidades.
2. Salvar rascunho com chave de criação.
3. Servidor obtém preços atuais e retorna snapshots e totais.
4. Editar enviando lista completa e versão; salvar para revisar novos preços.

**Alternativas e erros:** Produto repetido, quantidade inválida ou mais de 100 itens: 400. Rascunho vazio é permitido pela API, sem confirmação. Não há reserva. Versão antiga: reler.

**Pós-condição:** Rascunho persistido, sem baixa de estoque.

**Implementação:** backend/GestaoComercial.Infrastructure/Vendas/MongoVendas.cs; frontend/GestaoComercial.Web/Components/Pages/VendaDetalhe.razor.

## UC19 Confirmar venda

**Atores:** Administrador, Vendedor em vendas próprias. **Rastreio:** RF-14; RN-02/03/04/05/10/11; CA-08/09/10/11.

**Precondições:** Rascunho revisado com cliente ativo e ao menos um item.

1. Revisar cupom e acionar Concluir venda.
2. Enviar versão esperada e chave preservada por intenção.
3. Revalidar sessão, propriedade, estado, referências e preço.
4. Baixar todos os saldos e gravar movimentos, snapshots, confirmação, auditoria e recibo em uma transação.
5. Exibir resultado e reconsultar o estado quando necessário.

**Alternativas e erros:** PRECO_ALTERADO exige salvar/revisar explicitamente. ESTOQUE_INSUFICIENTE reverte tudo. Resultado desconhecido exige repetir mesma intenção, sem nova chave. Venda alheia: 404.

**Pós-condição:** Venda Confirmada e uma baixa por item, ou nenhuma alteração parcial.

**Implementação:** backend/GestaoComercial.Infrastructure/Vendas/MongoVendas.cs; backend/GestaoComercial.Infrastructure/Persistence/OperacoesComerciais.cs.

## UC20 Consultar venda

**Atores:** Administrador, Vendedor em vendas próprias. **Rastreio:** RF-15; RN-07/17/20; CA-08/13/14.

**Precondições:** Vendas.Ler e propriedade permitida.

1. Filtrar lista por cliente, estado e período de criação.
2. Abrir detalhe.
3. Exibir itens, valores históricos e eventos do ciclo.
4. Usar Atualizar venda para obter o estado atual.

**Alternativas e erros:** UUID fora do escopo do Vendedor: 404. Não inferir estado atual exclusivamente de recibo antigo. Consulta histórica conserva snapshots.

**Pós-condição:** Leitura sem alteração.

**Implementação:** backend/GestaoComercial.Api/Acesso/EndpointsVendas.cs.

## UC21 Descartar rascunho

**Atores:** Administrador, Vendedor em vendas próprias. **Rastreio:** RF-13; RN-07/10/17; CA-08.

**Precondições:** Venda Rascunho e versão atual.

1. Solicitar descarte e informar motivo.
2. Confirmar comando com versão e chave.
3. Registrar evento de descarte, estado, auditoria e recibo.

**Alternativas e erros:** Venda já confirmada ou encerrada: 409. Motivo fora de 10–500: validação. Descarte não cancela venda confirmada.

**Pós-condição:** Venda Descartada preservada, sem movimento de estoque.

**Implementação:** backend/GestaoComercial.Infrastructure/Vendas/MongoVendas.cs.

## UC22 Cancelar venda confirmada

**Atores:** Administrador. **Rastreio:** RF-16; RN-05/06/07/10; CA-12/13.

**Precondições:** Venda Confirmada; versão conhecida.

1. Abrir a área Cancelar venda no detalhe.
2. Informar motivo e confirmar a ação no formulário.
3. Enviar versão esperada e chave da intenção.
4. Estornar integralmente quantidades e registrar evento, auditoria e recibo na mesma transação.

**Alternativas e erros:** Produto inativo não bloqueia estorno. Capacidade excedida ou falha de auditoria: rollback integral. Repetição compatível não duplica. Nova chave em Cancelada: estado inválido.

**Pós-condição:** Venda Cancelada, baixa original mantida e um EstornoVenda por item.

**Implementação:** backend/GestaoComercial.Infrastructure/Vendas/MongoVendas.cs.

## UC23 Consultar dashboard

**Atores:** Administrador. **Rastreio:** RF-17/18; RN-13/20; CA-14/15.

**Precondições:** Dashboard.Ler e persistência inicializada.

1. Abrir dashboard e selecionar período; ausência usa mês corrente.
2. Converter datas civis no fuso configurado.
3. Consultar indicadores, evolução, ranking, vendas recentes e estoque baixo.
4. Exibir receita e ticket segundo fórmulas oficiais.

**Alternativas e erros:** Cancelada contribui zero na receita do período de confirmação. Estoque baixo exige ativo e saldo <= mínimo. Instalação incompleta retorna indisponibilidade, sem zeros aparentes.

**Pós-condição:** Visão atual do conjunto, sem fechamento contábil.

**Implementação:** backend/GestaoComercial.Infrastructure/Dashboard/MongoDashboard.cs.

## UC24 Consultar relatórios comerciais

**Atores:** Administrador. **Rastreio:** RF-19, incremento ADR-36; RN-13/20; CA-14/15.

**Precondições:** Relatorios.Vendas.

1. Selecionar vendas, operadores ou produtos.
2. Informar início/fim e filtros compatíveis.
3. Consultar linhas paginadas e totais do filtro inteiro.
4. Usar snapshots e contribuição líquida conforme situação atual.

**Alternativas e erros:** Intervalo acima de 366 dias ou filtro desconhecido: 400. Início inclusivo e fim exclusivo. Vendedor e Estoquista não recebem esses relatórios.

**Pós-condição:** Consulta comercial rastreável aos filtros e ao instante de geração.

**Implementação:** backend/GestaoComercial.Api/Acesso/EndpointsRelatorios.cs.

## UC25 Consultar relatórios de estoque e movimentos

**Atores:** Administrador, Estoquista. **Rastreio:** RF-20; RN-09/19/20; CA-15/17.

**Precondições:** Policy específica Relatorios.Estoque ou Relatorios.Movimentos.

1. Selecionar saldo atual ou movimentações.
2. Aplicar filtros próprios do tipo escolhido.
3. Consultar páginas e totais gerais.
4. Conferir geradoEm e período/fuso quando aplicável.

**Alternativas e erros:** Estoque não é retroativo; movimentos usam ocorridoEm. Sem resultados: página vazia. Datas inválidas ou ordenação desconhecida: validação.

**Pós-condição:** Leitura sem alteração de saldo.

**Implementação:** backend/GestaoComercial.Infrastructure/Relatorios/MongoRelatorios.cs.

## UC26 Exportar relatório em CSV

**Atores:** Administrador; Estoquista somente estoque/movimentos. **Rastreio:** RF-19/20; RN-19; CA-15/17.

**Precondições:** Filtros válidos e policy do relatório.

1. Solicitar exportação com os filtros da consulta.
2. API reaplica autorização e limite de 10.000 linhas.
3. Gerar CSV UTF-8 com separador ponto e vírgula e metadados.
4. Entregar download autenticado pelo Web.

**Alternativas e erros:** Acima do limite: EXPORTACAO_EXCEDE_LIMITE sem truncamento. Falha retorna ProblemDetails. Campos textuais são neutralizados contra interpretação de fórmula.

**Pós-condição:** Arquivo obtido, sem mudança comercial.

**Implementação:** backend/GestaoComercial.Api/Acesso/EndpointsRelatorios.cs; frontend/GestaoComercial.Web/Security/DownloadsRelatorios.cs.

## UC27 Gerar ou retomar carga demonstrativa

**Atores:** Administrador. **Rastreio:** M09; ADR-37; RF comerciais reutilizados.

**Precondições:** Banco existente inicializado; página administrativa acessível.

1. Consultar estado e última conclusão.
2. Acionar geração na página Dados de Demonstração.
3. Executar etapas identificadas usando serviços existentes e dados fictícios.
4. Retomar etapas concluídas sem duplicar e registrar conclusão.

**Alternativas e erros:** Outra operação em andamento: 409. Interrupção permite retomar. Carga não equivale a limpar; mantém dados manuais. Senhas aleatórias não são retornadas.

**Pós-condição:** Cenário demonstrativo gerado ou parcialmente retomável com proveniência.

**Implementação:** backend/GestaoComercial.Infrastructure/DadosDemonstracao.cs.

## UC28 Restaurar carga demonstrativa

**Atores:** Administrador. **Rastreio:** M09; ADR-37; preservação manual.

**Precondições:** Carga identificada; confirmação explícita de restauração.

1. Solicitar restauração confirmada.
2. Verificar proveniência, alterações e dependências de registros demonstrativos.
3. Remover somente o conjunto demonstrativo comprovado quando seguro.
4. Preservar auditoria e permitir geração posterior separada.

**Alternativas e erros:** Dependência ou alteração manual impede remoção destrutiva e gera conflito integral. Sem confirmação: recusar. Não é restauração de backup completo.

**Pós-condição:** Carga demonstrativa restaurada sem apagar dados manuais, ou operação recusada.

**Implementação:** backend/GestaoComercial.Infrastructure/DadosDemonstracao.cs.

## UC29 Limpar dados da demonstração

**Atores:** Administrador executor. **Rastreio:** RF-ADR39; RN-ADR39; CA-ADR39; ADR-39.

**Precondições:** Troca concluída; confirmação textual APAGAR DADOS; UUID não vazio.

1. Exibir área destrutiva e exigir confirmação explícita.
2. Revalidar administrador e coordenar exclusão com operações concorrentes.
3. Em transação, apagar dados comerciais/histórico/manual e demais contas conforme ADR-39.
4. Preservar executor, funcionário vinculado, sessão atual, estrutura técnica e recibos DemoLimpa.
5. Registrar UUID concluído e manter nova geração como ação separada.

**Alternativas e erros:** Confirmação inválida: 400 sem efeito. Perfis negados: 403. Falha na auditoria: rollback. Repetição de UUID concluído preserva dados criados depois da limpeza.

**Pós-condição:** Limpeza delimitada concluída uma vez; demais acessos revogados. Arquivos de imagens não são apagados por essa operação de banco.

**Implementação:** backend/GestaoComercial.Infrastructure/DadosDemonstracao.cs.

# 5 Rastreabilidade de implementação e testes

As relações foram organizadas a partir dos arquivos desta versão. A coluna teste aponta código de teste existente, sem atribuir aprovação atual. Os detalhes Dado/Quando/Então estão no capítulo 2. O inventário de métodos no apêndice permite localizar as asserções e suas linhas.

| Requisito / casos | Implementação resumida | Teste IntegrationTests |
| --- | --- | --- |
| RF-01 / UC01 | Acesso/EndpointsAcesso; Identity/MongoAutenticacao | AuthenticationTests.cs |
| RF-02 / UC02/03 | Identity/MongoAutenticacao; Security/ContasHttp | AuthenticationTests.cs |
| RF-03 / UC04 | EndpointsFuncionarios; Identity/MongoFuncionarios | AdministrationTests.cs |
| RF-04 / UC05 | Identity/MongoFuncionarios | EmployeeTests.cs |
| RF-05 / UC06 | Clientes/MongoClientes | CustomerTests.cs |
| RF-06 / UC07 | Clientes/MongoClientes; Vendas/MongoVendas | SalesTests.cs |
| RF-07 / UC08/09 | Produtos/MongoProdutos | ProductTests.cs |
| RF-08 / UC10/11/12 | Produtos/MongoProdutos; DadosProduto | ProductTests.cs |
| RF-09 / UC15 | Estoque/MongoEstoque; Movimentacao | StockTests.cs |
| RF-10 / UC16 | Estoque/MongoEstoque; Movimentacao | StockTests.cs |
| RF-11 / UC17 | Estoque/MongoEstoque; Movimentacao | StockTests.cs |
| RF-12 / UC13/14 | Estoque/MongoEstoque | StockQueryTests.cs |
| RF-13 / UC18/21 | Vendas/MongoVendas; RegrasVenda | SalesTests.cs |
| RF-14 / UC19 | Vendas/MongoVendas; OperacoesComerciais | SalesTests.cs |
| RF-15 / UC20 | Vendas/MongoVendas | SalesTests.cs |
| RF-16 / UC22 | Vendas/MongoVendas; MongoEstoque | SalesTests.cs |
| RF-17 / UC23 | Dashboard/MongoDashboard; Indicadores | DashboardTests.cs |
| RF-18 / UC23 | Dashboard/MongoDashboard | DashboardTests.cs |
| RF-19 / UC24/26 | Relatorios/MongoRelatorios; EndpointsRelatorios | ReportsTests.cs |
| RF-20 / UC25/26 | Relatorios/MongoRelatorios | ReportsTests.cs |
| RF-ADR39 / UC29 | Infrastructure/DadosDemonstracao | DemonstrationCleanupTests.cs |
| M09 / ADR-37 / UC27/28 | Infrastructure/DadosDemonstracao | DemonstrationTests.cs |


# 6 Arquitetura e componentes

Domain não referencia ASP.NET Core, BSON, MongoDB.Driver ou relógio global. Application referencia Domain, define portas específicas e usa entradas/saídas sem BSON. Infrastructure referencia Application/Domain e implementa persistência e Identity. Api compõe as dependências, valida transporte e converte contratos. Web referencia Contracts entre os projetos de produção, além de bibliotecas externas para UI e armazenamento técnico.

O uso de camadas e abstrações encontra respaldo na orientação de arquitetura .NET (Microsoft, s.d.a). Neste código, não se deve inferir uma implementação completa de agregados ricos ou de todos os padrões de Clean Architecture: regras estáticas e adaptadores coordenadores são decisões observáveis. Não há CQRS/MediatR, repositório genérico, microsserviços, mensageria ou Event Sourcing.

| Projeto | Responsabilidade | Exemplos reais |
| --- | --- | --- |
| Domain | Invariantes e normalização | RegrasVenda, Movimentacao, DadosProduto, DocumentoCliente, Perfis |
| Application | Portas e resultados de casos de uso | IVendas, IEstoque, IClientes, IProdutos, IAutenticacao, IDashboard, IRelatorios |
| Infrastructure | Driver, BSON, Identity, arquivo e transações | MongoVendas, MongoEstoque, MongoAutenticacao, OperacoesComerciais |
| Api | Rotas, policies, conversão, composição | EndpointsVendas, EndpointsRelatorios, EndpointsAcesso |
| Web | Componentes, clientes HTTP, BFF e sessão | VendaDetalhe, SessaoCircuito, MongoTickets, ContasHttp |
| Contracts | DTOs HTTP sem regra/persistência | CriarVendaRequest, VendaResponse, ProdutoResponse |

## 6.1 Fluxo e ciclo de vida

Uma operação de interface chama cliente HTTP tipado no servidor Web. O token é obtido do ticket protegido e usado na chamada API, sem ser renderizado. A API autentica sessão, verifica policy e fornece ator do servidor. A implementação específica aplica regras, persistência e mapeamento; a API retorna DTO ou erro seguro.

IMongoClient é singleton por configuração/credencial e reutiliza conexões. Sessões são curtas por operação. Não se executa Task.WhenAll dentro da mesma sessão, nem se mantém sessão no circuito Blazor. Arquivos e chamadas HTTP não participam da transação MongoDB. Cancelamento da requisição depois de commit não desfaz efeito comercial.

## 6.2 Versões identificadas

| Componente | Versão declarada |
| --- | --- |
| SDK .NET | 10.0.401 |
| SkiaSharp | 3.119.4 |
| SkiaSharp.NativeAssets.Linux.NoDependencies | 3.119.4 |
| MongoDB.Driver | 3.11.2 |
| Testcontainers.MongoDb | 4.14.0 |
| Microsoft.AspNetCore.OpenApi | 10.0.12 |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.12 |
| Microsoft.NET.Test.Sdk | 17.14.1 |
| xunit | 2.9.3 |
| xunit.runner.visualstudio | 3.1.1 |


As versões acima são declaradas em global.json e Directory.Packages.props. O servidor Atlas não foi consultado nesta tarefa. A configuração Testcontainers/CI não prova a versão corrente do servidor de apresentação. Configuração de infraestrutura futura e histórico em deploy não autorizam publicação.

# 7 Persistência e dicionário de dados

IDs de negócio são UUID Standard em BSON binData; quantidades são Int32, centavos e versões são Int64 e instantes são BSON Date em UTC. HTTP usa UUID textual e ISO 8601. Nomes persistidos são camelCase, com employeeId mapeado para funcionarioId no contrato. Campos anuláveis são explícitos no dicionário; anulação não significa ausência livre de qualquer campo exigido pelo validator.

Produto incorpora saldo e imagem opcional; venda incorpora até cem itens e snapshots. O saldo não constitui coleção separada. Movimentos, recibos, sessões e auditoria são coleções independentes. Os critérios de incorporação/referência e de valores temporais encontram referência na documentação do MongoDB (MongoDB, s.d.c); limites concretos pertencem ao projeto.

### Coleção employees

Fonte de definição: Funcionarios/FuncionariosSchema.cs.

| Campo | Tipo BSON e limite | Finalidade |
| --- | --- | --- |
| _id | binData UUID | Identificador Standard |
| schemaVersion | Int32 = 1 | Formato do documento |
| nome / nomeNormalizado | string 2–150 | Exibição e chave de busca |
| ativo | bool | Inativação lógica |
| versao | Int64 >= 1 | Concorrência de cadastro |
| guardVersion | Int64 >= 0 | Coordenação de referências |
| createdAt / updatedAt | Date UTC | Criação e alteração |
| email | string 3–254 ou null | Contato opcional |
| telefone | string 10–15 dígitos ou null | Contato opcional |


### Coleção users

Fonte de definição: Identity/AuthSchema.cs.

| Campo | Tipo BSON e limite | Finalidade |
| --- | --- | --- |
| _id | binData UUID | Identificador Standard |
| schemaVersion | Int32 = 1 | Formato do documento |
| nome / nomeNormalizado | string 2–150 | Exibição e chave de busca |
| ativo | bool | Inativação lógica |
| versao | Int64 >= 1 | Concorrência de cadastro |
| guardVersion | Int64 >= 0 | Coordenação de referências |
| createdAt / updatedAt | Date UTC | Criação e alteração |
| email / emailNormalizado | string 3–254 | Login e unicidade |
| passwordHash | string 1–1024 | Hash Identity, nunca retornar |
| perfil | string enum | Administrador, Vendedor ou Estoquista |
| deveAlterarSenha | bool | Troca obrigatória |
| securityStamp | string 32–64 | Revogação |
| failedAccessCount | Int32 0–5 | Falhas de login |
| lockoutEnd | Date UTC ou null | Bloqueio |
| employeeId | UUID ou null | Vínculo único; bootstrap pode ser null |


### Coleção customers

Fonte de definição: Clientes/ClientesSchema.cs.

| Campo | Tipo BSON e limite | Finalidade |
| --- | --- | --- |
| _id | binData UUID | Identificador Standard |
| schemaVersion | Int32 = 1 | Formato do documento |
| nome / nomeNormalizado | string 2–150 | Exibição e chave de busca |
| ativo | bool | Inativação lógica |
| versao | Int64 >= 1 | Concorrência de cadastro |
| guardVersion | Int64 >= 0 | Coordenação de referências |
| createdAt / updatedAt | Date UTC | Criação e alteração |
| tipo | string PF/PJ | Tipo de pessoa |
| documento | string ou null | CPF11/CNPJ14 válido e normalizado |
| email | string ou null | Contato |
| telefone | string ou null | 10–15 dígitos |


### Coleção categories

Fonte de definição: Produtos/ProdutosSchema.cs. Nomes normalizados únicos por coleção, inclusive inativos.

| Campo | Tipo BSON e limite | Finalidade |
| --- | --- | --- |
| _id | binData UUID | Identificador Standard |
| schemaVersion | Int32 = 1 | Formato do documento |
| nome / nomeNormalizado | string 2–80 | Exibição e chave de busca |
| ativo | bool | Inativação lógica |
| versao | Int64 >= 1 | Concorrência de cadastro |
| guardVersion | Int64 >= 0 | Coordenação de referências |
| createdAt / updatedAt | Date UTC | Criação e alteração |


### Coleção brands

Fonte de definição: Produtos/ProdutosSchema.cs. Nomes normalizados únicos por coleção, inclusive inativos.

| Campo | Tipo BSON e limite | Finalidade |
| --- | --- | --- |
| _id | binData UUID | Identificador Standard |
| schemaVersion | Int32 = 1 | Formato do documento |
| nome / nomeNormalizado | string 2–80 | Exibição e chave de busca |
| ativo | bool | Inativação lógica |
| versao | Int64 >= 1 | Concorrência de cadastro |
| guardVersion | Int64 >= 0 | Coordenação de referências |
| createdAt / updatedAt | Date UTC | Criação e alteração |


### Coleção products

Fonte de definição: Produtos/ProdutosSchema.cs.

| Campo | Tipo BSON e limite | Finalidade |
| --- | --- | --- |
| _id | binData UUID | Identificador Standard |
| schemaVersion | Int32 = 1 | Formato do documento |
| nome / nomeNormalizado | string 2–150 | Exibição e chave de busca |
| ativo | bool | Inativação lógica |
| versao | Int64 >= 1 | Concorrência de cadastro |
| guardVersion | Int64 >= 0 | Coordenação de referências |
| createdAt / updatedAt | Date UTC | Criação e alteração |
| sku / skuNormalizado | string 1–40 | SKU normalizado único; padrão A–Z/0–9/._- |
| descricao | string até 2000 ou null | Sem HTML |
| categoriaId | UUID | Categoria obrigatória |
| marcaId | UUID ou null | Marca opcional |
| precoCentavos | Int64 1–999.999.999 | Preço BRL |
| moeda | string BRL | Moeda fixa |
| estoqueMinimo | Int32 0–1.000.000 | Referência de reposição |
| saldo.quantidade | Int32 >= 0 | Saldo incorporado obrigatório |
| saldo.versao | Int64 >= 1 | Versão do saldo |
| imagem | object ou null | Metadados de arquivo privado |
| imagem.chave | string | 32 caracteres hexadecimais e .png |
| imagem.mime | string image/png | Arquivo convertido |
| imagem.tamanhoBytes | Int32 1–2.097.152 | Tamanho do PNG |


### Coleção sales

Fonte de definição: Vendas/VendasSchema.cs. Confirmada/Cancelada exigem itens e confirmação; Cancelada também estorno/evento. Não contém ativo nem nomeNormalizado.

| Campo | Tipo BSON e limite | Finalidade |
| --- | --- | --- |
| _id / clienteId / criadorId | UUID | Venda e referências |
| schemaVersion | Int32 = 1 | Formato |
| criadorNomeSnapshot | string 2–150 | Nome do criador na época |
| status | enum string | Rascunho, Confirmada, Cancelada, Descartada |
| itens | array até 100 | Incorporado; produto e ID distintos |
| itens.id / itens.produtoId | UUID | Item estável e produto |
| itens.skuSnapshot / nomeSnapshot | string 1–40 / 2–150 | Cópia histórica |
| itens.quantidade | Int32 1–1.000.000 | Quantidade UN |
| itens.precoUnitarioCentavos | Int64 1–999.999.999 | Preço copiado |
| itens.totalCentavos | Int64 positivo limitado | Quantidade × preço |
| totalCentavos | Int64 0–99.999.999.999.999 | Soma exata de itens |
| moeda | string BRL | Moeda fixa |
| versao / guardVersion | Int64 >= 1 / >= 0 | Edição e coordenação |
| createdAt / updatedAt / criadoEm | Date UTC | createdAt igual criadoEm |
| confirmadoEm / canceladoEm / descartadoEm | Date UTC ou null | Conforme estado |
| clienteSnapshot | object ou null | nome e documento nullable; confirmação |
| confirmacao / cancelamento / descarte | object ou null | atorId, nomeSnapshot e motivo nullable |
| motivo nos eventos | string 10–500 ou null | Null na confirmação; obrigatório no encerramento |


### Coleção stock_movements

Fonte de definição: Estoque/EstoqueSchema.cs e Vendas/VendasSchema.cs. Imutável no fluxo normal. A definição é expandida pela evolução06 para incluir vínculos comerciais.

| Campo | Tipo BSON e limite | Finalidade |
| --- | --- | --- |
| _id / produtoId / atorId | UUID | Identidade e referências |
| schemaVersion | Int32 = 1 | Formato |
| tipo | string enum | Entrada, SaidaManual, Ajuste, Venda, EstornoVenda |
| delta | Int32 não zero ±1.000.000 | Sinal conforme tipo |
| saldoAntes / saldoDepois | Int32 >= 0 | Depois = antes + delta |
| motivo | string 10–500 | Justificativa |
| atorNomeSnapshot | string 2–150 | Autoria histórica |
| ocorridoEm | Date UTC | Data do movimento |
| operacaoId | UUID no manual; null/ausente comercial | Identifica intenção manual |
| vendaId / itemId | UUID no comercial; null/ausente manual | Baixa/estorno por item |


### Coleção idempotency_receipts

Fonte de definição: Estoque/EstoqueSchema.cs e Vendas/VendasSchema.cs. Unique ator/escopo/chave; sem TTL comercial. Schema físico exige campos, inclusive durante preparação.

| Campo | Tipo BSON e limite | Finalidade |
| --- | --- | --- |
| _id / atorId / chave / recursoId | UUID | Recibo, ator, intenção e recurso |
| schemaVersion | Int32 | Formato |
| escopo | string 1–100 | Identificador interno do comando |
| hashRequest | binData 32 bytes | Hash da intenção normalizada |
| estado | string enum | Preparando ou Concluido |
| statusHttp | Int32 enum 200/201 | Sucesso do comando |
| resposta | object | Vazio na preparação invisível; documento do resultado na conclusão |
| criadoEm | Date UTC | Registro da intenção |


### Coleção audit_events

Fonte de definição: Identity/AuthSchema.cs. Schema atual não define um payload genérico detalhes. Proveniência da carga e recibos DemoLimpa usam campos existentes.

| Campo | Tipo BSON e limite | Finalidade |
| --- | --- | --- |
| _id | UUID | Evento |
| schemaVersion | enum 1 | Formato |
| atorId / recursoId | UUID ou null | Anonimato/recurso conforme evento |
| origem | string enum | Autenticado, Anonimo ou Bootstrap |
| traceId | string 1–150 | Correlação e proveniência demonstrativa |
| acao | string 1–50 | Ação registrada |
| recursoTipo | string 1–30 | Tipo de recurso |
| ocorridoEm | Date UTC | Instante |


### Coleção api_sessions

Fonte de definição: Identity/AuthSchema.cs. Expiração verificada em código; índice expiraEm não é declaração de TTL configurado.

| Campo | Tipo BSON e limite | Finalidade |
| --- | --- | --- |
| _id / usuarioId | UUID | Sessão e conta |
| schemaVersion | enum 1 | Formato |
| tokenHash | binData | SHA-256 do bearer, não token bruto |
| securityStamp | string 32–64 | Stamp da conta |
| criadoEm / expiraEm / ultimoUsoEm | Date UTC | Vida da sessão |
| revogadaEm | Date UTC ou null | Revogação |


### Coleção administration_control

Fonte de definição: Identity/AuthSchema.cs. Exceção à convenção genérica: schema físico não possui schemaVersion.

| Campo | Tipo BSON e limite | Finalidade |
| --- | --- | --- |
| _id | string administration | Documento técnico fixo |
| guardVersion | Int64 >= 0 | Escrita de coordenação administrativa |


### Coleção tickets

Fonte de definição: frontend/GestaoComercial.Web/Security/MongoTickets.cs. Mesmo banco, finalidade técnica Web. Sem conteúdo comercial ou token bruto.

| Campo | Tipo BSON e limite | Finalidade |
| --- | --- | --- |
| _id | UUID | Ticket técnico |
| schemaVersion | enum 1 | Formato |
| handleHash | binData | Hash SHA-256 da referência |
| ticketProtegido | binData | Ticket Data Protection |
| tokenApiProtegido | binData | Token protegido |
| expiraEm / createdAt | Date UTC | Validade e criação |


### Coleção persistence_versions

Fonte de definição: Persistence/MongoFoundation.cs.

| Campo | Tipo BSON e limite | Finalidade |
| --- | --- | --- |
| _id | string | ID da evolução |
| schemaVersion | Int32 = 1 | Formato do registro |
| version | Int32 | Posição aplicada |
| checksum | string | Conferência da definição |
| appliedAt | Date UTC | Aplicação explícita |


## 7.1 Índices e validação

| Coleção | Índice | Chaves e observação |
| --- | --- | --- |
| employees | ix_employees_nome_id | nomeNormalizado, _id |
| users | ux_users_email / ix_users_nome_id | emailNormalizado único; nomeNormalizado,_id |
| users | ux_users_employee | employeeId único parcial binData |
| customers | ux_customers_documento / ix_customers_nome_id | documento único parcial string; nomeNormalizado,_id |
| categories / brands | ux_*_nome / ix_*_nome_id | nomeNormalizado único; nomeNormalizado,_id |
| products | ux_products_sku / ix_products_nome_id | skuNormalizado único; nomeNormalizado,_id |
| products | ix_products_categoria_ativo_id / ix_products_marca_ativo_id | referência, ativo, _id |
| sales | ix_sales_criado_id | criadoEm desc, _id desc |
| sales | ix_sales_cliente_criado_id / ix_sales_criador_criado_id | cliente/criador, criadoEm desc, _id desc |
| sales | ix_sales_confirmado_status_id | confirmadoEm, status, _id |
| stock_movements | ix_stock_data_id / ix_stock_produto_data_id | ocorridoEm desc,_id desc; produtoId + data/id |
| stock_movements | ux_stock_operacao | operacaoId único parcial UUID |
| stock_movements | ux_stock_venda_item_tipo | vendaId,itemId,tipo único parcial UUID |
| idempotency_receipts | ux_receipts_ator_escopo_chave / ix_receipts_data | atorId,escopo,chave único; criadoEm |
| api_sessions | ux_sessions_token / ix_sessions_usuario_expira / ix_sessions_expira | tokenHash único; usuarioId,expiraEm; expiraEm |
| audit_events | ix_audit_recurso_data_id | recursoTipo,recursoId,ocorridoEm,_id |
| tickets | ux_tickets_handle / ix_tickets_expira | handleHash único; expiraEm |
| persistence_versions | ux_persistence_version | version único; _id possui unicidade nativa |


Os índices únicos parciais de documento, vínculo e movimentos não abrangem valores null da mesma maneira que os valores tipados. Não supor que unique em array impede produto repetido no mesmo documento: VendasSchema também verifica distinct e totais com $expr. As definições strict/error restringem tipos, campos e relações internas. A validação de esquema é um recurso do servidor (MongoDB, s.d.d); ela não valida automaticamente todas as referências entre coleções.

## 7.2 Evoluções explícitas

O catálogo persistence_versions registra ID, schemaVersion, versão, checksum e appliedAt. As evoluções principais são 00-foundation, 01-authentication, 02-employees, 03-customers, 04-products, 05-stock e 06-sales; tickets possui 01-web-tickets com catálogo compartilhado atualizado para posição8. A aplicação verifica readiness, mas não executa mutação de schema silenciosamente no startup normal.

Criação de coleção, índice ou validator não é apresentada como uma migração de dados automaticamente atômica. Os inicializadores verificam definições existentes e recusam divergências. Nenhum inicializador foi executado nesta tarefa. O manual orienta a instalação já preparada e remete procedimentos de primeira instalação ao responsável.

# 8 Protocolos comerciais

## 8.1 Dinheiro e limites

Valores internos usam centavos long, BRL. Preço permitido: 1 a 999.999.999 centavos. Total de venda/item limitado a 99.999.999.999.999 centavos; quantidade por item/manual até 1.000.000; até cem produtos distintos por venda. Estoque final deve ficar entre zero e 2.147.483.647. API recebe decimal em reais e rejeita fração de centavo; nunca recebe preço/total do item como autoridade no comando de venda.

Multiplicações e somas usam checked, que sinaliza overflow inteiro (Microsoft, s.d.c). Os limites comerciais são conferidos adicionalmente. Média usa arredondamento AwayFromZero só no centavo final. Indicadores agregam valores exatos e validam limites; uso transitório de Decimal128 em agregação não significa persistência monetária Decimal128 ou uso de double no domínio.

## 8.2 Confirmação e cancelamento

Confirmar exige propriedade, estado Rascunho, versão, cliente/produtos ativos, itens válidos e preços revisados. Guardas efetivas coordenam referências; cada baixa é update condicional com resultado conferido. Venda, movimentos, auditoria e recibo são gravados na mesma sessão/transação. Falha em qualquer item impede efeito parcial. Rascunho não reserva.

Cancelar exige Administrador e Confirmada, motivo de 10–500 e versão. O estorno integral é vinculado à venda/item, admite cadastro posteriormente inativo e não altera baixa original. Capacidade de saldo excedida provoca rollback integral. Descartar é operação de Rascunho, sem movimento, distinta de cancelar.

O manual MongoDB descreve atomicidade multidocumento e dependência dos concerns (MongoDB, s.d.a). A aplicação usa ReadConcern.Snapshot, WriteConcern.WMajority e ReadPreference.Primary. Leitura snapshot sozinha não protege dependência de ativo/preço; o código incrementa guardVersion e confere ModifiedCount. O driver usa sessão associada ao cliente (MongoDB, s.d.b).

## 8.3 Idempotência e falhas

No transporte, Idempotency-Key é UUID. Na persistência comercial, o escopo é um identificador interno como vendas/confirmacao/v1; o hash inclui ID da venda, versão esperada e motivo normalizado conforme comando. A descrição histórica de escopo como método+rota expressa a intenção conceitual, mas não é o texto físico gravado pelo código atual.

O índice de ator/escopo/chave reserva a intenção na transação. Estado Preparando não fica visível antes do commit. Mesmo hash retorna resposta original; hash divergente retorna CHAVE_REUTILIZADA. DuplicateKey aborta a tentativa e a consulta do recibo ocorre fora da transação abortada. Recibos comerciais não têm TTL automático.

OperacoesComerciais aplica orçamento de cinco segundos à operação e tentativas limitadas para falha transitória. UnknownTransactionCommitResult repete somente commit; dúvida persistente produz RESULTADO_DESCONHECIDO. O cliente deve conservar a intenção e reconsultar. O estado atual é obtido por GET: recibo de confirmação antigo pode existir depois do cancelamento.

## 8.4 Invariantes a verificar

| Invariante | Mecanismos complementares | Falha esperada |
| --- | --- | --- |
| Saldo não negativo | Cálculo, filtro condicional e validator | 409 e rollback |
| Nenhuma venda parcial | Mesma sessão para todos os efeitos | Rascunho e saldos anteriores preservados |
| Um efeito por intenção | Recibo, hash e índice único | Mesmo recibo ou conflito |
| Um estorno por item | Estado e índice venda/item/tipo | Sem novo EstornoVenda |
| Referências coerentes | Verificação e escrita de guarda | Conflito/reavaliação |
| Histórico preservado | Snapshots, rotas e ausência de edição comercial | Consulta conserva valores originais |

As proteções operacionais dependem das credenciais efetivamente utilizadas. Os scripts de privilégio mínimo não provam o RBAC vigente no Atlas, que não foi consultado. A ADR-39 introduz exclusão administrativa de histórico; portanto não se deve afirmar que toda credencial runtime é tecnicamente incapaz de excluir em qualquer instalação.

# 9 Contratos HTTP

Base API /api/v1. JSON camelCase, comandos rejeitam propriedades desconhecidas conforme configuração do host, resposta de erro usa ProblemDetails com code e traceId. Bearer opaco é comunicação servidor-servidor; cookie Web não autentica a API. Não publicar token, hash de senha ou conteúdo protegido como exemplo.

Os transportes privados dos hosts também fazem parte do contrato HTTP: LoginInput recebe email e senha; PasswordInput recebe senhaAtual e novaSenha; ResetInput recebe senhaTemporaria e versaoEsperada; CriarContaInput recebe funcionarioId, email, senhaTemporaria e perfil. As senhas são strings sigilosas; funcionarioId é UUID e versaoEsperada é inteiro longo. A resposta interna de login contém accessToken, expiraEm e usuario, mas não integra o contrato compartilhado nem é renderizada ao navegador. A remoção de campos sigilosos de Contracts não significa ausência desses dados na requisição interna de autenticação.

## 9.1 Paginação e filtros

Padrão page=1 e pageSize=20, máximo100. Campos de busca e limites específicos são validados no adaptador; não presumir um único maxlength universal. Cadastros combinam busca/situação, produtos incluem categoria/marca, vendas usam cliente/estado/data de criação e movimentos usam produto/tipo/ocorridoEm. Usuários usam bool? ativo, diferente do filtro textual true/false/todos em outros cadastros. Ordenações são definidas pelo servidor com UUID de desempate.

Relatórios de vendas/operadores/produtos usam início/fim e operadorId quando permitido; movimentos usam produtoId/tipo; estoque usa busca/situação. Filtros desconhecidos, repetidos ou incompatíveis são recusados. Intervalo comercial usa início inclusivo e fim exclusivo no fuso configurado, padrão America/Cuiaba, máximo366 dias. Exportação não usa paginação e limita dez mil linhas.

## 9.2 Catálogo de rotas API

Caminhos relativos a /api/v1; {id} exige UUID. Chave indica Idempotency-Key; limpeza usa OperacaoId no corpo. POST/PUT de cadastros não recebem chave comercial.

| Método e caminho | Entrada e sucesso | Policy |
| --- | --- | --- |
| POST /auth/login | LoginInput; 200 token interno e usuário | Anônimo; rate limit |
| GET /auth/me | 200 SessaoResponse | Conta |
| POST /auth/logout | 204 | Conta |
| POST /auth/alterar-senha | PasswordInput;204 | Conta |
| GET /usuarios | 200 UsuariosResponse; ativo bool? | Usuarios.Gerenciar |
| GET /usuarios/{id} | 200 UsuarioResponse | Usuarios.Gerenciar |
| POST /usuarios | CriarContaInput privado;201 UsuarioResponse | Usuarios.Gerenciar |
| PUT /usuarios/{id} | AtualizarUsuarioRequest;200 | Usuarios.Gerenciar |
| POST /usuarios/{id}/redefinir-senha | ResetInput privado;204 | Usuarios.Gerenciar |
| GET /funcionarios | 200 FuncionariosResponse | Funcionarios.Gerenciar |
| GET /funcionarios/{id} | 200 FuncionarioResponse | Funcionarios.Gerenciar |
| POST /funcionarios | CriarFuncionarioRequest;201 FuncionarioResponse | Funcionarios.Gerenciar |
| PUT /funcionarios/{id} | AtualizarFuncionarioRequest;200 FuncionarioResponse | Funcionarios.Gerenciar |
| GET /clientes | 200 ClientesResponse | Clientes.Gerenciar |
| GET /clientes/{id} | 200 ClienteResponse | Clientes.Gerenciar |
| POST /clientes | CriarClienteRequest;201 ClienteResponse | Clientes.Gerenciar |
| PUT /clientes/{id} | AtualizarClienteRequest;200 ClienteResponse | Clientes.Gerenciar |
| GET /categorias | 200 ReferenciasResponse | Produtos.Ler |
| GET /categorias/{id} | 200 ReferenciaResponse | Produtos.Ler |
| POST /categorias | CriarReferenciaRequest;201 ReferenciaResponse | Produtos.Gerenciar |
| PUT /categorias/{id} | AtualizarReferenciaRequest;200 ReferenciaResponse | Produtos.Gerenciar |
| GET /marcas | 200 ReferenciasResponse | Produtos.Ler |
| GET /marcas/{id} | 200 ReferenciaResponse | Produtos.Ler |
| POST /marcas | CriarReferenciaRequest;201 ReferenciaResponse | Produtos.Gerenciar |
| PUT /marcas/{id} | AtualizarReferenciaRequest;200 ReferenciaResponse | Produtos.Gerenciar |
| GET /produtos | 200 ProdutosResponse | Produtos.Ler |
| GET /produtos/{id} | 200 ProdutoResponse | Produtos.Ler |
| POST /produtos | CriarProdutoRequest;201 ProdutoResponse | Produtos.Gerenciar |
| PUT /produtos/{id} | AtualizarProdutoRequest;200 ProdutoResponse | Produtos.Gerenciar |
| GET /clientes/{id}/vendas | 200 HistoricoClienteResponse | Clientes.Gerenciar + propriedade |
| GET /produtos/{id}/imagem | 200 bytes PNG | Produtos.Ler |
| POST /produtos/{id}/imagem | multipart arquivo e versao;200 ProdutoResponse | Produtos.Gerenciar |
| DELETE /produtos/{id}/imagem | query versao;204 | Produtos.Gerenciar |
| GET /estoque/saldos | 200 SaldosResponse | Estoque.Ler |
| GET /estoque/movimentos | 200 MovimentosResponse | Estoque.Historico |
| GET /estoque/movimentos/{id} | 200 MovimentoResponse | Estoque.Historico |
| POST /estoque/entradas | MovimentoRequest; chave;201 MovimentoResponse | Estoque.Movimentar |
| POST /estoque/saidas | MovimentoRequest; chave;201 MovimentoResponse | Estoque.Movimentar |
| POST /estoque/ajustes | AjusteRequest; chave;201 MovimentoResponse | Estoque.Ajustar |
| GET /vendas | 200 VendasResponse | Vendas.Ler + propriedade |
| GET /vendas/{id} | 200 VendaResponse | Vendas.Ler + propriedade |
| POST /vendas | CriarVendaRequest; chave;201 VendaResponse | Vendas.Criar |
| PUT /vendas/{id} | AtualizarVendaRequest;200 VendaResponse | Vendas.Editar + propriedade |
| POST /vendas/{id}/confirmacao | ConfirmarVendaRequest; chave;200 VendaResponse | Vendas.Confirmar |
| POST /vendas/{id}/cancelamento | EncerrarVendaRequest; chave;200 VendaResponse | Vendas.Cancelar |
| POST /vendas/{id}/descarte | EncerrarVendaRequest; chave;200 VendaResponse | Vendas.Editar |
| GET /dashboard | 200 DashboardResponse | Dashboard.Ler |
| GET /relatorios/vendas | 200 RelatorioVendasResponse | Relatorios.Vendas |
| GET /relatorios/vendas/exportacao | 200 CSV UTF-8 | Relatorios.Vendas |
| GET /relatorios/operadores | 200 RelatorioOperadoresResponse | Relatorios.Vendas |
| GET /relatorios/operadores/exportacao | 200 CSV UTF-8 | Relatorios.Vendas |
| GET /relatorios/produtos | 200 RelatorioProdutosResponse | Relatorios.Vendas |
| GET /relatorios/produtos/exportacao | 200 CSV UTF-8 | Relatorios.Vendas |
| GET /relatorios/estoque | 200 RelatorioEstoqueResponse | Relatorios.Estoque |
| GET /relatorios/estoque/exportacao | 200 CSV UTF-8 | Relatorios.Estoque |
| GET /relatorios/movimentos | 200 RelatorioMovimentosResponse | Relatorios.Movimentos |
| GET /relatorios/movimentos/exportacao | 200 CSV UTF-8 | Relatorios.Movimentos |
| GET /administracao/dados-demonstracao | 200 EstadoCargaResponse | Funcionarios.Gerenciar |
| POST /administracao/dados-demonstracao/gerar | 200 CargaDemonstracaoResponse | Funcionarios.Gerenciar |
| POST /administracao/dados-demonstracao/restaurar | RestaurarCargaRequest;200 CargaDemonstracaoResponse | Funcionarios.Gerenciar |
| POST /administracao/dados-demonstracao/limpar | LimparDadosRequest;204 | Funcionarios.Gerenciar |


Rotas Web de conta: POST /conta/entrar, /conta/sair e /conta/senha, com formulário e antiforgery; criação/redefinição de conta também atravessam formulários próprios. GET /produtos/{id}/imagem transmite imagem autenticada e downloads /downloads/relatorios/{tipo} passam pelo BFF. Rotas técnicas /health/live e /health/ready estão nos hosts; não integram API comercial autenticada.

## 9.3 Exemplos sintéticos de transporte

Os exemplos mostram estrutura, não credenciais nem registros reais. Para criar rascunho, o cabeçalho Idempotency-Key deve conter UUID válido gerado para a intenção. Corpo: {"clienteId":"11111111-1111-4111-8111-111111111111","itens":[{"produtoId":"22222222-2222-4222-8222-222222222222","quantidade":2}]}. Os UUIDs ilustrativos só funcionariam se os recursos existissem. Não se envia preço, total, ator ou perfil.

Para confirmar, POST /vendas/{id}/confirmacao recebe {"versaoEsperada":1}, com a chave correspondente à confirmação. Para cancelar, POST /vendas/{id}/cancelamento recebe {"versaoEsperada":2,"motivo":"Cancelamento de exemplo"}. Valores de versão são ilustrativos e devem vir de leitura real do recurso.

## 9.4 Erros e recuperação

| Situação | HTTP/código | Conduta do consumidor |
| --- | --- | --- |
| Entrada inválida | 400 / VALIDACAO | Corrigir campos; não repetir cegamente |
| Sessão inválida | 401 | Encerrar acesso e autenticar |
| Perfil ou troca pendente | 403 | Respeitar policy ou concluir troca |
| Ausente ou venda alheia | 404 | Não revelar recurso fora do escopo |
| Versão antiga | 409 / VERSAO_DESATUALIZADA | Reler antes de decidir |
| Preço alterado | 409 / PRECO_ALTERADO | Salvar rascunho e revisar preços |
| Insuficiência/integridade | 409 / ESTOQUE_INSUFICIENTE ou SALDO_INEXISTENTE | Rever saldo; não criar saldo automaticamente |
| Chave com dados divergentes | 409 / CHAVE_REUTILIZADA | Não alterar intenção pendente |
| Imagem excedente/inválida | 413 ou 415, ou 400 de conteúdo | Usar JPEG/PNG permitido |
| Muitas tentativas | 429 | Aguardar |
| Resultado incerto | 503 / RESULTADO_DESCONHECIDO | Conservar chave e reconsultar |
| Instalação incompleta | 503 / PERSISTENCIA_NAO_INICIALIZADA | Responsável verifica evolução; não criar schema na requisição |

## 9.5 Inventário dos contratos compartilhados

Propriedades listadas com nome C# e tipo; no JSON, nomes camelCase. ? indica nulabilidade declarada; não prova que o caso de uso aceita null. IReadOnlyList indica coleção. Requests são entradas e Response/resultados são saídas; não contêm documentos de persistência. Credenciais de login/senha são tipos privados dos hosts, fora de Contracts.

### Autenticacao.cs

| Tipo | Campos e tipos C# |
| --- | --- |
| UsuarioAutenticado | Guid Id; string Nome; string Perfil; bool DeveAlterarSenha |
| SessaoResponse | UsuarioAutenticado Usuario; DateTimeOffset ExpiraEm |
| UsuarioResponse | Guid Id; string Nome; string Email; string Perfil; bool Ativo; long Versao; bool DeveAlterarSenha; Guid? FuncionarioId |
| UsuariosResponse | IReadOnlyList<UsuarioResponse> Items; int Page; int PageSize; long Total |
| AtualizarUsuarioRequest | string Perfil; bool Ativo; long Versao |


### Clientes.cs

| Tipo | Campos e tipos C# |
| --- | --- |
| ClienteResponse | Guid Id; string Nome; string Tipo; string? Documento; string? Email; string? Telefone; bool Ativo; long Versao; DateTimeOffset CreatedAt; DateTimeOffset UpdatedAt |
| ClientesResponse | IReadOnlyList<ClienteResponse> Items; int Page; int PageSize; long TotalItems |
| CriarClienteRequest | string? Nome; string? Tipo; string? Documento; string? Email; string? Telefone |
| AtualizarClienteRequest | string? Nome; string? Tipo; string? Documento; string? Email; string? Telefone; bool Ativo; long Versao |
| VendaClienteResponse | Guid Id; DateTimeOffset CriadoEm; string Status; decimal Total |
| HistoricoClienteResponse | IReadOnlyList<VendaClienteResponse> Items; int Page; int PageSize; long TotalItems |


### DadosDemonstracao.cs

| Tipo | Campos e tipos C# |
| --- | --- |
| RestaurarCargaRequest | bool Confirmar |
| LimparDadosRequest | Guid OperacaoId; string Confirmacao |
| CargaDemonstracaoResponse | int Funcionarios; int Usuarios; int Clientes; int Produtos; int Vendas; int Canceladas |
| EstadoCargaResponse | string Banco; DateTimeOffset? UltimaExecucao |


### Dashboard.cs

| Tipo | Campos e tipos C# |
| --- | --- |
| ResumoComercialResponse | long QuantidadeConfirmada; long QuantidadeCancelada; decimal ReceitaLiquida; decimal TicketMedio |
| ProdutoBaixoResponse | Guid ProdutoId; string Sku; string Nome; int Saldo; int Minimo |
| EstoqueBaixoResponse | IReadOnlyList<ProdutoBaixoResponse> Items; int Page; int PageSize; long TotalItems |
| VendaRecenteResponse | Guid VendaId; DateTimeOffset ConfirmadoEm; string ClienteNome; string OperadorNome; string Status; decimal TotalOriginal; decimal ContribuicaoLiquida |
| EvolucaoVendaResponse | DateOnly Dia; long Quantidade; decimal ReceitaLiquida |
| ProdutoVendidoResponse | Guid ProdutoId; string Sku; string Nome; long Quantidade; decimal ReceitaLiquida |
| DashboardResponse | DateOnly Inicio; DateOnly Fim; string Fuso; DateTimeOffset GeradoEm; ResumoComercialResponse Resumo; long ProdutosCadastrados; long ProdutosAtivos; EstoqueBaixoResponse EstoqueBaixo; IReadOnlyList<VendaRecenteResponse> Recentes; IReadOnlyList<EvolucaoVendaResponse> Evolucao; IReadOnlyList<ProdutoVendidoResponse> MaisVendidos |


### Estoque.cs

| Tipo | Campos e tipos C# |
| --- | --- |
| SaldoProdutoResponse | Guid ProdutoId; string Sku; string Nome; int Saldo; int Minimo; bool Ativo |
| SaldosResponse | IReadOnlyList<SaldoProdutoResponse> Items; int Page; int PageSize; long TotalItems |
| AtorMovimentoResponse | Guid Id; string Nome |
| MovimentoResponse | Guid Id; Guid ProdutoId; string Tipo; int Delta; int SaldoAntes; int SaldoDepois; string Motivo; AtorMovimentoResponse Ator; DateTimeOffset OcorridoEm; Guid? VendaId=null; Guid? ItemId=null |
| MovimentosResponse | IReadOnlyList<MovimentoResponse> Items; int Page; int PageSize; long TotalItems |
| MovimentoRequest | Guid ProdutoId; int Quantidade; string? Motivo |
| AjusteRequest | Guid ProdutoId; int Delta; string? Motivo |


### Funcionarios.cs

| Tipo | Campos e tipos C# |
| --- | --- |
| FuncionarioResponse | Guid Id; string Nome; string? Email; string? Telefone; bool Ativo; long Versao; DateTimeOffset CreatedAt; DateTimeOffset UpdatedAt |
| FuncionariosResponse | IReadOnlyList<FuncionarioResponse> Items; int Page; int PageSize; long TotalItems |
| CriarFuncionarioRequest | string? Nome; string? Email; string? Telefone |
| AtualizarFuncionarioRequest | string? Nome; string? Email; string? Telefone; bool Ativo; long Versao |


### HealthResponse.cs

| Tipo | Campos e tipos C# |
| --- | --- |
| HealthResponse | string Status |


### Produtos.cs

| Tipo | Campos e tipos C# |
| --- | --- |
| ReferenciaResponse | Guid Id; string Nome; bool Ativo; long Versao |
| ReferenciasResponse | IReadOnlyList<ReferenciaResponse> Items; int Page; int PageSize; long TotalItems |
| CriarReferenciaRequest | string? Nome |
| AtualizarReferenciaRequest | string? Nome; bool Ativo; long Versao |
| ProdutoResponse | Guid Id; string Sku; string Nome; string? Descricao; Guid CategoriaId; Guid? MarcaId; decimal Preco; int EstoqueMinimo; int Saldo; string? ImagemUrl; bool Ativo; long Versao; DateTimeOffset CreatedAt; DateTimeOffset UpdatedAt |
| ProdutosResponse | IReadOnlyList<ProdutoResponse> Items; int Page; int PageSize; long TotalItems |
| CriarProdutoRequest | string? Sku; string? Nome; string? Descricao; Guid CategoriaId; Guid? MarcaId; decimal Preco; int EstoqueMinimo |
| AtualizarProdutoRequest | string? Sku; string? Nome; string? Descricao; Guid CategoriaId; Guid? MarcaId; decimal Preco; int EstoqueMinimo; bool Ativo; long Versao |


### Relatorios.cs

| Tipo | Campos e tipos C# |
| --- | --- |
| FiltrosRelatorioResponse | DateOnly? Inicio; DateOnly? Fim; string Fuso; DateTimeOffset GeradoEm; string Ordenacao; Guid? OperadorId; Guid? ProdutoId; string? Tipo; string? Search; bool? Ativo |
| OperadorRelatorioResponse | Guid OperadorId; string Nome; long QuantidadeConfirmada; long QuantidadeCancelada; decimal ReceitaLiquida; decimal TicketMedio |
| TotaisEstoqueResponse | long Saldo; long Baixos |
| TotaisMovimentosResponse | long Entradas; long Saidas; long Delta |
| RelatorioVendasResponse | IReadOnlyList<VendaRecenteResponse> Items; int Page; int PageSize; long TotalItems; ResumoComercialResponse Totais; FiltrosRelatorioResponse Filtros |
| RelatorioOperadoresResponse | IReadOnlyList<OperadorRelatorioResponse> Items; int Page; int PageSize; long TotalItems; ResumoComercialResponse Totais; FiltrosRelatorioResponse Filtros |
| RelatorioProdutosResponse | IReadOnlyList<ProdutoVendidoResponse> Items; int Page; int PageSize; long TotalItems; ResumoComercialResponse Totais; FiltrosRelatorioResponse Filtros |
| RelatorioEstoqueResponse | IReadOnlyList<SaldoProdutoResponse> Items; int Page; int PageSize; long TotalItems; TotaisEstoqueResponse Totais; FiltrosRelatorioResponse Filtros |
| RelatorioMovimentosResponse | IReadOnlyList<MovimentoResponse> Items; int Page; int PageSize; long TotalItems; TotaisMovimentosResponse Totais; FiltrosRelatorioResponse Filtros |


### Vendas.cs

| Tipo | Campos e tipos C# |
| --- | --- |
| ItemVendaRequest | Guid ProdutoId; int Quantidade |
| CriarVendaRequest | Guid ClienteId; IReadOnlyList<ItemVendaRequest>? Itens |
| AtualizarVendaRequest | Guid ClienteId; IReadOnlyList<ItemVendaRequest>? Itens; long Versao |
| ConfirmarVendaRequest | long VersaoEsperada |
| EncerrarVendaRequest | long VersaoEsperada; string? Motivo |
| OperadorVendaResponse | Guid Id; string Nome |
| ClienteVendaResponse | string Nome; string? Documento |
| EventoVendaResponse | OperadorVendaResponse Ator; DateTimeOffset Data; string? Motivo |
| ItemVendaResponse | Guid Id; Guid ProdutoId; string SkuSnapshot; string NomeSnapshot; int Quantidade; decimal PrecoUnitario; decimal Total |
| VendaResponse | Guid Id; Guid ClienteId; ClienteVendaResponse? ClienteSnapshot; OperadorVendaResponse Criador; string Status; IReadOnlyList<ItemVendaResponse> Itens; decimal Total; long Versao; DateTimeOffset CriadoEm; EventoVendaResponse? Confirmacao; EventoVendaResponse? Cancelamento; EventoVendaResponse? Descarte |
| VendaResumoResponse | Guid Id; Guid ClienteId; string? ClienteNome; OperadorVendaResponse Criador; string Status; decimal Total; long Versao; DateTimeOffset CriadoEm |
| VendasResponse | IReadOnlyList<VendaResumoResponse> Items; int Page; int PageSize; long TotalItems |


# 10 Segurança e operação técnica

MongoAutenticacao verifica expiração absoluta e inatividade de trinta minutos no servidor. Senhas têm limites de 12–128 caracteres e política Identity configurada, com bloqueio após falhas conforme implementação. A API não aceita identidade do ator enviada no corpo. Troca de senha, redefinição e alteração de perfil revogam sessões conforme comando; proteção do último Administrador usa guarda administrativa real.

No Web, handle aleatório identifica ticket; handleHash e token protegido são persistidos. Cookie é HttpOnly/Secure e formulários de conta usam antiforgery. Sem as chaves Data Protection, tickets protegidos não são simplesmente recuperáveis; persistência das chaves é requisito operacional. Login/logout devem atravessar requisição HTTP, em vez de manipular cookie dentro do circuito interativo. A documentação Microsoft descreve os mecanismos de autenticação/autorização Blazor (Microsoft, s.d.b).

Imagens são decodificadas/validadas e convertidas a PNG, com tamanho e dimensões limitados. O caminho privado não é retornado. Downloads CSV reaplicam policy, usam UTF-8, ponto decimal e separador ponto e vírgula; texto potencialmente interpretado como fórmula é neutralizado. O risco é descrito pela OWASP (OWASP Foundation, s.d.b); não se declara proteção universal em todos os leitores de planilha.

Logs devem registrar contexto permitido sem URI, senha, token ou payload sensível. Health mínimo não publica detalhes de conexão. Liveness indica processo e readiness verifica pré-requisitos; ready503 não equivale a senha incorreta. Nenhum endereço privado, email de pessoa real ou credencial foi incorporado a este material.

# 11 Dados demonstrativos e exclusão delimitada

Gerar cria cenário fictício com 12 funcionários/contas, 40 clientes, 100 produtos e 250 vendas, sendo25 canceladas, conforme gerador. As quantidades descrevem a carga definida, não contagem atual do Atlas. Os serviços comerciais existentes são reutilizados; proveniência é registrada em audit_events. A operação pode retomar etapas já realizadas e a página informa última conclusão.

Restaurar verifica dependências e alterações manuais e remove somente o conjunto demonstrativo comprovado. Não equivale a restaurar um backup externo. Limpar exige UUID e texto exato APAGAR DADOS e é exceção explícita às regras normais de histórico. O backend apaga sales, stock_movements, idempotency_receipts, products, customers, categories e brands; elimina demais contas/funcionários e sessões, preservando executor, seu vínculo quando houver e sua sessão.

O catálogo, controle técnico e tickets permanecem; sessões API removidas impedem que tickets antigos concedam acesso. audit_events conserva recibos DemoLimpa e registra o novo UUID. Repetir UUID concluído não elimina dados criados depois. Arquivos de imagem fora do banco não são apagados. A geração posterior é outra ação. Nenhuma dessas operações foi acionada para produzir os documentos.

# 12 Estratégia de testes e evidências

Testes unitários cobrem validações, transições, contatos, dinheiro, CSV, configuração e renderização pontual. Integração usa MongoDB real e transações, com Atlas isolado por execução ou perfil Testcontainers explícito. O suporte de replica set do módulo é documentado por Testcontainers (Testcontainers, s.d.). Docker ausente não justifica standalone ou banco em memória para comprovar transações.

| Cenário | Método existente | Linha |
| --- | --- | --- |
| Venda com snapshots e estorno único | SalesTests.FullLifecycleUsesExactServerPricesSnapshotsAndOneRestock | 31 |
| Insuficiência e falha de auditoria | SalesTests.MultipleItemsInsufficientOrAuditFailureRollBackAllDocuments | 58 |
| Última unidade entre sessões | SalesTests.TwoSalesCompeteForLastUnitWithIndependentSessions | 71 |
| Idempotência e corpo divergente | SalesTests.CreateAndConfirmIdempotencyAreConcurrentAndBoundToRequest | 79 |
| Propriedade antes da paginação | SalesTests.SellerOwnershipAppliesBeforePaginationCountsAndClientHistory | 101 |
| Cancelamento com overflow e rollback | SalesTests.CancellationOverflowAndAuditFailurePreserveConfirmedSaleUntilSuccessfulRetry | 117 |
| Conciliação de estoque | StockTests.EntriesExitsAdjustmentsReconcileAndHistoryIsImmutable | 31 |
| Cinco relatórios e CSV | ReportsTests.AllFiveReportsUseSnapshotsFiltersTotalsDeterministicPagesAndSafeCsv | 20 |
| Limpeza e nova carga | DemonstrationCleanupTests.CleanupPreservesActorAndEmployeeRevokesOthersRollsBackAndAllowsFreshLoad | 13 |


## 12.1 Evidências históricas e limites

Estado03 registra homologação em07/11/2026 com código0d49db9 e CI de242 testes, incluindo126 unitários e116 integrações. Registra em09/11/2026 validação local da limpeza com128 unitários e30 integrações direcionadas. Em23/11/2026, o ajuste de identidade visual registra build e medições estáticas em1920/1366/768/390px. Estes resultados pertencem às execuções citadas, não a esta tarefa.

A pasta artifacts mencionada nos relatórios não está disponível neste checkout. Há imagens .local/unigran-*.png de montagem estática; não foram tratadas como jornada autenticada. A entrega conserva os relatórios como fontes históricas e não inventa screenshots funcionais, logs ausentes, percentual de cobertura ou novos resultados de CI.

## 12.2 Critérios para manutenção futura

Mudança de regra deve localizar RF/RN/CA e ampliar testes pertinentes. Em transações, verificar insuficiência em item posterior, rollback de auditoria, concorrência entre sessões, idempotência com mesmo e diferente corpo, commit incerto e revogação/perfil/propriedade. Em consultas, verificar filtros antes da paginação e totais do conjunto inteiro. Alterações só documentais requerem conferir fontes, links, diagramas e renderização.

# 13 Catálogo de diagramas

Cada modelo possui fonte .puml, exportação .svg e .png no mesmo diretório diagramas. Fontes são autorais, derivadas da inspeção do código; não são figuras externas copiadas. Classes conceituais, recortes de implementação e documentos físicos são explicitamente distintos. Referência de notação: UML2.5.1 (Object Management Group, 2017).

## Casos de uso gerais

![Figura - Casos de uso gerais](../diagramas/01-casos-visao-geral.png)

Fonte: elaboração própria a partir de docs/requirements/05-MATRIZ-PERMISSOES.md; backend/GestaoComercial.Api/Acesso.

## Casos de uso de acesso e equipe

![Figura - Casos de uso de acesso e equipe](../diagramas/02-casos-acesso-equipe.png)

Fonte: elaboração própria a partir de backend/GestaoComercial.Api/Acesso; docs/requirements/05-MATRIZ-PERMISSOES.md.

## Casos de uso de cadastros

![Figura - Casos de uso de cadastros](../diagramas/03-casos-cadastros.png)

Fonte: elaboração própria a partir de backend/GestaoComercial.Api/Acesso; docs/requirements/05-MATRIZ-PERMISSOES.md.

## Casos de uso de estoque

![Figura - Casos de uso de estoque](../diagramas/04-casos-estoque.png)

Fonte: elaboração própria a partir de backend/GestaoComercial.Api/Acesso; docs/requirements/05-MATRIZ-PERMISSOES.md.

## Casos de uso de vendas

![Figura - Casos de uso de vendas](../diagramas/05-casos-vendas.png)

Fonte: elaboração própria a partir de backend/GestaoComercial.Api/Acesso; docs/requirements/05-MATRIZ-PERMISSOES.md.

## Casos de uso de análise e demonstração

![Figura - Casos de uso de análise e demonstração](../diagramas/06-casos-analise-demonstracao.png)

Fonte: elaboração própria a partir de backend/GestaoComercial.Api/Acesso; docs/requirements/05-MATRIZ-PERMISSOES.md.

## Classes conceituais de cadastros

![Figura - Classes conceituais de cadastros](../diagramas/07-classes-conceituais-cadastros.png)

Fonte: elaboração própria a partir de docs/architecture/04-MODELO-DE-DOMINIO.md; schemas de cadastros.

## Classes conceituais de vendas e estoque

![Figura - Classes conceituais de vendas e estoque](../diagramas/08-classes-conceituais-vendas.png)

Fonte: elaboração própria a partir de IVendas.cs; VendasSchema.cs; EstoqueSchema.cs.

## Classes e interfaces implementadas em vendas

![Figura - Classes e interfaces implementadas em vendas](../diagramas/09-classes-implementadas-vendas.png)

Fonte: elaboração própria a partir de backend/GestaoComercial.Application/Vendas/IVendas.cs; backend/GestaoComercial.Infrastructure/Vendas/MongoVendas.cs.

## Classes e interfaces implementadas em estoque

![Figura - Classes e interfaces implementadas em estoque](../diagramas/10-classes-implementadas-estoque.png)

Fonte: elaboração própria a partir de backend/GestaoComercial.Application/Estoque/IEstoque.cs; backend/GestaoComercial.Infrastructure/Estoque/MongoEstoque.cs.

## Dependências entre projetos

![Figura - Dependências entre projetos](../diagramas/11-pacotes.png)

Fonte: elaboração própria a partir de arquivos .csproj dos seis projetos de produção.

## Componentes e comunicação

![Figura - Componentes e comunicação](../diagramas/12-componentes.png)

Fonte: elaboração própria a partir de Program.cs de Api/Web; arquitetura02.

## Modelo documental comercial MongoDB

![Figura - Modelo documental comercial MongoDB](../diagramas/13-modelo-documental-comercial.png)

Fonte: elaboração própria a partir de ClientesSchema; ProdutosSchema; EstoqueSchema; VendasSchema.

## Modelo documental de segurança e coordenação

![Figura - Modelo documental de segurança e coordenação](../diagramas/14-modelo-documental-seguranca.png)

Fonte: elaboração própria a partir de AuthSchema.cs; MongoTickets.cs; EstoqueSchema.cs; MongoFoundation.cs.

## Sequência de autenticação

![Figura - Sequência de autenticação](../diagramas/15-sequencia-login.png)

Fonte: elaboração própria a partir de EndpointsAcesso.cs; Security/ContasHttp.cs; MongoAutenticacao.cs; MongoTickets.cs.

## Sequência de confirmação de venda

![Figura - Sequência de confirmação de venda](../diagramas/16-sequencia-confirmacao.png)

Fonte: elaboração própria a partir de MongoVendas.cs; OperacoesComerciais.cs; MongoEstoque.cs.

## Sequência de cancelamento

![Figura - Sequência de cancelamento](../diagramas/17-sequencia-cancelamento.png)

Fonte: elaboração própria a partir de MongoVendas.TransicionarAsync; MongoEstoque.AplicarAsync.

## Sequência de movimentação manual

![Figura - Sequência de movimentação manual](../diagramas/18-sequencia-estoque.png)

Fonte: elaboração própria a partir de EndpointsEstoque.cs; MongoEstoque.cs; Movimentacao.cs.

## Atividade de venda

![Figura - Atividade de venda](../diagramas/19-atividade-venda.png)

Fonte: elaboração própria a partir de VendaDetalhe.razor; MongoVendas.cs.

## Atividade de manutenção de estoque

![Figura - Atividade de manutenção de estoque](../diagramas/20-atividade-estoque.png)

Fonte: elaboração própria a partir de Estoque.razor; Movimentacao.cs; MongoEstoque.cs.

## Estados da venda

![Figura - Estados da venda](../diagramas/21-estados-venda.png)

Fonte: elaboração própria a partir de backend/GestaoComercial.Domain/Vendas/RegrasVenda.cs.

## Topologia da execução local

![Figura - Topologia da execução local](../diagramas/22-execucao-local.png)

Fonte: elaboração própria a partir de launchSettings.json de Api/Web; Program.cs; ADR-37.

# Referências

<!-- references -->

# Apêndice A Inventário de testes

Este inventário lista métodos anotados Fact/Theory encontrados no código. Theory pode executar múltiplos casos. Não representa contagem de testes aprovados e não inclui execução nesta tarefa.

### AdministrationTests

Localização: tests/GestaoComercial.IntegrationTests/AdministrationTests.cs.

| Método | Tipo / linha |
| --- | --- |
| BootstrapIsOneTimeAndNeverResetsPassword | Fact / 11 |
| ConcurrentRemovalProtectsLastActiveAdministrator | Fact / 23 |


### AnalyticsReadinessTests

Localização: tests/GestaoComercial.IntegrationTests/AnalyticsReadinessTests.cs.

| Método | Tipo / linha |
| --- | --- |
| IncompletePersistenceCannotLookLikeValidEmptyDashboardOrCsv | Fact / 9 |


### AuthAtomicityTests

Localização: tests/GestaoComercial.IntegrationTests/AuthAtomicityTests.cs.

| Método | Tipo / linha |
| --- | --- |
| AuditFailureRollsBackProfileStampAndSessionRevocation | Fact / 11 |


### AuthenticationTests

Localização: tests/GestaoComercial.IntegrationTests/AuthenticationTests.cs.

| Método | Tipo / linha |
| --- | --- |
| ValidLoginPersistsHashOnlyAndMeDoesNotRefreshIdle | Fact / 67 |
| InvalidCredentialsAreIndistinguishable | Theory / 82 |
| LogoutRevokesOnlyCurrentSession | Fact / 90 |
| BackendEnforcesCurrentRole | Theory / 102 |
| UniqueLoginIncludesInactiveAccountsAndInvalidProfilesAreRejected | Fact / 111 |
| FiveFailedAttemptsLockAccountAndCounterIsNotLost | Fact / 119 |
| PasswordChangeIsMandatoryAndRevokesEverySession | Fact / 127 |
| ProfileChangeResetAndOptimisticVersionAreAtomic | Fact / 138 |
| IdleAndAbsoluteExpirationFailClosedWithControlledClock | Fact / 151 |
| IdentityStoreNormalizesEmailAndRejectsStaleWrites | Fact / 162 |
| ConcurrentFailuresPreserveBothAttempts | Fact / 178 |
| LoginIdentifierRateLimitReturns429 | Fact / 188 |
| DisablingAccountRevokesCurrentSessions | Fact / 195 |
| SchemaUpgradeIsIdempotentPreservesUsersAndRejectsDivergence | Fact / 204 |


### BffTests

Localização: tests/GestaoComercial.IntegrationTests/BffTests.cs.

| Método | Tipo / linha |
| --- | --- |
| RealBffLoginCookieSessionCsrfLogoutAndNoTokenInHtml | Fact / 54 |
| MandatoryPasswordChangeUsesHttpAndRequiresNewLogin | Fact / 72 |
| TwoUsersAreIsolatedAndSellerCannotOpenAdministration | Fact / 83 |
| InvalidLoginReturnsGenericFormWithoutPasswordAndOfflineFailsClosed | Fact / 93 |
| RevokedCookieCanLoginAgainAndRevalidationRejectsOldState | Fact / 102 |
| CrossOriginCircuitIsRejected | Fact / 126 |
| LogoutOfflineRemovesLocalTicketAndCookie | Fact / 132 |


### CustomerBffTests

Localização: tests/GestaoComercial.IntegrationTests/CustomerBffTests.cs.

| Método | Tipo / linha |
| --- | --- |
| CustomerPagesRespectPolicyAndRealApiFailure | Fact / 8 |


### CustomerTests

Localização: tests/GestaoComercial.IntegrationTests/CustomerTests.cs.

| Método | Tipo / linha |
| --- | --- |
| CrudFiltersEmailDocumentPaginationVersionAndHistory | Fact / 18 |
| AuthorizationForEveryVerb | Theory / 38 |
| RejectsMalformedFieldsFiltersAndHistoryPeriods | Fact / 53 |
| ConcurrentDocumentAndConcurrentEditsHaveOneWinner | Fact / 62 |
| UpgradeIdempotentValidatorRejectsBadDataAndAuditFailureRollsBack | Fact / 71 |


### DashboardBffTests

Localização: tests/GestaoComercial.IntegrationTests/DashboardBffTests.cs.

| Método | Tipo / linha |
| --- | --- |
| DashboardConsumesRealApiAndHidesMetricsFromOtherRoles | Fact / 9 |


### DashboardTests

Localização: tests/GestaoComercial.IntegrationTests/DashboardTests.cs.

| Método | Tipo / linha |
| --- | --- |
| SnapshotSurvivesCancellationBetweenSummaryAndListsAndSumsBeyondDoublePrecision | Fact / 17 |
| ExactMetricsBoundariesRankingSnapshotsRecentAndLowStock | Fact / 43 |
| BackendPolicyAndValidation | Theory / 74 |


### DemonstrationBffTests

Localização: tests/GestaoComercial.IntegrationTests/DemonstrationBffTests.cs.

| Método | Tipo / linha |
| --- | --- |
| PageNavigationAndConfirmationAreAdministratorOnly | Fact / 10 |


### DemonstrationCleanupTests

Localização: tests/GestaoComercial.IntegrationTests/DemonstrationCleanupTests.cs.

| Método | Tipo / linha |
| --- | --- |
| CleanupPreservesActorAndEmployeeRevokesOthersRollsBackAndAllowsFreshLoad | Fact / 13 |
| OnlyAdministratorMayClear | Theory / 64 |


### DemonstrationTests

Localização: tests/GestaoComercial.IntegrationTests/DemonstrationTests.cs.

| Método | Tipo / linha |
| --- | --- |
| LoadRepeatRestoreReconcileReportsAndPreserveManualDependencies | Fact / 14 |
| DenyNonAdministratorsAndAnonymous | Theory / 73 |


### DemonstrationWebTests

Localização: tests/GestaoComercial.IntegrationTests/DemonstrationWebTests.cs.

| Método | Tipo / linha |
| --- | --- |
| SharedDatabaseTicketsUpgradeAndAdminPageAuthorization | Fact / 13 |


### EmployeeBffTests

Localização: tests/GestaoComercial.IntegrationTests/EmployeeBffTests.cs.

| Método | Tipo / linha |
| --- | --- |
| EmployeePagesAndHttpAccountCreationRequireCookiePolicyAndAntiforgery | Fact / 10 |


### EmployeeTests

Localização: tests/GestaoComercial.IntegrationTests/EmployeeTests.cs.

| Método | Tipo / linha |
| --- | --- |
| CrudPaginationVersionAndNoAutomaticAccount | Fact / 23 |
| AllEmployeeRoutesDenyOtherRoles | Theory / 41 |
| LinkedAccountMustChangePasswordAndDeactivationRevokesExistingSessions | Fact / 53 |
| ConcurrentAccountsHaveExactlyOneWinnerAndUniqueIndexIncludesInactive | Fact / 73 |
| CreateAccountVersusDeactivateNeverLeavesActiveLinkedAccount | Fact / 82 |
| SchemaIdempotentAndAuditFailureRollsBackEmployeeAccountAndSession | Fact / 101 |
| LastAdministratorCannotBeDisabledThroughEmployee | Fact / 115 |


### FoundationTests

Localização: tests/GestaoComercial.IntegrationTests/FoundationTests.cs.

| Método | Tipo / linha |
| --- | --- |
| ExplicitInitializationIsIdempotentAndWebIndependent | Fact / 79 |
| TransactionCommitsAndAbortsDisposableDocuments | Fact / 94 |
| BaselineRejectsInvalidDocumentAndDoesNotOverwriteIncompatibleVersion | Fact / 122 |
| ApiHostReadsRealReadyAndWebUsesTypedHttpClient | Fact / 139 |


### ProductBffTests

Localização: tests/GestaoComercial.IntegrationTests/ProductBffTests.cs.

| Método | Tipo / linha |
| --- | --- |
| PagesAndImageProxyUseRealSessionWithoutExposingApiToken | Fact / 8 |


### ProductTests

Localização: tests/GestaoComercial.IntegrationTests/ProductTests.cs.

| Método | Tipo / linha |
| --- | --- |
| ProductCrudFiltersPreserveZeroBalanceAndStoreInt64 | Fact / 20 |
| ReferenceCrudUniqueIncludingInactiveAndInUseProtection | Theory / 32 |
| RejectsInvalidPriceFieldsReferencesAndMassAssignment | Fact / 46 |
| ConcurrentSkuAndReferenceInactivationHaveSafeOutcomes | Fact / 53 |
| EnforcesPoliciesForEveryRoute | Theory / 67 |
| ImagesArePrivateVersionedReencodedAndBadUploadsPreservePrevious | Fact / 86 |
| ConcurrentImageReplacementsKeepCommittedFileAndCleanupNeverRemovesReferences | Fact / 100 |
| AuditFailureAbortsProductAndImageMetadataAndRetainsOldFile | Fact / 109 |


### ProductsUpgradeTests

Localização: tests/GestaoComercial.IntegrationTests/ProductsUpgradeTests.cs.

| Método | Tipo / linha |
| --- | --- |
| CleanInstallAndUpgradePreserveEarlierDataRejectBadBsonAndDivergence | Fact / 12 |


### ReportsBffTests

Localização: tests/GestaoComercial.IntegrationTests/ReportsBffTests.cs.

| Método | Tipo / linha |
| --- | --- |
| PagesAndAuthenticatedDownloadUseRealApiProtectRolesAndKeepErrorsAsJson | Fact / 12 |


### ReportsScaleTests

Localização: tests/GestaoComercial.IntegrationTests/ReportsScaleTests.cs.

| Método | Tipo / linha |
| --- | --- |
| TenThousandRowsExportCompletelyAndTenThousandAndOneIsRejectedBeforeAttachment | Fact / 10 |


### ReportsTests

Localização: tests/GestaoComercial.IntegrationTests/ReportsTests.cs.

| Método | Tipo / linha |
| --- | --- |
| AllFiveReportsUseSnapshotsFiltersTotalsDeterministicPagesAndSafeCsv | Fact / 20 |
| EveryQueryAndExportEnforcesSameBackendPolicy | Theory / 63 |
| StockSnapshotKeepsTotalsAndRowsTogetherDuringCommittedMovement | Fact / 71 |


### SalesBffTests

Localização: tests/GestaoComercial.IntegrationTests/SalesBffTests.cs.

| Método | Tipo / linha |
| --- | --- |
| SalesAndCustomerHistoryUseRealBffSessionAndProtectStockRole | Fact / 11 |


### SalesTests

Localização: tests/GestaoComercial.IntegrationTests/SalesTests.cs.

| Método | Tipo / linha |
| --- | --- |
| FullLifecycleUsesExactServerPricesSnapshotsAndOneRestock | Fact / 31 |
| DraftEditingRefreshesPricesPreservesItemIdsAndDiscardNeverMovesStock | Fact / 47 |
| MultipleItemsInsufficientOrAuditFailureRollBackAllDocuments | Fact / 58 |
| TwoSalesCompeteForLastUnitWithIndependentSessions | Fact / 71 |
| CreateAndConfirmIdempotencyAreConcurrentAndBoundToRequest | Fact / 79 |
| InvalidInactiveAndMissingReferencesNeverProduceBusinessEffects | Fact / 88 |
| SellerOwnershipAppliesBeforePaginationCountsAndClientHistory | Fact / 101 |
| CancellationOverflowAndAuditFailurePreserveConfirmedSaleUntilSuccessfulRetry | Fact / 117 |
| MissingBalanceCannotBeRecreatedBySaleConfirmation | Fact / 129 |
| ReferenceChangesAndConfirmationSerializeWithRealGuards | Theory / 136 |
| ConfirmationControlFaultsUseSameStockTransactionAndReceipt | Theory / 144 |
| FiltersDatesPaginationAndServerValidatorsRejectInvalidHistory | Fact / 151 |


### SalesUpgradeTests

Localização: tests/GestaoComercial.IntegrationTests/SalesUpgradeTests.cs.

| Método | Tipo / linha |
| --- | --- |
| UpgradePreservesManualMovementsAndReceiptsAndReinitializesWholeChain | Fact / 13 |


### StockBffTests

Localização: tests/GestaoComercial.IntegrationTests/StockBffTests.cs.

| Método | Tipo / linha |
| --- | --- |
| StockPagesUseProtectedBffAndSellerOnlySeesBalances | Fact / 8 |


### StockCommitTests

Localização: tests/GestaoComercial.IntegrationTests/StockCommitTests.cs.

| Método | Tipo / linha |
| --- | --- |
| RealTransactionsResolveControlFaultsWithoutDuplicatingMovements | Theory / 62 |


### StockQueryTests

Localização: tests/GestaoComercial.IntegrationTests/StockQueryTests.cs.

| Método | Tipo / linha |
| --- | --- |
| LowStockPlanIsMeasuredWithTenThousandProducts | Fact / 11 |


### StockRuntimeSecurityTests

Localização: tests/GestaoComercial.IntegrationTests/StockRuntimeSecurityTests.cs.

| Método | Tipo / linha |
| --- | --- |
| RuntimeCannotRewriteHistoryChangeSchemaOrReadAnotherDatabase | Fact / 11 |


### StockTests

Localização: tests/GestaoComercial.IntegrationTests/StockTests.cs.

| Método | Tipo / linha |
| --- | --- |
| EntriesExitsAdjustmentsReconcileAndHistoryIsImmutable | Fact / 31 |
| InvalidInsufficientInactiveAndOverflowHaveNoEffects | Fact / 43 |
| IdempotencyIsConcurrentBodyBoundAndActorIsolated | Fact / 64 |
| TwoIndependentExitsCompeteForLastUnit | Fact / 74 |
| EveryRouteEnforcesPolicies | Theory / 83 |
| ConcurrentProductInactivationAndEntrySerializeWithoutResurrectingProduct | Fact / 91 |
| AuditFailureRollsBackBalanceMovementAndReceiptThenSameKeyWorks | Fact / 101 |
| FiltersPaginationAndServerValidatorsAreReal | Fact / 111 |


### StockUpgradeTests

Localização: tests/GestaoComercial.IntegrationTests/StockUpgradeTests.cs.

| Método | Tipo / linha |
| --- | --- |
| Upgrade04To05IsExplicitPreservesProductsAndRejectsDivergentCatalog | Fact / 13 |


### AccountDocumentTests

Localização: tests/GestaoComercial.UnitTests/AccountDocumentTests.cs.

| Método | Tipo / linha |
| --- | --- |
| ErrorDocumentDisplaysSafeMessageAndPreservesLocalReturn | Fact / 11 |


### AggregateMoneyTests

Localização: tests/GestaoComercial.UnitTests/AggregateMoneyTests.cs.

| Método | Tipo / linha |
| --- | --- |
| IntegerCentavosRemainExactBeyondDoublePrecision | Theory / 9 |
| AggregateOverflowOrFractionIsRejectedWithoutRounding | Theory / 11 |


### AuthenticationRulesTests

Localização: tests/GestaoComercial.UnitTests/AuthenticationRulesTests.cs.

| Método | Tipo / linha |
| --- | --- |
| ReturnUrlIsStrictlyLocal | Theory / 17 |
| IdentityHashIsSaltedVerifiableAndSupportsRehash | Fact / 19 |
| ExpirationHasExactBoundaries | Theory / 33 |
| OnlyApprovedRolesAreAccepted | Theory / 46 |


### CenarioMercadoTests

Localização: tests/GestaoComercial.UnitTests/CenarioMercadoTests.cs.

| Método | Tipo / linha |
| --- | --- |
| EmbalagensMaioresCustamMaisEProdutosTemPrecoPositivo | Fact / 7 |


### ConfigurationTests

Localização: tests/GestaoComercial.UnitTests/ConfigurationTests.cs.

| Método | Tipo / linha |
| --- | --- |
| InvalidConnectionHasSafeError | Theory / 15 |
| InvalidDatabaseRejected | Theory / 26 |
| ProductionDoesNotUseDevelopmentAlias | Fact / 29 |
| InvalidApiUrlRejected | Theory / 39 |
| EnvironmentFilePreservesExistingVariablesAndDoesNotExpandValues | Fact / 47 |


### ContactTests

Localização: tests/GestaoComercial.UnitTests/ContactTests.cs.

| Método | Tipo / linha |
| --- | --- |
| NormalizesContactWithoutCreatingCredentials | Fact / 6 |
| RejectsInvalidFields | Theory / 16 |


### CsvTests

Localização: tests/GestaoComercial.UnitTests/CsvTests.cs.

| Método | Tipo / linha |
| --- | --- |
| FormulaPrefixesAreNeutralized | Theory / 9 |
| QuotesSeparatorsNewlinesAndUnicodeRemainOneCell | Fact / 10 |


### CustomerDocumentTests

Localização: tests/GestaoComercial.UnitTests/CustomerDocumentTests.cs.

| Método | Tipo / linha |
| --- | --- |
| AcceptsValidOptionalDocuments | Theory / 7 |
| RejectsInvalidTypeOrDocument | Theory / 10 |


### DashboardRulesTests

Localização: tests/GestaoComercial.UnitTests/DashboardRulesTests.cs.

| Método | Tipo / linha |
| --- | --- |
| TicketIsExactWithSingleRounding | Theory / 8 |
| DefaultMonthUsesInstallationTimezoneAndEndIsExclusive | Fact / 9 |
| InvalidAndOverlongPeriodsAreRejected | Fact / 14 |


### HostTests

Localização: tests/GestaoComercial.UnitTests/HostTests.cs.

| Método | Tipo / linha |
| --- | --- |
| LivePublicReadyPrivateAndFallbackDenied | Fact / 44 |
| UnavailableMongoDoesNotAffectLiveness | Fact / 52 |
| ExceptionIsSafeProblemDetails | Fact / 62 |
| MissingConfigurationFailsAtStartupWithoutSecrets | Fact / 69 |
| TechnicalPagesArePublicAndSafe | Theory / 78 |
| OpenApiExposesCurrentContractsWithoutFutureModules | Fact / 85 |
| LoginFailsClosedWhenMongoIsUnavailable | Fact / 103 |
| CssIsServed | Fact / 111 |


### ProductRulesTests

Localização: tests/GestaoComercial.UnitTests/ProductRulesTests.cs.

| Método | Tipo / linha |
| --- | --- |
| NormalizesSkuAndPlainTextAndExactMoney | Fact / 10 |
| RejectsInvalidPrice | Theory / 12 |
| HttpRejectsInvalidScaleAndBounds | Theory / 14 |
| RejectsInvalidFields | Theory / 16 |
| RequiresCategoryAndValidOptionalBrand | Fact / 17 |
| ReencodesOnlyDecodedPixels | Theory / 20 |
| RejectsFakeOversizedCorruptAndLargeDimensions | Fact / 21 |
| RefusesClientPaths | Theory / 23 |


### SalesRulesTests

Localização: tests/GestaoComercial.UnitTests/SalesRulesTests.cs.

| Método | Tipo / linha |
| --- | --- |
| MonetaryTotalsAreExactAndBounded | Fact / 6 |
| ItemsMustBeDistinctValidAndLimited | Fact / 12 |
| StateTransitionsPreserveLifecycle | Theory / 17 |
| InvalidStatesNeverTransition | Theory / 19 |


### StockRulesTests

Localização: tests/GestaoComercial.UnitTests/StockRulesTests.cs.

| Método | Tipo / linha |
| --- | --- |
| DeltaPreservesExplicitOperationSign | Theory / 7 |
| InvalidMovementIsRejected | Theory / 9 |
| NegativeAndOverflowNeverProduceBalance | Fact / 10 |
| ReasonIsRequired | Theory / 18 |
| ReasonIsTrimmedAndBounded | Fact / 19 |

