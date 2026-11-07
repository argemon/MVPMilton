# 05 — Prompt do executor

## Uso

Copiar o texto abaixo em um novo contexto de implementação, preencher o bloco alvo e respeitar o estado real. Não usar este prompt como autorização para implementar nesta entrega documental.

## Prompt reutilizável

> Atue como Tech Lead .NET e implemente somente o bloco **[NÚMERO — NOME]** do GestaoComercial.
>
> Leia `AGENTS.md`, `docs/README.md`, `docs/execution/03-ESTADO-PROJETO.md`, o plano, as decisões técnicas, a DoD e o documento do módulo. Leia também banco, API, segurança, requisitos/regras/permissões e aceites associados ao bloco.
>
> Primeiro inspecione arquivos, alterações locais e evidências do bloco anterior. Se não houver implementação anterior válida, retome o primeiro bloco pendente; não crie atalhos para simular dependências. Nesta tarefa, preserve trabalho preexistente e registre inconsistências.
>
> Use .NET 10 LTS, ASP.NET Core API, Blazor Web App Interactive Server, MongoDB, MongoDB.Driver oficial e inicialização versionada, xUnit, Testcontainers com MongoDB real em replica set e Docker Compose. Mantenha frontend/backend fisicamente separados e shared restrito a contratos de transporte. Não use microsserviços, CQRS/MediatR, mensageria, Event Sourcing ou Generic Repository/Unit of Work artificial.
>
> Implemente domínio, caso de uso, persistência, contrato HTTP, autorização backend, UI pt-BR e testes necessários ao bloco. Não antecipe funcionalidades de blocos futuros. Dinheiro interno é centavos long/BSON Int64 (BRL), com decimal em reais somente no transporte e conversão exata; saldo nunca negativo. Confirmação/cancelamento têm transação, escritas de guarda ordenadas, idempotência e auditoria no mesmo commit. Tokens da API ficam nos servidores; login/logout Web são HTTP com antiforgery.
>
> Siga as hipóteses documentadas. Decisão nova exige registro e atualização coerente dos documentos afetados; se faltar resposta de negócio indispensável, descreva o ponto e continue partes independentes, sem inventar regra atribuída ao TCC.
>
> Execute build e testes adequados. MongoDB real é obrigatório para demonstrar validadores e filtros condicionais, escritas de guarda e transações. Não declare sucesso de teste não executado, não marque skips como aprovação e não versione segredos/dados reais.
>
> Ao terminar, atualize `docs/execution/03-ESTADO-PROJETO.md` com arquivos, comandos reais, resultados, evoluções de persistência, evidências RF/RN/CA, decisões, limitações e próximo bloco. Verifique DoD global e específica. Se algum gate falhar, registre bloqueio e a primeira ação de retomada; não avance para o bloco seguinte.
>
> Responda com entrega concreta, validações realizadas, limitações e próximo bloco liberado. Não confunda documentação existente com funcionalidade implementada.

## Retomada de bloco interrompido

Acrescentar: “Retome a primeira pendência do bloco em andamento registrada no estado. Confirme os arquivos antes de repetir operações. Preserve evidências anteriores e acrescente resultados novos com data e versão.”

Antes de usar este prompt, conferir ADR-20–26 e a auditoria vigente em `docs/execution/06-AUDITORIA-MONGODB.md`: o código anterior do00 não foi adaptado pela revisão Markdown e nenhuma evidência antiga libera01. Sem operações paralelas na mesma sessão transacional; IMongoClient singleton, guardas efetivas e política de retries/commit incerto normativas da arquitetura05. A adaptação00 foi autorizada e executada com Atlas conforme ADR-27–29; consultar os resultados atuais do estado03 e limitações antes de assumir liberação do próximo bloco. Este prompt não é autorização automática para implementar.
