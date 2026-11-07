# M11 — Catálogo e pedidos online

## 1. Identificação, objetivo e estado

**Bloco 11, P2, posterior ao MVP, NÃO liberado para código.** Esta revisão especifica uma **decisão proposta** de menor complexidade (ADR-18): catálogo público e solicitação de pedido sem pagamento, reserva ou conversão automática em venda. Valor: receber interesse comercial para atendimento interno, preservando integridade do MVP. Não substituir ADRs aprovadas nem tratar Q-04 como respondida pelo usuário.

## 2. Pré-requisitos e dependências concluídas

Normas transversais: arquitetura05/09 e ADR-20–29, MongoDB.Driver oficial restrito à infraestrutura, transações sequenciais em topologia compatível e testes MongoDB reais isolados (Atlas ou perfil Testcontainers explícito) conforme ADR-28. Classes/serviços de módulo futuro não são pré-requisitos ocultos.

00–10 aprovados e pedido explícito de iniciar implementação da fase posterior. Antes de codificar, responsável aceita/ajusta ADR-18, RF-21–24/RN-21–24 propostos e registra Q-04; define tratamento do contato público em Q-03. A arquitetura API/BFF existente é reutilizada. Nenhum gateway/serviço futuro é necessário para a proposta mínima. Se aprovada integralmente, basta registrar aceite, não inventar novo mecanismo.

## 3. Requisitos e prioridades

RF-21 P2 publicar/despublicar produto; RF-22 P2 consultar catálogo; RF-23 P2 enviar solicitação anônima com recibo; RF-24 P2 listar/detalhar/encerrar solicitação internamente. Todos **propostos**, fora de RF-01–20 e fora do gate MVP. RNF-01/02/03/04/07/08/11/12 continuam aplicáveis. RN-01/02/07/08/15/19 e RN-21–24 propostos. Aceites locais M11-CA01–06, sem renumerar CA-01–18.

## 4. Escopo proposto e exclusões

Dentro: publicação explícita por produto ativo, nome/SKU/descrição/preço/imagem pública segura, solicitação anônima com nome/telefone/itens, preço estimado em snapshot, protocolo, triagem interna e encerramento motivado. Fora: conta de consumidor, carrinho persistido, pagamento/fiscal/financeiro, endereço/frete/entrega, reserva/baixa/estorno de pedido, ligação automática com Venda, email/WhatsApp e acompanhamento público de pedido.

Atendimento pode posteriormente criar venda pelo fluxo interno já existente, se houver cliente e estoque; isso é decisão humana separada, sem vínculo/automação exigidos neste bloco. Não apresentar pedido como venda confirmada ou valor como pagamento.

## 5. Atores e permissões propostos

Visitante anônimo: somente catálogo/imagem publicados e envio de solicitação, sujeito a rate limit. Administrador: Catalogo.Gerenciar e Pedidos.Gerenciar (novas policies propostas com os mesmos perfis fixos). Vendedor/Estoquista não triam/publicam no recorte mínimo. Anônimo nunca acessa `/api/v1` interno, saldo, histórico, dados de cliente ou detalhe de pedido. Endpoint público de API é roteado pelo BFF/proxy com allowlist explícita; rede da API interna continua privada e sem CORS amplo.

## 6. Entidades e evoluções de persistência propostas

Somente após liberação P2: incorporar publicacao {publicado:false,versao,atorId,updatedAt} em products; criar `online_orders` com _id UUID, protocolo UUID unique, chavePublicaHash unique, hashRequest, nome 2–150, telefone 10–15, estado Recebido/Encerrado, totalEstimadoCentavos Int64, reciboOriginal público imutável, timestamps/metadados/versao e até 100 itens incorporados com snapshots e valores Int64. Sem coleções de itens/publicações nem referência relacional. Índices estado/criadoEm/_id, validadores de estado/limites; contato privado e retenção Q-03. Publicação não muda ativo/preço/saldo e exige Produto ativo. Sem exclusão física, reserva, movimento ou Venda automática.

## 7. Fluxos principais e alternativos propostos

1. Administrador escolhe produto ativo → publica com versão da publicação → catálogo público passa a exibir. Despublicar ou inativar produto remove do público imediatamente em novo GET.
2. Visitante consulta página/detalhe e informa nome/telefone/itens → POST ao BFF público → API valida produtos ativos/publicados, quantidades e preço atual sob escritas de guarda → grava pedido/itens/auditoria técnica e recibo no mesmo commit, sem tocar saldos/movimentos.
3. Resposta perdida → repetir mesma Idempotency-Key e mesmo corpo recupera recibo mínimo; corpo diferente 409. Dedupe público usa chave aleatória não associada a usuário interno, sem criar sessão de consumidor.
4. Administrador abre lista de Recebidos → detalhe privado → atende externamente sem integração automática → encerra com motivo e versão; mantém solicitação original e snapshots.
5. Produto despublicado/inativo durante envio → 409, pedir atualização; nenhum pedido parcial. Falta de estoque não bloqueia solicitação, pois não é confirmação; UI informa “Disponibilidade e valor final sujeitos à confirmação interna”.
6. Não existe GET público de pedido, evitando expor contato por enumeração de protocolo; recibo é mostrado uma vez e em repetição idempotente sem dados pessoais.

## 8. Regras e validações propostas

RN-21: público só vê ativo+publicado; nunca saldo/contatos internos. RN-22: pedido é solicitação Recebido→Encerrado, não reserva/consome estoque, sem conversão automática. RN-23: preço estimado vem do servidor, até 100 itens distintos, quantidade inteira1–1.000.000, total no limite RN-01 em centavos Int64 internos; HTTP em reais com até duas casas, sem desconto/financeiro. RN-24: dedupe público, limites de abuso e triagem autorizada com versão/motivo preservam rastreabilidade.

Nome/telefone obrigatórios; telefone normalizado, sem email/CPF/endereço. Não aceitar preço/status/ator/total do visitante. Motivo de encerramento10–500. Limite proposto:10 submissões por 10 min por IP e 60 consultas/min por IP, tamanho do corpo64 KiB, sem CAPTCHA/dependência externa inicial. Contar IP somente do proxy confiável; registrar hash/retencão técnica conforme Q-03, sem prometer eliminação completa de abuso.

## 9. Endpoints REST propostos

Não são contratos liberados do MVP. Superfície pública externa via BFF com rotas equivalentes encaminhadas à API privada; interno usa sessão BFF existente. PublicId de produto é o UUID existente; não adicionar sistema de slug nesta proposta.

| Método / rota API | Request | Response | Autorização / erros |
| --- | --- | --- | --- |
| GET `/api/public/v1/catalogo` | `search,page,pageSize` até 100 | 200 página `{id,sku,nome,descricao,preco,imagemUrl?}` sem saldo | Anônimo; 400/429/503 |
| GET `/api/public/v1/catalogo/{id}` | UUID | 200 detalhe público | Anônimo; 404 para inativo/não publicado; 429 |
| GET `/api/public/v1/catalogo/{id}/imagem` | UUID | 200 bytes seguros | Anônimo; 404 se oculto/sem imagem; 429 |
| POST `/api/public/v1/pedidos` | Idempotency-Key UUID; `{nome,telefone,itens:[{produtoId,quantidade}]}` | 201 `{protocolo,status:"Recebido",totalEstimado,criadoEm}` sem contato/itens; sem Location público pois não há GET público | Anônimo; 400 campos/chave; 409 produto indisponível/chave; 413 corpo; 429; 503 |
| GET `/api/v1/catalogo/publicacoes` | `search,page,pageSize` | 200 página de produtoId/publicado/versao, ausência representada por false/0 | Catalogo.Gerenciar; 401/403 |
| PUT `/api/v1/catalogo/publicacoes/{produtoId}` | `{publicado,versao}`; primeira publicação usa0 | 200 `{produtoId,publicado,versao}` | Catalogo.Gerenciar; 400; 404 produto; 409 versão/inativo |
| GET `/api/v1/pedidos-online` | `status?,inicio?,fim?,page,pageSize` | 200 página protocolo/status/nome/criadoEm/totalEstimado/versao | Pedidos.Gerenciar; 400/401/403 |
| GET `/api/v1/pedidos-online/{id}` | UUID interno | 200 detalhe com contato/itens/snapshots/metadados | Pedidos.Gerenciar; 401/403/404 |
| POST `/api/v1/pedidos-online/{id}/encerramento` | `{versaoEsperada,motivo}` | 200 detalhe Encerrado | Pedidos.Gerenciar; 400/401/403/404; 409 estado/versão |

Erros JSON ProblemDetails seguros; recibo público não devolve dados pessoais nem resposta de outro corpo. Chave pública é capacidade de dedupe, não credencial de consulta; usar UUID aleatório, hash no armazenamento, sem logs do header/corpo. No BFF, POST `/pedido` de formulário SSR usa antiforgery; API pública não usa cookie. Catálogo/imagem públicos usam cache sem armazenamento compartilhado no recorte inicial (Cache-Control no-store), para não continuar publicando inativado por cache antigo.

## 10. Páginas, componentes e estados propostos

| Rota | Componentes propostos | Vazio / carregando / sucesso / erro |
| --- | --- | --- | --- |
| `/catalogo` | CatalogoPublico, FiltroCatalogo, Paginacao | Nenhum produto publicado; carregando; lista; indisponibilidade |
| `/catalogo/{id}` | ProdutoPublico, QuantidadeSolicitada | Produto não disponível; carregando; detalhe/preço estimado; 404 seguro |
| `/pedido` | FormularioSolicitacao SSR, ItensSolicitados, ReciboPedido | Sem itens; enviando; protocolo e aviso sem venda/pagamento; validação/429/retry mesma chave |
| `/administracao/catalogo` | ListaPublicacoes, EditorPublicacao | Sem produtos elegíveis; carregando; publicação salva; conflito pede reler |
| `/pedidos-online`, `/pedidos-online/{id}` | ListaPedidos, DetalhePedido, EncerramentoPedido | Nenhum recebido; carregando; detalhe/encerrado; 401/403/409 |

Estado dos itens só no formulário atual, sem carrinho persistido obrigatório. Contatos não ficam em URL/localStorage. Sucesso público não expõe detalhes internos nem promete estoque disponível.

## 11. Erros e concorrência propostos

Publicação e edição/inativação de produto escrevem o mesmo documento products e comparam versão pertinente; primeira publicação usa filtro publicacao ausente ou publicacao.versao=0 e update condicional, sem índice de coleção separada. POST de pedido escreve guardVersion de produtos por UUID, revalida ativo+publicado+preço no snapshot e insere pedido/recibo/auditoria na mesma transação, sem escrever saldo nem movimentos. Dedupe público usa índice unique em chavePublicaHash e comparação do hashRequest em online_orders, sem ator fictício nem recibo comercial por usuário. reciboOriginal={protocolo,status:Recebido,totalEstimadoCentavos,criadoEm} é incorporado e imutável; o hash inclui método/rota pública/corpo canônico. Após encerramento, retry devolve o mesmo recibo Recebido convertido para reais, nunca o status atual Encerrado. Corpo divergente dá409. Encerramento por estado+versao preserva motivo original. Nenhuma operação paralela ou serviço externo dentro da transação. Estoque 0 permite solicitação sem garantia de atendimento.

## 12. Testes unitários e integração propostos

Unidade: visibilidade, entrada/quantidades/dinheiro, estados, dedupe/recibo e política de triagem. Integração MongoDB: publicação/ocultação e produto inativo, corrida produto/publicação/pedido, dedupe paralelo e corpo divergente, snapshot/preço estimado, zero alteração em saldo/movimentos, acesso público sem PII, sessão interna/403, encerramento concorrente, rate limit/413/CSRF do BFF e imagem privada não publicada. Sem mocks de gateway porque não há gateway.

## 13. Given/When/Then propostos

| ID | Given | When | Then |
| --- | --- | --- | --- |
| M11-CA01 | Produto ativo não publicado | Visitante lista/consulta por id | Não aparece e detalhe404; sem vazamento de saldo |
| M11-CA02 | Produto publicado, saldo 0 | Enviar solicitação válida | 201 Recebido com aviso; saldo 0 e nenhum movimento |
| M11-CA03 | POST concluído e resposta perdida | Repetir mesma chave/corpo | Mesmo recibo inclusive após encerramento, um pedido; corpo diferente409 |
| M11-CA04 | Produto despublicado/inativado antes do envio | POST incluindo produto | 409, nenhum pedido parcial |
| M11-CA05 | Pedido Recebido e dois gerentes | Encerrar concorrentemente | Uma mudança, outro409, snapshots/motivo preservados |
| M11-CA06 | Visitante e vendedor interno | Tentar GET detalhe interno ou publicar | 401 visitante/403 vendedor, nenhum contato público |

## 14. Checklist de execução condicionado à liberação

- [ ] Registrar aceite/ajustes de ADR-18, RF-21–24/RN-21–24 e Q-04/Q-03 antes de código.
- [ ] Confirmar 00–10 concluídos e nenhuma alteração dos gates do MVP.
- [ ] Criar evoluções de persistência do 11 somente agora; revisar escritas de guarda/validadores e filtros condicionais; testar base limpa e upgrade 10→11.
- [ ] Implementar DTOs públicos separados, BFF allowlist/SSR, policies e telas sem expor API interna.
- [ ] Executar `dotnet restore`, `dotnet build --no-restore -c Release`, `dotnet test -c Release --no-build` com MongoDB isolado.
- [ ] Demonstrar M11-CA01–06, rate limit e zero mutação de estoque; atualizar OpenAPI/estado e evidências.

## 15. DoD e encerramento

Após liberação de escopo: DoD global mais todos os RF/RN propostos aceitos e cenários locais comprovados; catálogo não vaza dados e solicitação não cria venda/baixa. Não há bloco 12 previsto. Antes da aceitação da proposta, estado permanece **especificado, não liberado**, sem alegar dependência de implementação futura: faltam decisões externas de escopo, não um serviço de software posterior.
