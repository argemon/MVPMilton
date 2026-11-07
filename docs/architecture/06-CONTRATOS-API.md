# 06 — Contratos da API

## Convenções

Base `/api/v1`, JSON camelCase, UUIDs, datas ISO 8601 UTC, decimais JSON numéricos com até duas casas. UI formata R$ e pt-BR; API nunca recebe `12,50`. Campos desconhecidos são rejeitados nos comandos. Não aceitar `atorId`, total calculado, saldo ou perfil do cliente como autoridade.

Bearer opaco de sessão da API trafega somente entre BFF e API por TLS. A API não autentica por cookie. Endpoints de login são internos, limitados por rede e taxa; demais rotas exigem sessão ativa e policy, salvo liveness e readiness mínimo restrito à rede interna; páginas técnicas do00 são as exceções transitórias do módulo. [Permissões exatas](../requirements/05-MATRIZ-PERMISSOES.md).

Listagens: `page=1&pageSize=20`, máximo 100; `search` até 100 caracteres; filtros combinam por AND; ordenação explícita permitida, padrão nome normalizado+id para cadastro e data decrescente+id para eventos. Retorno `{ items, page, pageSize, totalItems }`. `ativo=true|false|todos`, padrão true nos cadastros. Histórico nunca desaparece com esse filtro.

Busca textual inicial por prefixo do nome normalizado; documento e SKU por igualdade normalizada. Não interpretar `search` como detalhes do armazenamento nem prometer busca por trecho arbitrário. Data de ordenação de movimentos é `ocorridoEm`; de vendas, `criadoEm`. Use id como desempate determinístico.

PUT exige `versao` no corpo; comandos de venda exigem `versaoEsperada`. Respostas de detalhe contêm `versao`. Versão ausente: 400; divergente: 409. UUID inexistente: 404. Validação de formato/negócio sem conflito: 400. Sem sessão: 401; sem permissão: 403; propriedade de venda fora do escopo resulta 404 para não revelar conteúdo.

## Endpoints de acesso e contas

| Método / caminho | Entrada → saída | Policy / status de sucesso |
| --- | --- | --- |
| POST `/auth/login` | `{email,senha}` → `{accessToken,expiraEm,usuario:{id,nome,perfil,deveAlterarSenha}}` interno | anônimo na rede interna; 200 |
| GET `/auth/me` | → usuário e expiração; não renova inatividade | autenticado; 200 |
| POST `/auth/logout` | sem corpo → sem conteúdo; revoga sessão atual | autenticado; 204 |
| POST `/auth/alterar-senha` | `{senhaAtual,novaSenha}` → sem conteúdo; revoga todas as sessões | autenticado; 204 |
| GET `/usuarios` e `/usuarios/{id}` | paginação → resumo/detalhe sem hashes | Usuarios.Gerenciar; 200 |
| POST `/usuarios` | `{funcionarioId,email,senhaTemporaria,perfil}` → detalhe | Usuarios.Gerenciar; 201 + Location |
| PUT `/usuarios/{id}` | `{perfil,ativo,versao}` → detalhe | Usuarios.Gerenciar; 200 |
| POST `/usuarios/{id}/redefinir-senha` | `{senhaTemporaria,versaoEsperada}` → 204, força troca e revoga sessões | Usuarios.Gerenciar; 204 |

Login inválido/inativo/bloqueado tem mesma resposta 401 e mensagem. Excesso de taxa retorna 429. Senha temporária é entrada sigilosa, nunca retornada ou registrada. Se troca obrigatória estiver pendente, só me, logout e alterar-senha são aceitos; demais operações retornam 403 `TROCA_SENHA_OBRIGATORIA`.

## Cadastros

| Recurso | GET lista/detalhe | POST criar | PUT `/{id}` |
| --- | --- | --- | --- |
| `/funcionarios` | filtros ativo/search | nome, email?, telefone? | mesmos + ativo, versao |
| `/clientes` | filtros ativo/search/tipo | nome, tipo, documento?, email?, telefone? | mesmos + ativo, versao |
| `/categorias`, `/marcas` | filtros ativo/search | nome | nome, ativo, versao |
| `/produtos` | filtros ativo/search/categoriaId/marcaId | sku, nome, descricao?, categoriaId, marcaId?, preco, estoqueMinimo | mesmos + ativo, versao |

GET usa policy de leitura; POST/PUT, de manutenção da matriz. POST retorna 201 com Location e detalhe; PUT 200. Detalhes incluem id, ativo, versão e metadados; produto inclui saldo informativo (não editável), referências e `imagemUrl` autenticada quando houver. SKU e documento duplicados: 409 com código específico. Documento deve conter apenas dígitos e passar validação PF/PJ quando informado.

- GET `/clientes/{id}/vendas`: policy Clientes.Historico; paginação e período opcional; vendedor vê somente vendas próprias. Resumo de venda com snapshots e estados.
- POST `/produtos/{id}/imagem`: Produtos.Gerenciar, multipart `arquivo` e `versao`; 200 detalhe. JPEG/PNG até 2 MiB e 4096×4096; 413 para tamanho, 415 para tipo.
- GET `/produtos/{id}/imagem`: Produtos.Ler, bytes com tipo validado; 404 sem imagem. BFF transmite bytes autenticados sem token em URL.
- DELETE `/produtos/{id}/imagem?versao=N`: Produtos.Gerenciar; 204. Exceção de DELETE para arquivo substituível, sem exclusão do produto/histórico.
- Não há DELETE de cadastros; inativação via PUT. Categoria/marca não podem ser inativadas se houver produto ativo vinculado (409).

## Estoque

| Endpoint | Request / response | Policy |
| --- | --- | --- |
| GET `/estoque/saldos` | filtros search, abaixoMinimo; página de produtoId, sku, nome, saldo, minimo, ativo | Estoque.Ler |
| GET `/estoque/movimentos` | produtoId?, tipo?, inicio?, fim?; página de id, delta, saldoAntes/Depois, motivo, ator, data, vendaId? | Estoque.Historico |
| POST `/estoque/entradas` | `{produtoId,quantidade,motivo}` → movimento | Estoque.Movimentar |
| POST `/estoque/saidas` | `{produtoId,quantidade,motivo}` → movimento | Estoque.Movimentar |
| POST `/estoque/ajustes` | `{produtoId,delta,motivo}` → movimento | Estoque.Ajustar |

POST manual exige `Idempotency-Key` e retorna 201 + Location do movimento. GET `/estoque/movimentos/{id}` retorna detalhe pela policy Historico. Gravar saldoAntes/Depois no movimento; diferença deve ser delta. Saída não pode ser usada como confirmação de venda. Motivo tem 10–500 caracteres; entrada/saída quantidade 1–1.000.000; ajuste delta não zero entre -1.000.000 e 1.000.000.

## Vendas

| Endpoint | Entrada → saída | Policy / status |
| --- | --- | --- |
| GET `/vendas` e `/vendas/{id}` | filtros clienteId, status, inicio, fim; resumo/detalhe com itens e snapshots | Vendas.Ler; 200 |
| POST `/vendas` | `{clienteId,itens:[{produtoId,quantidade}]}` → rascunho com preços atuais | Vendas.Criar; 201 + Location |
| PUT `/vendas/{id}` | `{clienteId,itens:[...],versao}` → rascunho; recalcula snapshots/preços explicitamente | Vendas.Editar; 200 |
| POST `/vendas/{id}/confirmacao` | `{versaoEsperada}` → venda confirmada | Vendas.Confirmar; 200 |
| POST `/vendas/{id}/cancelamento` | `{versaoEsperada,motivo}` → venda cancelada | Vendas.Cancelar; 200 |
| POST `/vendas/{id}/descarte` | `{versaoEsperada,motivo}` → rascunho descartado | Vendas.Editar; 200 |

POST criar, confirmar, cancelar e descartar exigem chave idempotente. PUT usa versão: retry cego pode gerar 409, então reler antes de decidir. Vendedor acessa/edita/confirma/descarta apenas próprias vendas; administrador, todas. Nenhum perfil edita venda confirmada. Preço nunca é aceito no request de item. Rascunhos com zero itens são permitidos. Exemplo: duas unidades cujo preço atual é 12.50 retornam item total 25.00 e total da venda 25.00.

## Dashboard e relatórios

- GET `/dashboard?inicio=2026-11-01&fim=2026-11-07`: Dashboard.Ler; retorna período/fuso, quantidadeConfirmada, quantidadeCancelada, receitaLiquida, ticketMedio e página de estoque baixo (máximo 20 no painel; link para lista completa).
- GET `/relatorios/vendas?inicio=...&fim=...&page=1&pageSize=20`: Relatorios.Vendas; linhas por venda confirmada no intervalo, status atual, total e contribuição líquida; totais de todo o filtro fora da paginação.
- GET `/relatorios/estoque?search=...&page=1&pageSize=20`: Relatorios.Estoque; saldo atual, mínimo, ativo e `geradoEm`; sem saldo retroativo no MVP.
- GET `/relatorios/movimentos?inicio=...&fim=...&produtoId=...`: Relatorios.Movimentos; mesmas informações do histórico, com totais de entrada/saída/delta do filtro.
- GET dos mesmos caminhos com `/exportacao` no final: mesmos filtros e policy; CSV UTF-8, valores decimais com ponto e delimitador `;`, datas ISO, até 10.000 linhas; acima disso 400 `EXPORTACAO_EXCEDE_LIMITE`, sem truncar. Corpo de erro segue JSON ProblemDetails.

Datas civis usam fuso da instalação (`America/Cuiaba` por H-04), início inclusivo e fim exclusivo, convertidos no servidor para UTC. Ambos obrigatórios para relatórios de vendas/movimentos; intervalo máximo 366 dias. Dashboard usa mês corrente quando omitidos. Vendas listadas usam data de criação; relatório comercial usa confirmação. Cancelamento posterior altera contribuição do período original: relatório é posição atual, não fechamento contábil. Fórmulas comerciais normativas em RF-17/RF-19; movimentos/estoque em RF-20.

## Idempotência

`Idempotency-Key` é UUID criado pelo BFF por intenção do usuário e preservado até resultado conhecido, incluindo retries. Escopo = método + rota normalizada, incluindo id. Hash inclui corpo normalizado e versão; ator vem da sessão. Chave igual/corpo diferente: 409 `CHAVE_REUTILIZADA`. A mesma operação concluída devolve status e corpo originais, após autenticar e revalidar policy atual; não gera novos efeitos.

A chave é gravada na mesma transação dos efeitos. DuplicateKey aborta a tentativa; consultar o recibo fora da transação abortada no primary/majority, com nova sessão de leitura se necessário. Só uma nova tentativa completa pode escrever após rollback conhecido; nunca continuar a transação abortada. Após espera limitada de 5 s, devolver 409 `OPERACAO_EM_ANDAMENTO`, sem declarar sucesso. Falhas de validação não são persistidas como sucesso. Estado/índices únicos reforçam que nova chave não causa nova baixa/estorno. Manter chaves de vendas e movimentos enquanto houver histórico; sem purga automática no MVP.

Resposta idempotente é um recibo do instante original: repetir confirmação após posterior cancelamento ainda retorna o recibo original da confirmação, sem transicionar o estado novamente. O BFF relê GET da venda para exibir o estado atual após resolver uma repetição. Diferenciar recibo de estado atual evita que retry antigo faça a UI apresentar Confirmada como situação corrente de uma venda Cancelada.

## Erros e exemplo concreto

Formato `application/problem+json`: `type`, `title`, `status`, `detail` seguro, `instance`, `code`, `traceId`, `errors` quando houver campo inválido.

```json
{
  "type": "urn:gestaocomercial:erro:estoque-insuficiente",
  "title": "Estoque insuficiente",
  "status": 409,
  "detail": "Há 1 unidade disponível para a quantidade solicitada de 2.",
  "instance": "/api/v1/vendas/00000000-0000-0000-0000-000000000001/confirmacao",
  "code": "ESTOQUE_INSUFICIENTE",
  "traceId": "identificador-de-correlacao"
}
```

Códigos estáveis adicionais: `VALIDACAO`, `DOCUMENTO_DUPLICADO`, `SKU_DUPLICADO`, `NOME_DUPLICADO`, `VERSAO_DESATUALIZADA`, `PRECO_ALTERADO`, `RECURSO_INATIVO`, `ESTADO_INVALIDO`, `ULTIMO_ADMINISTRADOR`, `SEM_PERMISSAO`. 500 é inesperado e genérico, 503 indisponibilidade transitória, 429 limite de uso. Não converter toda exceção em 400.

## Esclarecimentos propostos da revisão de módulos

ADR-15/17 detalham a baseline, sem alterar autenticação ou camadas:

- POST/PUT rascunho rejeita qualquer produto inativo com 409 RECURSO_INATIVO. Rascunho não reserva saldo; confirmação exige saldo incorporado e quantidade para todos. Ausência dá409 SALDO_INEXISTENTE, overflow de saldo/estorno409 SALDO_EXCEDE_LIMITE. Nada é parcialmente aplicado.
- Conflitos complementares de cadastro:409 EMAIL_DUPLICADO, VINCULO_OCUPADO, REFERENCIA_EM_USO. Falta de referência existente requerida por UUID dá404; referência inativa dá409. Erros de formato continuam400.
- GET usuários usa `search,ativo,page,pageSize`; resumo/detalhe administrativo inclui id/nome/email/funcionarioId/perfil/ativo/versao/deveAlterarSenha, sem senha/hash. GET me inclui usuario/expiraEm; login continua exclusivamente entre servidores. Conta bootstrap tem funcionário null; conta humana tem referência válida após 02.
- Health é exceção ao envelope de erro: live200 `{status:"ok"}`, ready200 `{status:"ready"}` ou 503 `{status:"unavailable"}`, sem diagnóstico. Rede mantém readiness privado.
- Período opcional deve trazer início/fim juntos ou omitir ambos; início<fim e <=366 dias. Dashboard padrão vai do primeiro dia do mês ao primeiro do seguinte no fuso. Exportação rejeita paginação e usa filtro completo.
- GET saldos/relatório estoque aceita `ativo=true|false|todos` (padrão true). Baixo estoque sempre exige produto ativo e ordena saldo/nome/id; painel retorna primeira página 20 com totalItems.
- DTOs concretos de detalhe/lista/indicador e componentes/rotas Web estão detalhados nos respectivos módulos00–10. Campo de metadado genérico usa createdAt/updatedAt; evento comercial usa criadoEm/confirmadoEm/ocorridoEm conforme recurso. Não retornar documentos BSON ou detalhes do driver indiscriminadamente.
- Totais de movimentos: entradas=soma de delta positivo, saídas=valor absoluto da soma negativa e delta=entradas-saidas, respeitando tipo/filtro. Exportações têm geradoEm do instante próprio; tela não congela consulta posterior.
- Endpoints HTTP do host Web: GET/POST `/conta/entrar`, POST `/conta/sair`, GET/POST `/conta/senha`, com antiforgery nos POSTs, redirect303 local no sucesso e sem token na resposta ao browser. GET `/downloads/relatorios/{tipo}` usa cookie, allowlist de tipos, policy e encaminha CSV ou erro seguro.

Superfície P2 proposta em módulo11/ADR-18 não integra `/api/v1` do MVP. Após aceite de Q-04, somente rotas públicas explicitamente permitidas são encaminhadas pelo BFF à API privada; API interna/DB continuam sem exposição pública geral. Não implementar endpoints/DTOs P2 antes da liberação.

## Contrato publicado e validação

Gerar OpenAPI da API, conferir exemplos/nullable/status e comparar mudanças em CI. Swagger UI apenas em desenvolvimento ou homologação restrita. `/health/live` público mínimo e `/health/ready` interno não contêm segredos. Frontend usa clientes tipados e contratos compilados compartilhados; incompatibilidade deve falhar em teste de contrato.

## Persistência MongoDB e compatibilidade do transporte

ADR-23 mantém os campos monetários HTTP existentes (preco, precoUnitario, total, receitaLiquida, ticketMedio e totalEstimado) como números decimais em **reais**, nunca números BSON/Extended JSON ou centavos disfarçados sob o mesmo nome. Na borda API, validar escala <=2 e limites, converter exatamente para long centavos; na saída dividir por 100m. UUIDs continuam strings UUID, datas ISO UTC. Nenhum ObjectId, filtro MongoDB ou operador recebido do cliente é usado como autoridade. JSON de comando rejeita campos desconhecidos como antes.

Conversão 12.50 →1250→12.50; 12.505 dá400 VALIDACAO. Total calculado internamente e snapshots em centavos. CSV mantém reais decimais com ponto, sem mudar unidade dos relatórios. Versao é versão de negócio, não guardVersion. Registro/commit MongoDB incerto dá503 RESULTADO_DESCONHECIDO com Retry-After e repetição da mesma Idempotency-Key, sem afirmar rollback ou criar nova intenção. Limite de representação de agregado dá400 LIMITE_MONETARIO_EXCEDIDO. OpenAPI/cliente/tests de conversão devem refletir esses limites na implementação; rotas, policies, status de sucesso e escopo dos módulos permanecem iguais.

Os nomes físicos users/employees/customers/categories/brands/products/sales/stock_movements não renomeiam rotas/DTOs em português. API mapeia explicitamente employeeId persistido para funcionarioId de transporte, preço em centavos para preco em reais e saldo incorporado para o saldo informativo existente. Nenhum documento de persistência é retornado diretamente.

## Implementação05 —07/11/2026

RF-09–12 implementados conforme contratos acima, sem sales. Evolução explícita05-stock/version6 acrescenta movimentos manuais/recibos e readiness exige05. Evidências, inventário e gates no [relatório de entrega](../execution/10-ENTREGA-ESTOQUE-VENDAS.md). Execução06 somente após gate05 integral;07 não autorizado.

## Incremento06

GET /vendas retorna página de `VendaResumo={id,clienteId,clienteNome?,criador,status,total,versao,criadoEm}`, projetada no servidor sem itens/documento do cliente. GET /vendas/{id} e respostas de comandos retornam VendaDetalhe conforme M06. GET /clientes/{id}/vendas passa a usar resumos reais filtrados pelo criador para Vendedor. Campos monetários seguem reais na borda e centavos long internamente. POST criar/confirmar/cancelar/descartar exigem Idempotency-Key UUID e devolvem recibo original; PUT usa versão. Movimentos acrescentam vendaId/itemId UUID nullable. Códigos503 incluem PERSISTENCIA_NAO_INICIALIZADA e RESULTADO_DESCONHECIDO. Nenhuma rota07/08 foi acrescentada.

## Incremento07 — ADR-36

GET /api/v1/dashboard?inicio&fim, policy Dashboard.Ler, somente Administrador. Datas opcionais juntas, padrão mês local, [inicio,fim), máximo366 dias. DashboardResponse contém resumo comercial, contagem de produtos cadastrados/ativos, estoqueBaixo página1/20/totalItems, recentes10, evolução diária e maisVendidos10. Campos completos em M07 e contrato compilado Dashboard.cs. Snapshot único; valores HTTP em reais, Cancelada contribui zero. Sem dados retorna200 com zeros/listas vazias (dias preenchidos). 400 VALIDACAO/LIMITE_MONETARIO_EXCEDIDO;401/403;503 persistência;500 seguro. Nenhuma informação de documento pessoal/token/driver.

## Incremento08 — ADR-36

GET /api/v1/relatorios/{vendas|operadores|produtos|estoque|movimentos} e cada /exportacao; páginas /relatorios/{tipo}; handler /downloads/relatorios/{tipo}. Vendas/operadores/produtos somente Administrador; estoque/movimentos Administrador/Estoquista. Período obrigatório exceto estoque atual; filtros/ordenações e DTOs mínimos detalhados em M08. Nome histórico do criador identifica funcionário/operador, sem inferir employeeId do cadastro atual. Contagens/totais/linhas no mesmo snapshot, sem coleção duplicada. CSV UTF-8 BOM delimitado por ponto e vírgula, até10.000 linhas sem paginação;400 EXPORTACAO_EXCEDE_LIMITE antes de qualquer arquivo. Cancelada preserva totalOriginal e contribuição zero. Estoque não aceita data histórica. Sem PDF. Request inválido400;401/403;503 persistência;500 seguro. Nenhuma alteração de schema prevista.
