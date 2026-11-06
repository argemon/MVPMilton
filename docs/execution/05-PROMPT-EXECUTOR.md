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
> Use .NET 10 LTS, ASP.NET Core API, Blazor Web App Interactive Server, PostgreSQL, EF Core/migrations, xUnit e Docker Compose. Mantenha frontend/backend fisicamente separados e shared restrito a contratos de transporte. Não use microsserviços, CQRS/MediatR, mensageria, Event Sourcing ou Generic Repository.
>
> Implemente domínio, caso de uso, persistência, contrato HTTP, autorização backend, UI pt-BR e testes necessários ao bloco. Não antecipe funcionalidades de blocos futuros. Dinheiro é decimal; saldo nunca negativo. Confirmação/cancelamento têm transação, locks ordenados, idempotência e auditoria no mesmo commit. Tokens da API ficam nos servidores; login/logout Web são HTTP com antiforgery.
>
> Siga as hipóteses documentadas. Decisão nova exige registro e atualização coerente dos documentos afetados; se faltar resposta de negócio indispensável, descreva o ponto e continue partes independentes, sem inventar regra atribuída ao TCC.
>
> Execute build e testes adequados. PostgreSQL real é obrigatório para demonstrar constraints, locks e transações. Não declare sucesso de teste não executado, não marque skips como aprovação e não versione segredos/dados reais.
>
> Ao terminar, atualize `docs/execution/03-ESTADO-PROJETO.md` com arquivos, comandos reais, resultados, migrations, evidências RF/RN/CA, decisões, limitações e próximo bloco. Verifique DoD global e específica. Se algum gate falhar, registre bloqueio e a primeira ação de retomada; não avance para o bloco seguinte.
>
> Responda com entrega concreta, validações realizadas, limitações e próximo bloco liberado. Não confunda documentação existente com funcionalidade implementada.

## Retomada de bloco interrompido

Acrescentar: “Retome a primeira pendência do bloco em andamento registrada no estado. Confirme os arquivos antes de repetir operações. Preserve evidências anteriores e acrescente resultados novos com data e versão.”

