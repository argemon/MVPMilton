# GestaoComercial — instruções oficiais para agentes

**Escopo vigente (07/11/2026): TCC de apresentação local**, um único banco MongoDB Atlas e apenas blocos 00–10. Os blocos 00–08 estão concluídos conforme os registros existentes; 09 é carga demonstrativa, 10 é homologação visual/funcional. **Não implementar publicação, catálogo ou pedidos online.**

- Leia [índice](docs/README.md), [plano](docs/execution/00-PLANO-EXECUCAO.md), [estado](docs/execution/03-ESTADO-PROJETO.md), requisitos e MD do bloco antes de agir.
- Stack: C#/.NET10, Blazor Interactive Server, ASP.NET Core API, MongoDB.Driver oficial, Clean Architecture, xUnit e GitHub Actions existentes. Frontend/backend separados no monorepo.
- Seguir SOLID/KISS, sem microsserviços, generic repository ou novas camadas sem necessidade. Domain não depende de MongoDB/ASP.NET/Blazor.
- Reutilizar conexão MongoDB Atlas já configurada; **não criar outro banco ou ambiente de demonstração**. Não sobrescrever `.env`, expor credenciais nem versionar segredos.
- Bloco 09: ferramenta de carga/restauração pelo administrador, com dados fictícios identificáveis e coerentes; nunca limpar coleções/banco indiscriminadamente, preservar dados manuais.
- Bloco 10: testar todos os módulos e fluxos reais, corrigir bugs, inspecionar telas no navegador, revisar responsividade e preparar apresentação local; não criar features.
- Manter invariantes: valores em centavos Int64, estoque não negativo, confirmação/cancelamento transacionais e idempotentes, auditoria e autorização no backend.
- Executar restore/build/testes existentes; quando não for possível, registrar a limitação sem afirmar aprovação. Continuar usando a infraestrutura de testes que já existe, sem expandir a infraestrutura de demonstração.
- Registrar decisões e resultados reais em [decisões](docs/execution/04-DECISOES-TECNICAS.md) e [estado](docs/execution/03-ESTADO-PROJETO.md). Não declarar conclusão sem evidência.
- A numeração anterior (09 Homologação, 10 Publicação, 11 Catálogo) foi substituída. Registros históricos permanecem apenas em commits anteriores e relatórios de evidência, não como instruções vigentes.
