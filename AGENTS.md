# GestaoComercial — instruções para agentes

- Leia [o índice](docs/README.md), [o estado](docs/execution/03-ESTADO-PROJETO.md), o bloco liberado e seus documentos normativos antes de alterar arquivos.
- Nesta etapa, entregue exclusivamente Markdown. Não criar solução, projetos, código, migrations, configurações executáveis ou infraestrutura.
- Na implementação futura, execute somente um bloco liberado por vez, na ordem de [execução](docs/execution/00-PLANO-EXECUCAO.md). Não antecipar catálogo ou pedidos.
- Use .NET 10 LTS, ASP.NET Core API, Blazor Web App Interactive Server, EF Core, PostgreSQL, xUnit e Docker Compose. Frontend e backend ficam fisicamente separados.
- Respeite Domain ← Application ← Infrastructure e a composição na Api. Frontend referencia somente Shared Contracts, nunca backend ou banco de negócio.
- Shared Contracts contém somente DTOs e contratos HTTP; nunca entidades EF, regras, serviços ou credenciais.
- Não usar microsserviços, CQRS/MediatR, mensageria, Event Sourcing, Generic Repository ou abstrações sem necessidade demonstrada.
- Segurança é obrigatória na API, com negação por padrão. Tokens ficam nos servidores; login/logout do Blazor usam requisições HTTP e proteção antifalsificação.
- Dinheiro usa decimal; saldo nunca negativo; confirmação/cancelamento são transacionais, concorrentes e idempotentes conforme os documentos 05 e 06 de arquitetura.
- Não remover histórico nem editar movimentos. Não versionar segredos nem dados pessoais reais. Não executar migrations automaticamente ao iniciar produção.
- Consulte a matriz RF/RN/CA. Acrescente testes de regra e integração PostgreSQL para cada comportamento alterado; não substituir por EF InMemory nos testes transacionais.
- Decisões novas devem ser registradas antes da implementação em [decisões técnicas](docs/execution/04-DECISOES-TECNICAS.md); atualizar todos os contratos afetados.
- Ao terminar o bloco: registrar arquivos, comandos reais, resultados, evidências, pendências e próximo bloco no estado. Nunca declarar testes executados sem execução.
- Avançar somente quando todos os itens da Definition of Done do bloco estiverem demonstrados. Em novo contexto, retomar pelo estado, sem supor que documentação significa implementação.

