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

<!-- functional -->
<!-- nonfunctional -->
<!-- rules -->
<!-- acceptance -->

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

<!-- usecases -->

# 5 Rastreabilidade de implementação e testes

As relações foram organizadas a partir dos arquivos desta versão. A coluna teste aponta código de teste existente, sem atribuir aprovação atual. Os detalhes Dado/Quando/Então estão no capítulo 2. O inventário de métodos no apêndice permite localizar as asserções e suas linhas.

<!-- traceability -->

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

<!-- versions -->

As versões acima são declaradas em global.json e Directory.Packages.props. O servidor Atlas não foi consultado nesta tarefa. A configuração Testcontainers/CI não prova a versão corrente do servidor de apresentação. Configuração de infraestrutura futura e histórico em deploy não autorizam publicação.

# 7 Persistência e dicionário de dados

IDs de negócio são UUID Standard em BSON binData; quantidades são Int32, centavos e versões são Int64 e instantes são BSON Date em UTC. HTTP usa UUID textual e ISO 8601. Nomes persistidos são camelCase, com employeeId mapeado para funcionarioId no contrato. Campos anuláveis são explícitos no dicionário; anulação não significa ausência livre de qualquer campo exigido pelo validator.

Produto incorpora saldo e imagem opcional; venda incorpora até cem itens e snapshots. O saldo não constitui coleção separada. Movimentos, recibos, sessões e auditoria são coleções independentes. Os critérios de incorporação/referência e de valores temporais encontram referência na documentação do MongoDB (MongoDB, s.d.c); limites concretos pertencem ao projeto.

<!-- dictionary -->

## 7.1 Índices e validação

<!-- indexes -->

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

<!-- endpoints -->

## 9.2 Exemplos sintéticos de transporte

Os exemplos mostram estrutura, não credenciais nem registros reais. Para criar rascunho, o cabeçalho Idempotency-Key deve conter UUID válido gerado para a intenção. Corpo: {"clienteId":"11111111-1111-4111-8111-111111111111","itens":[{"produtoId":"22222222-2222-4222-8222-222222222222","quantidade":2}]}. Os UUIDs ilustrativos só funcionariam se os recursos existissem. Não se envia preço, total, ator ou perfil.

Para confirmar, POST /vendas/{id}/confirmacao recebe {"versaoEsperada":1}, com a chave correspondente à confirmação. Para cancelar, POST /vendas/{id}/cancelamento recebe {"versaoEsperada":2,"motivo":"Cancelamento de exemplo"}. Valores de versão são ilustrativos e devem vir de leitura real do recurso.

## 9.3 Erros e recuperação

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

<!-- dto -->

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

<!-- criticaltests -->

## 12.1 Evidências históricas e limites

Estado03 registra homologação em07/11/2026 com código0d49db9 e CI de242 testes, incluindo126 unitários e116 integrações. Registra em09/11/2026 validação local da limpeza com128 unitários e30 integrações direcionadas. Em23/11/2026, o ajuste de identidade visual registra build e medições estáticas em1920/1366/768/390px. Estes resultados pertencem às execuções citadas, não a esta tarefa.

A pasta artifacts mencionada nos relatórios não está disponível neste checkout. Há imagens .local/unigran-*.png de montagem estática; não foram tratadas como jornada autenticada. A entrega conserva os relatórios como fontes históricas e não inventa screenshots funcionais, logs ausentes, percentual de cobertura ou novos resultados de CI.

## 12.2 Critérios para manutenção futura

Mudança de regra deve localizar RF/RN/CA e ampliar testes pertinentes. Em transações, verificar insuficiência em item posterior, rollback de auditoria, concorrência entre sessões, idempotência com mesmo e diferente corpo, commit incerto e revogação/perfil/propriedade. Em consultas, verificar filtros antes da paginação e totais do conjunto inteiro. Alterações só documentais requerem conferir fontes, links, diagramas e renderização.

# 13 Catálogo de diagramas

Cada modelo possui fonte .puml, exportação .svg e .png no mesmo diretório diagramas. Fontes são autorais, derivadas da inspeção do código; não são figuras externas copiadas. Classes conceituais, recortes de implementação e documentos físicos são explicitamente distintos. Referência de notação: UML2.5.1 (Object Management Group, 2017).

<!-- diagrams -->

# Referências

<!-- references -->

# Apêndice A Inventário de testes

Este inventário lista métodos anotados Fact/Theory encontrados no código. Theory pode executar múltiplos casos. Não representa contagem de testes aprovados e não inclui execução nesta tarefa.

<!-- testinventory -->
