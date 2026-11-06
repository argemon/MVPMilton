# 00 — Plano de execução

## Estado e regra de sequência

Esta etapa produz documentos, não implementações. Todos os blocos de software estão pendentes. A sequência abaixo é obrigatória, sem execução simultânea de blocos que alterem contratos/modelos comuns. Não delegar trabalho a outros agentes sem instrução do usuário ou regra aplicável que o solicite.

| Ordem | Bloco | Resultado demonstrável | Gate mínimo |
| --- | --- | --- | --- |
| 00 | Fundação | API/Web/DB locais, build e health | Referências, configuração, baseline e pipeline |
| 01 | Autenticação | Bootstrap, login/logout, perfis e sessões seguras | CA-01/02 e segurança BFF |
| 02 | Funcionários | CRUD lógico e criação de acesso vinculado | CA-03; concluir RF-03 |
| 03 | Clientes | Cadastro/pesquisa e histórico vazio tipado | CA-04 incremental |
| 04 | Produtos | Categoria/marca/produto/imagem e saldo zero | CA-05 e validação de imagem |
| 05 | Estoque | Entrada/saída/ajuste e razão auditável | CA-06/07, locks/idempotência |
| 06 | Vendas | Rascunho/confirmar/cancelar e histórico real | CA-08 a CA-13; concluir RF-06 |
| 07 | Dashboard | Fórmulas comerciais e estoque baixo | CA-14/15 |
| 08 | Relatórios | Vendas/estoque/movimentos e CSV | CA-14/15/17 |
| 09 | Homologação | Evidência integrada e defeitos resolvidos | CA-01 a CA-18 aplicáveis |
| 10 | Publicação | URL segura, backup/restore e runbook | CA-16 na topologia final |
| 11 | Catálogo/pedidos | Fase posterior a definir | Q-04 e novo escopo; não liberado |

## Procedimento de cada bloco

1. Ler AGENTS, índice, estado, decisões, módulo alvo e todos os documentos normativos referenciados.
2. Inspecionar arquivos/referências existentes e alterações locais. Não sobrescrever trabalho de outro executor.
3. Confirmar pré-requisitos demonstrados no estado; se não, concluir/corrigir bloco anterior.
4. Registrar início e escopo. Resolver ambiguidade técnica pela especificação; questão de negócio nova vira Q/ADR e bloqueia somente a parte dependente.
5. Implementar domínio/caso de uso, persistência/migration, contrato HTTP, UI e testes necessários ao bloco, sem funcionalidade futura.
6. Validar regras, autorização, erros, concorrência e interface. Executar build/testes adequados; registrar comando e resultado reais.
7. Revisar contrato/OpenAPI, atualizar documentos afetados e eliminar inconsistências.
8. Preencher DoD global/específica e estado, listar pendências e indicar próximo bloco. Não avançar com gate falhando.

## Incrementos explicitamente permitidos

- 01 não depende de funcionário: só conta bootstrap; criação humana vinculada é finalizada em 02.
- 03 não depende de venda: histórico vazio tipado; consulta real e teste com dados em 06.
- 04 cria saldo zero junto ao produto, sem movimentos manuais. 05 acrescenta movimentos; 06 acrescenta referências de venda.
- 07 linka estoque já existente, sem depender da tela de relatório do 08.
- 09 testa restore de ensaio; 10 repete na topologia escolhida.

## Protocolo de retomada

Em novo contexto, ler primeiro `03-ESTADO-PROJETO.md`, conferir evidências contra os arquivos reais e retomar o primeiro checkbox não comprovado do bloco em andamento. Não executar novamente bootstrap/migration/destruir volume sem verificar estado real. Se estado e código divergirem, registrar divergência e corrigir antes de avançar.

## Entrega documental atual

Após a revisão dos módulos: 37 arquivos Markdown, sendo 10 de arquitetura, 6 de requisitos, 12 módulos, 7 de execução, índice e AGENTS. A matriz consolidada foi acrescentada em `06-MATRIZ-MODULOS.md`; decisões propostas 15–18 complementam, sem substituir, ADR-01–14. Catálogo/pedidos continua P2 condicionado à liberação de escopo. Nenhum critério de software foi marcado como executado.

