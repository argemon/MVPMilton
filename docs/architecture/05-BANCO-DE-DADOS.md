# 05 — Banco de dados

> Atualização vigente00 — ADR-27–29: implementação autorizada com Atlas já configurado em .env, sem sobrescrever segredos. Development reutiliza MONGODB_URI; Production/Staging exigem configuração externa específica. Integração pode executar transações reais em bancos efêmeros exclusivos no mesmo cluster ou perfil Testcontainers explícito; não criar outro MongoDB quando Atlas atende. rs0 é nome apenas do perfil local, não do Atlas. As exigências universais de Docker/rs0, somente Markdown e adaptação ainda não autorizada nas seções históricas anteriores ficam superadas nesses pontos.


## Tipos e convenções

MongoDB e MongoDB.Driver oficial substituem a persistência anterior (ADR-20–25). Linha de referência MongoDB 8.0 em replica set; patches de servidor, driver, Testcontainers e Database Tools serão fixados após verificar suporte/compatibilidade na adaptação futura do 00. Não atribuir a versão .NET ao driver. Este documento conserva o nome histórico BANCO-DE-DADOS e passa a especificar documentos, coleções e transações.

Bancos lógicos distintos: gestao_comercial (API/negócio/contas) e gestao_web (tickets BFF). Usuários distintos e sem leitura cruzada. IMongoClient singleton por configuração/credencial, com pool reutilizado; IMongoDatabase/IMongoCollection obtidos dele. Sessões IClientSessionHandle são curtas por operação e nunca concorrentes. BSON, filtros, serializers, classes de persistência e driver ficam em Infrastructure ou persistência técnica do Web, sem contaminar Domain/Application/Contracts.

_id de entidades e referências: Guid/UUID BSON Binary subtype 4 (representação Standard fixada no mapeamento), preservando UUID textual HTTP; controle singleton e catálogo de evoluções usam _id técnico string explícito, sem exposição comercial. Campos camelCase; instantes C# DateTimeOffset UTC mapeados explicitamente para BSON Date UTC com precisão de milissegundos; não usar tempo como token de concorrência. Datas civis permanecem datas de transporte e são convertidas pelo fuso da instalação. Quantidades BSON Int32; versões/guardVersion e centavos BSON Int64, nunca double/Decimal128 para dinheiro persistido.

Todo documento persistido possui schemaVersion BSON Int32; o dicionário pode abreviar esse campo. Documentos de negócio mutáveis possuem também createdAt/updatedAt UTC e versao Int64 inicial 1, update por _id+versao esperada e incremento efetivo. Documentos técnicos usam os campos temporais/de concorrência explícitos de suas linhas: sessions/tickets têm expiração/estado; administration_control tem guardVersion; persistence_versions tem appliedAt/checksum, sem versão comercial fictícia. guardVersion técnico separado evita expor mudanças de coordenação como edição de cadastro; saldo incorporado tem sua própria versão. Nunca substituir documento products inteiro em edição de cadastro. Nenhum histórico ilimitado incorporado; limite de projeto de 1 MiB BSON por documento, verificado antes da escrita, bem abaixo do limite do servidor. Pedidos/vendas até 100 itens, snapshots com tamanhos normativos; binários de imagens ficam fora do MongoDB.

Dinheiro: BRL em centavos long; preço 1–999.999.999 centavos (R$ 9.999.999,99), total de venda/item <=99.999.999.999.999 centavos (R$ 999.999.999.999,99). Multiplicar quantidade e somar com checked e verificar limites; overflow nunca grava parcial. HTTP mantém decimal em reais, com conversão exata na borda: 12.50 ↔ 1250 centavos, rejeitando valor com fração de centavo. Divisão para média usa quociente/resto e AwayFromZero uma única vez no centavo final. Agregações de muitos totais usam acumulação exata com limite explícito e BigInteger na aplicação quando necessário; não usar $sum que possa promover overflow Int64 a double. Resultado monetário HTTP deve caber no decimal do contrato; excedente retorna 400 LIMITE_MONETARIO_EXCEDIDO, sem arredondamento/truncamento.

## Dicionário mínimo

Campos abaixo são obrigatórios salvo ?; timestamps/versao/schemaVersion seguem a convenção acima para mutáveis. Null/ausente em campos opcionais é normalizado pelo mapeamento; validadores aceitam apenas os casos explicitados. Nomes de coleções são escolhas técnicas, endpoints e linguagem de negócio permanecem em português.

| Coleção / banco | Documento e obrigatoriedade | Incorporação, referência e índices |
| --- | --- | --- |
| employees / comercial | _id, nome, nomeNormalizado, email?, telefone?, ativo, metadados, guardVersion | Sem array de usuários/vendas; índice nomeNormalizado/_id; usuários referenciam este UUID |
| users / comercial | _id, nomeExibicao, nomeNormalizado, email, emailNormalizado, passwordHash Identity, perfil fixo, ativo, deveAlterarSenha, securityStamp, failedAccessCount, lockoutEnd?, employeeId?, guardVersion, metadados | Email normalizado unique inclusive inativos; employeeId unique parcial para BSON binData (UUID Standard validado no backend) (null/ausente fora do índice), só em 02; bootstrap é exceção de vínculo |
| customers / comercial | _id, nome, nomeNormalizado, tipo PF/PJ, documento?, email?, telefone?, ativo, metadados, guardVersion | Documento normalizado unique parcial quando string; busca por prefixo nomeNormalizado/_id; não incorporar vendas |
| categories e brands / comercial | _id, nome, nomeNormalizado, ativo, metadados, guardVersion | Coleções distintas: ambos têm CRUD e unicidade própria exigidos; unique nomeNormalizado, inclusive inativos |
| products / comercial | _id, sku, skuNormalizado, nome, nomeNormalizado, descricao?, categoriaId, marcaId?, precoCentavos, moeda BRL, estoqueMinimo, ativo, saldo{quantidade,versao}, imagem?{chave,mime,tamanhoBytes}, metadados, guardVersion | Saldo/imagem incorporados, SKU unique, índice nomeNormalizado/_id e categoriaId/ativo/_id e marcaId/ativo/_id; sem coleção de saldo/imagem; estoque baixo usa projeção/expressão saldo<=mínimo, validar plano |
| sales / comercial | _id, clienteId, criadorId, status, itens[], totalCentavos, moeda BRL, criadoEm, confirmadoEm?, canceladoEm?, descartadoEm?, clienteSnapshot?, confirmacao?, cancelamento?, descarte?, metadados | Itens incorporados <=100: id, produtoId, quantidade, precoUnitarioCentavos, totalCentavos, skuSnapshot, nomeSnapshot. Snapshots e operadores congelados; índices confirmadoEm/status/_id, clienteId/criadoEm/_id, criadorId/criadoEm/_id |
| stock_movements / comercial | _id, produtoId, delta, saldoAntes, saldoDepois, tipo, motivo, atorId, atorNomeSnapshot, ocorridoEm, operacaoId? ou vendaId?/itemId? | Imutável, sem incorporação em products/sales. Índice produtoId/ocorridoEm/_id; unique parcial operacaoId BSON binData (UUID Standard) para manual; em 06 unique parcial vendaId/itemId/tipo para baixa/estorno |
| idempotency_receipts / comercial | _id, atorId, escopo, chave UUID, hashRequest, recursoId? enquanto Preparando e obrigatório quando Concluido, estado interno Preparando/Concluido, statusHttp?/resposta? obrigatórios quando concluído, criadoEm | Unique (atorId,escopo,chave), índice criadoEm; resposta sem credenciais; sem TTL/purga automática comercial |
| audit_events / comercial | _id, atorId? somente anônimo explícito, origem, acao, recursoTipo/id, ocorridoEm, traceId, detalhes mínimos | Imutável; índice recursoTipo/recursoId/ocorridoEm/_id; sem senha/token/payload livre |
| api_sessions / comercial | _id, usuarioId, tokenHash SHA-256 binário de32 bytes, securityStamp, criadoEm, expiraEm, ultimoUsoEm, revogadaEm? | Token hash unique, usuário/expiraEm para revogação; TTL opcional de sessão expirada somente como limpeza, nunca autoriza nem substitui verificação de expiração |
| administration_control / comercial | _id fixo, guardVersion Int64 | Um documento técnico criado em 01; toda mutação de conta/vínculo o altera efetivamente, inclusive bootstrap |
| tickets / web | _id UUID, handleHash SHA-256 binário de32 bytes, ticketProtegido, tokenApiProtegido, expiraEm, metadados técnicos | handleHash unique; índice expiraEm, TTL opcional só para limpeza; nunca token bruto nem conteúdo de negócio |
| persistence_versions / ambos | _id da evolução, checksum, appliedAt, versão alvo | Registro imutável de evolução concluída, índice único _id; 00 cria somente mecanismo/coleção técnica |

Índices compostos usam direções compatíveis com ordenação estável e _id como desempate. Busca por prefixo normalizado é escapada/ancorada, sem regex arbitrária recebida do cliente. Filtros por proprietário precedem contagem/paginação. Índices adicionais exigem explain e ensaio de volume. Sem índices para todos os campos por conveniência.

## Integridade adicional

Validação do servidor strict/error: $jsonSchema para campos/tipos/bounds/tamanho de strings/arrays/enums e expressões $expr compatíveis para relações internas. Exigir Int64 nos centavos/versões e Int32 nas quantidades; proibir saldo negativo, delta zero, preço inválido e estado inconsistente. Validar soma de itens/total, mínimo de1 item em Confirmada/Cancelada (0–100 em Rascunho/Descartada), produtoId distinto dentro de sales.itens e saldoDepois=saldoAntes+delta com expressão e aplicação, testando tentativa de escrita inválida direta. Índice multikey unique não garante produto único dentro de um mesmo array: não usá-lo como substituto desta validação. Runtime não possui bypassDocumentValidation.

Movimentos: Entrada positiva, SaidaManual negativa, Ajuste assinado não zero, Venda negativa e EstornoVenda positiva. Manuais exigem operacaoId, sem vendaId/itemId; comerciais exigem vendaId/itemId, sem operação manual. Índices parciais usam filtros suportados de tipo/campos existentes e sinal/tipo validado, não operadores não suportados presumidos. Todas as referências produto/ator/venda/item são verificadas na mesma sessão/transação; MongoDB não fornece FK ou cascade. Autoria precisa de usuário existente e autenticado, salvo evento anônimo previsto.

Rascunho/Descartada não possuem confirmação/cancelamento; Descartada exige data/ator/motivo. Confirmada exige confirmação e proíbe cancelamento; Cancelada conserva confirmação e exige cancelamento/motivo. Itens confirmados/cancelados, movimentos e auditoria são preservados. Somente itens de rascunho e imagem substituível admitem remoção delimitada; cadastros usam ativo, sem exclusão física. Runtime tem find/insert e apenas updates estritamente necessários por coleção; stock_movements/audit_events sem update/remove. Validador não impede reescrita de histórico por operador privilegiado: RBAC, backend, logs e operação controlada completam a proteção.

Categoria/marca podem ser desativadas só sem produto ativo. Nenhuma coleção contém listas crescentes de referências inversas. Vínculo único conta/funcionário é índice parcial e validação transacional; inativar preserva referência/autoria. Imagens são privadas em volume e trocadas fora da transação com metadado versionado e limpeza posterior ao commit.

## Protocolo transacional obrigatório

Toda alteração de múltiplos documentos que precisa atomicidade usa a mesma IClientSessionHandle e o mesmo IMongoClient, ReadConcern.Snapshot, WriteConcern.WMajority e ReadPreference.Primary. Requer replica set, nunca standalone. Coleções/índices existem antes da operação. Sem chamadas paralelas, Task.WhenAll, HTTP, arquivo ou espera de usuário dentro da transação. Portas da Application recebem comandos de caso de uso e não recebem BSON/sessões; infraestrutura coordena a sessão explicitamente, sem Generic Repository ou Unit of Work genérica.

Snapshot não é serializável e leitura de referência sozinha pode estar obsoleta. Dependência de ativo/preço/contagem é protegida com alteração efetiva do documento de referência, por exemplo incremento guardVersion, antes da decisão dependente. Update que não muda valor não serve como guarda. Edição/inativação concorrente escreve o mesmo documento: WriteConflict aborta uma tentativa, e a decisão é reavaliada com snapshot novo. Versão de negócio e guarda técnica são distintas. Ordem determinística reduz conflitos, não simula SELECT FOR UPDATE.

Ordem comercial: chave idempotente → sale existente quando aplicável → customer → products por UUID crescente. Produto incorpora saldo, não existe segunda coleção para bloqueá-lo. No cadastro: categories antigas/novas ordenadas → brands ordenadas → product. Inativar referência altera o mesmo guardVersion antes de consultar produtos ativos; atualizar vínculo escreve as referências e revalida. Ordem administrativa: administration_control → users ordenados → employees ordenados → api_sessions; bootstrap/criar/ativar/rebaixar/desativar/redefinir senha e vínculo compartilham esse controle. Contagem de Admins ocorre depois da escrita efetiva do controle e é reavaliada após conflito.

### Criar/editar rascunho

POST reserva UUID em memória, valida chave e referências, incrementa guardVersion efetivamente em customer/products, valida ativo/preço no snapshot e insere sales com itens incorporados e recibo. PUT usa filtro _id+status Rascunho+versao, valida propriedade, substitui apenas itens/cliente/snapshots de rascunho e incrementa versão. Nenhuma reserva/baixa de estoque; produto inativado após commit impedirá confirmação posterior.

### Confirmar

1. Autenticar e validar policy/propriedade/formato. Consultar recibo por ator/escopo/chave; se concluído e hash igual devolver original, sempre após autorização atual. Caso novo, registrar chave na transação; registro em preparação é visível somente na transação, pode não ter resposta ainda e nunca é commitado. Antes do commit o coordenador deve conferir recibo completo/Concluido; validadores exigem status e resposta quando Concluido. Qualquer exceção antes disso aborta a transação inteira, sem deixar pendência persistida.
2. Obter sale com estado Rascunho e versão esperada; alteração condicional efetiva da venda serializa comandos sobre o mesmo documento. Zero correspondência distingue 404/409, sem efeito parcial.
3. Guardar customer e products, validar ativo, itens 1–100 e preços iguais aos snapshots; preço alterado dá PRECO_ALTERADO até PUT explícito.
4. Para cada product, update atômico condicionado a saldo existente e saldo.quantidade >= quantidade, decrementando com $inc e incrementando saldo.versao. Conferir resultado de cada update; zero correspondência aborta tudo com erro apropriado. Nunca converter ausência em saldo zero nem criar saldo por upsert.
5. Gravar snapshots finais, totais exatos/estado/metadados/versao em sales, uma baixa stock_movements por item, audit_events e recibo completo. Commit único. Falha de qualquer etapa aborta venda inteira, inclusive baixas anteriores.

### Cancelar

Somente Confirmada com motivo e policy. Ordem/mesma sessão; atualização de sale por estado+versao e produtos por UUID. Somar quantidades originais com filtro saldo.quantidade <= int.MaxValue-quantidade e saldo existente, sem exigir ativo. Registrar EstornoVenda único por item, manter baixa/snapshots e metadados originais; estado Cancelada/auditoria/recibo no mesmo commit. Nova chave em Cancelada dá ESTADO_INVALIDO, sem estorno; mesmo recibo não muda estado atual.

### Movimentos manuais

Transação com chave, product ativo, saldo incorporado obrigatório, filtro de quantidade mínima para saída e capacidade máxima para entrada/ajuste positivo. Atualizar saldo e sua versão e gravar movimento, auditoria e recibo juntos. Ajuste é delta justificado, não saldo absoluto. Soma dos movimentos concilia com saldo criado zero; corrigir somente via novo ajuste autorizado.

## Concorrência e falhas

- Update de cadastro/rascunho com versão antiga: 409 VERSAO_DESATUALIZADA. Resultado sem correspondência não é sucesso. A regra de permissão/propriedade continua no backend.
- TransientTransactionError: abortar e repetir transação inteira em sessão válida com snapshot novo e mesma intenção/chave; no máximo duas novas tentativas e orçamento total de 5 s por tentativa HTTP, backoff curto com jitter e cancelamento. Não repetir somente decremento/insert isolado. Usar política explícita alinhada aos rótulos do driver, sem combinar retries ilimitados do helper com outro laço externo.
- UnknownTransactionCommitResult: repetir somente commit da mesma transação enquanto orçamento permitir. Não iniciar novo corpo nem declarar rollback conhecido. Persistindo dúvida, responder 503 RESULTADO_DESCONHECIDO com Retry-After e instrução de resolver pela mesma chave; repetição consulta recibo persistido e evita duplicidade. Cancelamento/time-out depois de commit pode esconder sucesso, nunca desfaz venda confirmada.
- DuplicateKey na chave idempotente aborta a tentativa; consulta de recibo ocorre fora da transação abortada no primary, com leitura majority e espera limitada total de 5 s. Recibo/hash igual devolve status/corpo original; hash diferente dá 409 CHAVE_REUTILIZADA. Sem recibo conclusivo após janela dá 409 OPERACAO_EM_ANDAMENTO, sem afirmar sucesso. Nova tentativa posterior conserva chave. Não fazer query dentro de transação já abortada.
- Demais conflitos transitórios esgotados: 503/Retry-After. Validação/referência inativa/saldo insuficiente/overflow conhecidos: 400 ou 409 conforme API, sem retry de regra. Request inválido nunca vira recibo de sucesso.
- Saldo ausente: 409 SALDO_INEXISTENTE e investigação; >int máximo: SALDO_EXCEDE_LIMITE e rollback. Falha de auditoria impede commit. SDK não suporta operações paralelas na mesma transação.
- Índice idempotente por ator/escopo/chave e unique de movimentos reforçam atomicidade. Recibos comerciais não expiram automaticamente; resposta original autorizada pode conter snapshots pessoais mínimos, nunca senha/token. Repetir confirmação após cancelamento devolve recibo original e UI relê GET atual.

Teste específico de write skew: inativação/preço/vínculo contra operação que depende de referência; deve haver conflito real/reavaliação. Não basta testar duas baixas concorrentes, que já alteram o mesmo saldo.

## Evoluções de persistência e operações

Evolução explícita, incremental e idempotente de coleções/validadores/índices, por tarefa operacional única com usuário próprio. Não é migration EF nem conversor automático. Registrar ID, checksum, início/fim e versão alvo; registro aplicado só após verificar todas as definições. Falha parcial de createCollection/createIndex/collMod não é atômica com dados: manter manutenção, detectar definições já criadas, retomar com segurança e nunca marcar aplicada antes de concluir. Índice existente com nome/opções divergentes bloqueia, não é recriado silenciosamente. Checar duplicatas antes de ativar unique. Runtime não tem createCollection/createIndex/collMod/drop/bypass.

Expandir → adaptar dados por lotes retomáveis e versionados → validar → contrair em janela posterior. schemaVersion por documento; aplicação suporta intervalo conhecido durante transição e readiness compara catálogo aplicado com versão exigida. Backfill de centavos exige conversão exata, conciliação e relatório de rejeições. Sem importação de dados anteriores autorizada nesta tarefa. Nenhum downgrade destrutivo automático; restore validado ou evolução corretiva.

Sequência preservada: 00 replica set/configuração/catálogos técnicos separados; 01 users/sessões/auditoria/controle e tickets; 02 employees + vínculo único; 03 customers; 04 categories/brands/products com saldo 0/imagem incorporados; 05 stock_movements manuais/recibos; 06 sales com itens incorporados e índices de movimentos comerciais; 07/08 somente índices adicionais justificados. Proposta 11 permanece P2: publicação incorporada no product e online_orders com itens, só após aceite. Não criar coleções futuras para aprovar gate anterior.

Testar instalação vazia, reexecução sem efeito duplicado e evolução da versão anterior com dados sintéticos preservados, validadores contra escrita inválida, contas runtime contra alteração de histórico e leitura cruzada. Backup/restore completo e consistente conforme arquitetura 10; provas existentes da stack antiga são históricas, não prova MongoDB.

## Fontes

Fontes oficiais consultadas para mecanismos; a modelagem, limites e ordens acima são decisões do projeto: [atomicidade](https://www.mongodb.com/docs/manual/core/write-operations-atomicity/), [transações no driver .NET](https://www.mongodb.com/docs/drivers/csharp/current/crud/transactions/), [write conflicts e leituras obsoletas](https://www.mongodb.com/docs/v8.0/core/transactions-production-consideration/), [validação documental](https://www.mongodb.com/docs/manual/core/schema-validation/), [índices únicos e parciais](https://www.mongodb.com/docs/manual/core/index-unique/). Validar a combinação exata de versões, rótulos de falha e expressões de validator na implementação, sem presumir compatibilidade só por compilar.

## Campos físicos e cobertura mínima das consultas (ADR-26)

sales usa status como campo físico de estado; criadoEm/confirmadoEm/canceladoEm/descartadoEm são BSON Date na raiz. confirmadoEm é obrigatório somente em Confirmada/Cancelada e permanece após cancelar; canceladoEm somente em Cancelada; descartadoEm somente em Descartada. confirmacao/cancelamento/descarte incorporam atorId, nomeSnapshot e motivo quando aplicável, sem segunda cópia da data. createdAt de sales é igual ao criadoEm e não muda. API compõe metadados de data/ator a partir desses campos explícitos, sem indexar campo inexistente ou ambíguo. Itens são id UUID, não _id de coleção separada. Tickets sempre possuem _id UUID e handleHash separado; somente os _id de administration_control e persistence_versions são strings técnicas. Hashes de request usam SHA-256 binário de32 bytes, hash normalizado dos campos do comando na mesma unidade/unidade monetária do contrato e escopo/versionamento declarados, nunca serialização BSON arbitrária.

| Consulta/restrição | Campos/índices mínimos | Bloco que cria e limite |
| --- | --- | --- |
| Usuários por nome/ativo e login por email | users(nomeNormalizado:1,_id:1); unique users(emailNormalizado:1) |01; perfil/stamp não vêm da busca; filtro ativo é aplicado antes da paginação |
| Funcionários/clientes/categorias/marcas/produtos por nome | (nomeNormalizado:1,_id:1) em cada coleção; categories/brands unique nomeNormalizado pode atender prefixo mas adicionar composto se necessário ao desempate |02/03/04; prefixo escapado, sem regex livre; sem prometer busca por substring |
| Vínculo humano e documento de cliente | unique users(employeeId:1), parcial $type binData; unique customers(documento:1), parcial $type string |02/03; null/ausente fora do índice, inclusive cadastros inativos mantêm unicidade |
| SKU exato | unique products(skuNormalizado:1) |04; não tornar nome do produto único |
| Categoria/marca em uso | products(categoriaId:1,ativo:1,_id:1), products(marcaId:1,ativo:1,_id:1) |04; guardas transacionais preservam ativo/vínculo |
| Movimentos gerais e por produto | stock_movements(ocorridoEm:-1,_id:-1) e (produtoId:1,ocorridoEm:-1,_id:-1) |05; query geral não depende de prefixo produtoId; filtro tipo opcional pode exigir índice adicional após explain |
| Vendas gerais/por cliente/próprias | sales(criadoEm:-1,_id:-1), (clienteId:1,criadoEm:-1,_id:-1), (criadorId:1,criadoEm:-1,_id:-1) |06; histórico vendedor combina cliente e criador antes de contar/paginar; índice conjunto adicional se explain justificar |
| Indicadores/relatórios por confirmação | sales(confirmadoEm:1,status:1,_id:1) |06 para dados/consulta de histórico; usado em07/08 sem depender de índice futuro para funcionar; medir plano/filtro no snapshot |
| Baixa/estorno únicos | unique stock_movements(vendaId:1,itemId:1,tipo:1), parcial vendaId/itemId $type binData |06; validador permite somente Venda/EstornoVenda nesse caso e itens pertencentes são verificados na transação |
| Recibos, sessões e tickets | unique receipts(atorId,escopo,chave), unique api_sessions(tokenHash), api_sessions(usuarioId,expiraEm); unique tickets(handleHash) |05/01; receipts significa idempotency_receipts, sem coleção com nome abreviado; índice de expiração separado se TTL for adotado |
| Estoque baixo | products com ativo e expressão saldo.quantidade<=estoqueMinimo; projeção/ordenação saldo/nome/_id |05/07; expressão comparando campos não ganha índice mágico: medir com10 mil produtos, aceitar plano dentro da meta, otimizar sem alterar semântica |

Campos de ordenação de nome nos cadastros são nomeNormalizado e _id; nome original é apresentação. Todos os índices têm nomes/opções/direções versionados e validação na evolução do bloco que os cria. A lista cobre consultas básicas, não garante p95 sem medição. Não criar índice de coleção futura para aprovar módulo anterior.

## Evolução01 implementada

Inicialização explícita cria users, api_sessions, audit_events e administration_control na API e tickets no Web, validadores strict/error com campos tipados e roles fixas. Catálogo usa01-authentication/01-web-tickets, version2, preservando00-foundation/version1. Reexecução confere validadores/índices/checksum e não sobrescreve divergências. Em users, nome é o campo físico de exibição; nomeNormalizado ordena; emailNormalizado é único inclusive inativos. UUIDs Standard, versao/guardVersion Int64, datas BSON Date em UTC. Audit01 contém ator/origem/ação/recurso/data/schemaVersion/traceId; a correlação HTTP vem do host e bootstrap usa identificador técnico próprio, sem capturar payload/senha. Sem coleção/índice de vínculo antes de02.

As verificações de tamanho de tokenHash/handleHash são feitas na criação pelo adaptador (SHA-256,32 bytes) e por testes; o validator exige binData e os índices únicos impedem colisões. Não há token bruto persistido na API; Web armazena binários Data Protection. Expiração é verificada em código sem depender de TTL.

## Evoluções02–04

02-employees/version3 acrescenta employees e índice users.employeeId unique parcial binData.03-customers/version4 acrescenta customers e documento unique parcial string.04-products/version5 acrescenta categories/brands/products, índices normalizados e referências/ativo/id. Produto tem saldo incorporado obrigatório (quantidade Int32 zero/versao Int64 um na criação), preço Int64 e moeda BRL. Nenhuma coleção stock_movements/sales criada. Todos os inicializadores são explícitos, strict/error, conferem definição/checksum e preservam dados; readiness exige os cinco catálogos. Upload atualiza imagem/versão/auditoria em transação, sem I/O de arquivo na sessão. Índices/evoluções não rodam no startup.

## Implementação05 —07/11/2026

RF-09–12 implementados conforme contratos acima, sem sales. Evolução explícita05-stock/version6 acrescenta movimentos manuais/recibos e readiness exige05. Evidências, inventário e gates no [relatório de entrega](../execution/10-ENTREGA-ESTOQUE-VENDAS.md). Execução06 somente após gate05 integral;07 não autorizado.

## Evolução06 implementada

06-sales/version7 cria sales com itens incorporados e validators strict/error para tipos, limites, produtos/IDs distintos, total/subtotais e metadados de estado. Expande validadores05 aceitando manuais antigos e vínculos comerciais e índices únicos parciais vendaId/itemId/tipo; recibos aceitam status200/201 e resposta concluída vinculada ao recurso. Catálogos05/06 preservados; instalação/reexecução detectam definições divergentes, sem alteração silenciosa. CLI explícita aplica00–06 e readiness exige ambos; casos de uso conferem catálogo antes de escrever, impedindo coleção implícita. Runtime não recebe delete de sales nem update/delete de movimentos/auditoria. Dados comerciais permanecem no backend. Detalhes e evidências no relatório10.
