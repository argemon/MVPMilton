# GestaoComercial — instruções para agentes

- Leia [o índice](docs/README.md), [o estado](docs/execution/03-ESTADO-PROJETO.md), o bloco liberado e seus documentos normativos antes de alterar arquivos.
- A solicitação atual autoriza os blocos07–08, sequencialmente e sem aprovação intermediária, além de commit/push/PR e acompanhamento do CI. Iniciar08 somente após o gate07; não iniciar09 nem fazer deploy ou merge automático. ADR-30 mantém Administrador, Vendedor e Estoquista.
- Na implementação futura, execute somente um bloco liberado por vez, na ordem de [execução](docs/execution/00-PLANO-EXECUCAO.md). Não antecipar catálogo ou pedidos.
- Use .NET 10 LTS, ASP.NET Core API, Blazor Web App Interactive Server, MongoDB, MongoDB.Driver oficial, xUnit, Testcontainers e Docker Compose com replica set. Frontend e backend ficam fisicamente separados.
- Respeite Domain ← Application ← Infrastructure e a composição na Api. Frontend referencia somente Shared Contracts, nunca backend ou banco de negócio.
- Shared Contracts contém somente DTOs e contratos HTTP; nunca documentos de persistência, regras, serviços ou credenciais.
- Não usar microsserviços, CQRS/MediatR, mensageria, Event Sourcing, Generic Repository ou abstrações sem necessidade demonstrada.
- Segurança é obrigatória na API, com negação por padrão. Tokens ficam nos servidores; login/logout do Blazor usam requisições HTTP e proteção antifalsificação.
- Dinheiro usa centavos long no domínio/aplicação, mapeados para BSON Int64 somente na infraestrutura, BRL; decimal em reais somente na borda HTTP/formatação, com conversão exata e cálculos checked; saldo nunca negativo; confirmação/cancelamento são transacionais, concorrentes e idempotentes conforme os documentos 05 e 06 de arquitetura.
- Não remover histórico nem editar movimentos. Não versionar segredos nem dados pessoais reais. Não executar evoluções de persistência automaticamente ao iniciar produção.
- Consulte a matriz RF/RN/CA. Acrescente testes de regra e integração MongoDB para cada comportamento alterado; não usar substitutos em memória nos testes transacionais.
- Decisões novas devem ser registradas antes da implementação em [decisões técnicas](docs/execution/04-DECISOES-TECNICAS.md); atualizar todos os contratos afetados.
- Ao terminar o bloco: registrar arquivos, comandos reais, resultados, evidências, pendências e próximo bloco no estado. Nunca declarar testes executados sem execução.
- Avançar somente quando todos os itens da Definition of Done do bloco estiverem demonstrados. Em novo contexto, retomar pelo estado, sem supor que documentação significa implementação.

- ADR-20–25 e documentação MongoDB vigente prevalecem sobre decisões históricas incompatíveis. A fundação foi convertida pela autorização anterior00; usar as novas evidências Atlas do estado03, sem transferir aprovação de testes antigos. ADR-27–29 prevalecem nas condições de execução/estrutura.
- IMongoClient singleton por credencial, sessões curtas por operação; BSON/filtros/driver e ASP.NET Core/Blazor fora de Domain; BSON/filtros/driver fora de Application/Contracts. Sem Generic Repository nem Unit of Work artificial.
- Transações snapshot/majority/primary em replica set, updates condicionais com resultado conferido, escritas de guarda efetivas e operações sequenciais; nenhum Task.WhenAll na mesma sessão. Não há reserva no MVP.
- Integração com MongoDB real e transações: Atlas isolado por execução conforme ADR-28 ou perfil Testcontainers explícito; ausência de Docker não autoriza simulação/standalone, mas não bloqueia o caminho Atlas comprovado. Evoluções de coleções/índices/validadores são explícitas e idempotentes; sem schema mutável no startup.

- Auditorias06/08 são históricas. Estado vigente em03 contém os gates atuais; preservar evidências antigas sem tratá-las como atuais. Blocos07–08 autorizados nesta conversa;09 e seguintes aguardam nova solicitação.

- ADR-27–29 atualizam a execução00: reutilizar Atlas existente e .env sem sobrescrever/imprimir credenciais; integração pode usar bancos descartáveis próprios nesse cluster, sem criar instância paralela. rs0/Testcontainers aplicam-se ao caminho local/CI, não são exigência de nome/topologia Atlas. Estrutura física atual é backend/Projeto, frontend/Projeto e tests/UnitTests/IntegrationTests conforme solicitação.
