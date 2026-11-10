# 03 — Estado do projeto

## Ajuste da tela inicial — 09/11/2026

A pedido do responsável após captura da home: rota / agora apresenta área de trabalho com atalhos de nova venda, clientes, estoque e produtos, consultas secundárias e destaque ao dashboard somente para Administrador. Cada atalho usa a policy existente; troca obrigatória de senha continua bloqueando atalhos. Removida consulta técnica de readiness da home, mantendo MeAsync e tratamento de falha de sessão. Sem regra/contrato/persistência novos; .env e Atlas não acessados/alterados nesta correção. Alterações locais anteriores de login e .vs preservadas.

Arquivos desta correção: InicioTecnico.razor, app.css, complemento ADR-38 e este registro. Comando real aprovado: dotnet test tests/GestaoComercial.UnitTests --configuration Release --verbosity minimal -m:1 -p:UseArtifactsOutput=true -p:ArtifactsPath="$pwd/.local/home-build", DOTNET_CLI_HOME=.local/dotnet; 127 aprovados, 0 falhas/ignorados. Compilação dos sete projetos concluída. Primeiro teste no output padrão encontrou DLLs bloqueadas pelos hosts abertos; pasta isolada sem restore falhou por assets ausentes; restore inicial falhou por acesso NuGet e repetição com acesso autorizado concluiu. git diff --check sem erros. Sem novos testes de integração, pois apenas composição visual/atalhos, sem comportamento transacional alterado.

Limites: nova composição ainda não inspecionada em navegador real; aplicação já aberta continua executando a compilação anterior e precisa ser reiniciada para carregar a nova tela. Sem commit/push/CI desta correção; não atribuir CI histórico ao código atual. Próxima ação: reiniciar Web, conferir início nos três perfis e larguras do bloco10. Nenhum bloco adicional, deploy ou merge.

## Acesso administrativo restabelecido — 09/11/2026

Após o responsável autorizar o IP atual no Network Access do Atlas existente, diagnóstico local com TLS1.2 retornou Atlas ping OK; API readiness200 e login do administrador original200. Criada a conta fictícia solicitada, vinculada a funcionário ativo, pelo POST autenticado de funcionários e usuários, perfil Administrador. Novo login200 e perfil conferidos; sessões de verificação encerradas por logout. Senha temporária exige troca no primeiro acesso. Credenciais somente em arquivo .local ignorado; .env preservado e nenhuma escrita direta em documentos. Procedimento local create-requested-admin.ps1; tentativa inicial recusada400 por filtro ativo=todos em endpoint que recebe bool?, corrigido no procedimento para omitir filtro; nenhum usuário existente redefinido. Sem novos testes de integração ou CI nesta ação operacional. Nenhum deploy/merge ou novo banco.

## Suporte ao login — 09/11/2026

Relato: POST de login exibe somente “Erro”. Confirmado defeito no DocumentoConta: parâmetros string Erro/ReturnUrl eram literais. Corrigidos para expressões Razor; AccountDocumentTests renderiza o documento real e verifica mensagem segura e retorno /produtos. Complemento registrado na ADR-38. Arquivos: DocumentoConta.razor, AccountDocumentTests.cs e registros03/04. Nenhuma regra, persistência, credencial ou .env alterado; diretório .vs preexistente não incluído.

Comandos reais: `dotnet test tests/GestaoComercial.UnitTests --configuration Release --no-restore --filter FullyQualifiedName~AccountDocumentTests --verbosity minimal -m:1` (1 aprovado) e `dotnet test tests/GestaoComercial.UnitTests --configuration Release --no-build --no-restore --verbosity minimal` (127 aprovados, zero falhas/skips), com DOTNET_CLI_HOME=.local/dotnet. Tentativas iniciais sem elevação falharam na compilação sem diagnóstico; execução autorizada acima concluiu. Integração MongoDB não executada: teste é exclusivamente de renderização, sem mudança de comportamento transacional.

Diagnóstico local autorizado: HTTP Web7243=200, API7143/health/live=200; API8081/health/ready=503, login direto com acesso privado=503 OPERACAO_INDISPONIVEL (nenhum token exposto). DNS SRV resolveu3 nós; TCP27017 conectou nos3. Ping MongoDB.Driver no Atlas existente, somente leitura, falhou por TimeoutException com heartbeat TLS: AuthenticationException → Win32Exception0x80090304 (“A autoridade de segurança local não pode ser contatada”). Esse resultado pertence ao processo de diagnóstico; não prova causa idêntica no processo API do usuário. Nenhuma evidência de senha inválida. Diagnóstico temporário em .local/login-diagnostic, ignorado. Mensagem corrigida compilada, mas hosts existentes não reiniciados. Pendência: reiniciar API/Web em PowerShell do usuário, verificar readiness e repetir login; se falhar, obter diagnóstico do processo API. Login funcional ainda não confirmado, sem commit/push/CI desta correção. Nenhum bloco novo, deploy ou merge.

## Execução10 — HOMOLOGAÇÃO APROVADA — 07/11/2026 America/Cuiaba

Código final0d49db9: CI PR42429190111 e push42429186208 success,242 testes sem falhas/skips,build/auditoria/scanner aprovados. Detalhamento do gate final ao fim deste registro.

Solicitação explícita atual autoriza10, commit/push/PR e CI; ADR-38 prevalece sobre a restrição09 histórica. Base main b3b7b1d, branch codex/final-homologation, [PR5](https://github.com/argemon/MVPMilton/pull/5). Sem deploy/merge ou novo banco local; .env preservado por SHA-256. Não há bloco11/publicação.

API/Blazor Development em HTTPS e Edge reais no único gestao_comercial_dev.75 verificações HTTP específicas,16 de perfis no navegador, expiração real de sessão sintética401, formulário de senha incorreta, CSRF400. Jornada entrada20→venda3×12,50→saldo17→cancelamento→saldo20 e histórico preservado. Repetição→restauração→repetição da carga aprovadas; manuais HOMO10 preservados. Banco final:16 funcionários/15 contas/42 clientes/101 produtos/6 categorias/6 marcas/251 vendas/857 movimentos,0 divergências;225 confirmadas/26 canceladas; receita18401569 centavos. Dashboard, relatório e soma exata do CSV reconferidos depois da restauração. Cinco downloads CSV reais aprovados.

Correções: menus em faixas substituídos por sidebar com grupos/policies preservados e menu móvel recolhível; área principal fluida; campos de busca separados dos links; acabamento de tipografia/cards/tabelas/foco. Sem nova regra/contrato/biblioteca.33 páginas/fichas×4 larguras=132 medições finais,0 overflow de página/JS; imagem e ficha de usuário visitadas adicionalmente. Zoom125/150 emulado por viewport/escala, sem afirmar zoom nativo. Teclado no menu conferido. Capturas e limites no relatório.

Restore locked/build final Release0 avisos/erros;126 unitários locais finais,0 falhas/skips. [CI PR42427827241](https://github.com/argemon/MVPMilton/actions/runs/42427827241) e [push42427798381](https://github.com/argemon/MVPMilton/actions/runs/42427798381), commit912eb15: todas as etapas success,126 unitários+116 integrações MongoDB8.0.17/Testcontainers autenticado/replica set=242 aprovados,0 falhas/skips, sem filtros. Logs em artifacts/block10/ci-pr.log e ci-pr.json. Auditoria NuGet0 vulneráveis; scanner241 arquivos sem segredos. A suíte de integração não foi executada contra o banco persistente. Listener JS sem uso removido depois desse CI, build/UI final aprovados; CI do código final0d49db9 aprovado, conforme gate abaixo.

Arquivos: AGENTS/README/índice/plano/estado/ADR/módulo10/relatório, LayoutPrincipal, app.css/GestaoComercial.Web.lib.module.js (App foi restaurado ao conteúdo original no resultado final). Comandos reais, falhas de ambiente/automação e correções, inventário/evidências/limites em [relatório de homologação](07-RELATORIO-HOMOLOGACAO.md). Evidências locais em artifacts/block10; acesso privado em .local/demo-access.json e .local/block10-roles.json, não versionar/exibir. Imagem sintética persistente em .local/images, conforme README. Roteiro12 minutos e checklist preparados. Sem defeito crítico identificado. Próxima ação é iniciar a aplicação conforme README e apresentar à banca; nenhum bloco adicional autorizado.

## Execução09 — CONCLUÍDO — CI integral aprovado — 07/11/2026 America/Cuiaba

Somente09 autorizado/implementado (ADR-37);10-Homologação não iniciado. Branch codex/demo-data atualizada por fast-forward à documentação oficial ada96fd, preservando requisitos/plano remotos e as instruções explícitas mais recentes do usuário. PR3/706b61f foi mesclado externamente; nenhum merge de PR/deploy executado pelo agente.

Banco único gestao_comercial_dev no Atlas existente, inclusive tickets Web; .env inalterado. Inicialização explícita00–06 aprovada no banco persistente pela autorização atual, resolvendo pendência histórica. Catálogo tickets2→8 atualizado explicitamente para compartilhar o catálogo com autenticação2, sem recriar tickets/banco. Não remover o banco Web antigo. Sem instância/ambiente Demo, URI ou credencial MongoDB adicional.

Página Administração > Dados de Demonstração com banco/última conclusão, gerar/retomar e restaurar confirmado. API GET/POSTs administrativos, revalidação real de sessão, auditoria transacional de proveniência e etapas idempotentes; serviços existentes de cadastro/Identity/estoque/venda. Restauração recusa dependências/alterações manuais e usa transação/guardas para excluir apenas UUIDs demonstrativos comprovados. Auditoria preservada; sem DropDatabase/limpeza de coleção no gerador.

Cenário final restaurado e repetido pela página no mesmo banco:12 funcionários/contas (9 ativos/3 inativos;3 perfis),40 clientes (5 sem compras),100 produtos (5 inativos/10 zerados),5 categorias/5 marcas;250 vendas de2 itens,225 confirmadas/25 canceladas;850 movimentos (300 manuais+500 baixas+50 estornos).0 divergências produto×soma dos deltas; receita líquida18401569 centavos. Users total13 inclui administrador bootstrap normal fora da carga. Senhas aleatórias não retornadas. Acesso inicial apenas em .local/demo-access.json ignorado; não versionar/exibir. .env preservado por hash.

Comandos/resultados reais: restore8 projetos e builds Release0 avisos/erros. Regressão Atlas anterior ao complemento da massa:126 unitários+113 integrações,0 falhas/skips (artifacts/block09/final); cobre interrupção/retomada, duas cargas concorrentes, rollback de restauração e dependência manual. Código após complemento:126 unitários (unit-final)+4 integrações de contratos/autorizações/BFF/banco único (current-contracts),0 falhas/skips. Cenário final de2 itens/inativos/última conclusão também conferido nos hosts HTTPS reais, gerando/restaurando/repetindo:360/1280 sem overflow,0 JS, Dashboard/cinco relatórios; resumo antes/depois da repetição idêntico. CI integral atual comprovou também a massa ampliada:126 unitários+116 integrações =242 aprovados,0 falhas/skips, sem filtros, MongoDB8.0.17/Testcontainers autenticado/replica set. Não foi declarada execução local desse teste ampliado.

Auditoria NuGet0 vulneráveis; scanner239 arquivos sem segredos e busca por credenciais reais0 ocorrências; referências válidas,44 Markdown/137 links locais sem quebra; diff --check limpo. Comandos completos, inventário exato dos arquivos, diagnósticos iniciais e rastreio RF/RN/CA no [relatório12](12-ENTREGA-CARGA-DEMONSTRACAO.md). Evidências locais em artifacts/block09/{focused,final,unit-final,current-contracts,ui,ui-final,ui-repeat,persistent-check.json}; processos temporários encerrados após jornadas, sem publicação.

DoD09 demonstrada. Commit funcional c622648 publicado; [PR4](https://github.com/argemon/MVPMilton/pull/4) aberto, sem merge. [CI PR42424883343](https://github.com/argemon/MVPMilton/actions/runs/42424883343) e [push42424854966](https://github.com/argemon/MVPMilton/actions/runs/42424854966) success em todas as etapas, incluindo auditoria/scanner; artefato13595640050 confirmado. Logs registram126+116 testes e build0 avisos/erros. Este registro fixa evidências do commit funcional; commits de evidência não alteram a implementação e seu HEAD deve ser conferido no PR. Sem pendência funcional09. Próximo10-Homologação somente mediante nova solicitação; não iniciado. Nenhum deploy/merge automático. A carga já existe no banco persistente; não repetir restauração sem necessidade/confirmar proteção das dependências.

## Plano oficial revisado — 07/11/2026

**Apenas documentação atualizada nesta revisão.** Blocos 00–08 concluídos conforme os registros de execução abaixo; **09 Carga de Dados** e **10 Homologação/Acabamento Visual** pendentes até validação própria. A aplicação do TCC será exibida localmente com o único banco MongoDB Atlas existente. Foram retirados do escopo deploy/publicação e catálogo/pedidos. Textos históricos abaixo não autorizam módulos excluídos. Nenhum teste novo executado nesta revisão documental.


## Execução07–08 — 07/11/2026 America/Cuiaba, CONCLUÍDA; sem deploy/merge

### Resultado final do código — 14h46

07-Dashboard e08-Relatórios concluídos/aprovados sequencialmente. Branch codex/dashboard-reports, commits funcionais a51438d/e60e900 e correção07c8065. [PR3](https://github.com/argemon/MVPMilton/pull/3) aberto, sem merge/deploy. [CI PR42402364442](https://github.com/argemon/MVPMilton/actions/runs/42402364442) e [push42402359673](https://github.com/argemon/MVPMilton/actions/runs/42402359673) concluídos com todos os passos success após a correção:125 unitários+111 integrações MongoDB8.0.17 Testcontainers autenticado/replica set =236 aprovados,0 falhas/0 skips, sem filtros. Build Release0 avisos/erros, auditoria e gitleaks aprovados; artefato13585992783 confirmado. Este registro refere-se ao último código funcional07c8065; o commit documental posterior também será acompanhado até conclusão, sem mudança funcional.

DoD07/08 demonstrada: critérios M07-CA01–05/M08-CA01–05, consultas reais/agregações/snapshot, fórmulas/Int64/overflow, autorização API/BFF, canceladas/snapshots, paginação/ordenação/filtros/limite10.000/CSV seguro, mensagens de erro/vazio/carregamento, cinco telas360/1280 e oito downloads/teclado confirmados. Instalação parcial retorna503, não zeros aparentes. Sem schema/índice/coleção novo, sem PDF conforme escopo, sem dependência de09. Inventários, comandos e histórico das falhas/correções no [relatório11](11-ENTREGA-DASHBOARD-RELATORIOS.md).

Pendências preservadas: aplicação das evoluções anteriores ao banco persistente continua aguardando autorização específica (rejeição automática histórica abaixo), Q-01/02/03 e ensaios/carga/backup dos blocos posteriores. Nada disso foi declarado executado aqui; somente bancos descartáveis autorizados foram utilizados/removidos. .env intacto e sem segredo/PII real versionado. Próximo bloco09-HOMOLOGACAO, somente mediante nova solicitação. Primeira ação futura:ler este estado, confirmar código/PR e tratar os pré-requisitos operacionais de09 antes de iniciar;10/11 não liberados.

### Git/CI e correção final de prontidão — 14h37

Correção publicada em07c8065; [novo CI PR42402364442](https://github.com/argemon/MVPMilton/actions/runs/42402364442) e [push42402359673](https://github.com/argemon/MVPMilton/actions/runs/42402359673) em acompanhamento. Jornada final repetida com esse código:cinco relatórios360/1280, oito downloads concluídos e arquivos conferidos, seleção de relatório por dropdown e rolagem horizontal das tabelas por teclas ArrowRight com foco real, filtros/vazio/erro/recuperação,0 JS/5xx. Bancos exclusivos removidos. Nenhum comportamento de09 foi iniciado.

Branch codex/dashboard-reports publicada, commits a51438d (Dashboard) e e60e900 (Relatórios), [PR3](https://github.com/argemon/MVPMilton/pull/3) aberto contra main. [CI PR42401350272](https://github.com/argemon/MVPMilton/actions/runs/42401350272) e [push42401320201](https://github.com/argemon/MVPMilton/actions/runs/42401320201) concluídos com sucesso em todos os passos. Logs PR confirmam125 unitários+110 integrações Testcontainers Mongo8.0.17 autenticado/replica set =235 aprovados,0 falhas/0 skips; build0 avisos/erros, auditoria/scanner aprovados, artefato13585711518 presente. Nenhuma alteração do CI para esconder falhas.

Revisão adicional encontrou leitura200 com catálogo06 incompatível em instalação parcial. Novo AnalyticsReadinessTests reproduziu1 falha real antes da correção (artifacts/block08/readiness-before). Complemento ADR-36 registrado antes de alterar: Dashboard e cinco relatórios agora exigem VendasSchema.IsReadyAsync, sem executar DDL/evolução. Validação posterior12 integrações (prontidão+Dashboard+Relatórios/snapshots),0 falhas/skips; todos os11 GETs retornam503 JSON sem attachment enquanto o catálogo está incompatível, e200 após restaurá-lo. Somente catálogo técnico do banco descartável foi alterado/restaurado; banco persistente intacto. Build Release de todos os projetos aprovado0 avisos/erros e125 unitários finais aprovados,0 skips; arquitetura/diff/documentação/scanner válidos (230 arquivos sem segredos). Correção será enviada e CI integral revalidado antes da conclusão.

Nova solicitação anexada autoriza07 Dashboard e08 Relatórios sequencialmente, commit/push/PR/acompanhamento CI; sem09/deploy/merge. Autorizações antigas abaixo ficam históricas. Base812126c, checkout limpo em codex/stock-sales; branch criada codex/dashboard-reports.00–06 têm gate anterior199 testes, sem transferir aprovação para código novo. ADR-36 registrada antes da implementação;07 aprovado/commit a51438d;08 em validação. .env e banco persistente preservados; somente bancos exclusivos descartáveis nos testes.

### Gate07 — APROVADO localmente, 14h02

Restore locked8 projetos e build Release0 avisos/erros. Regressão final:110 unitários+101 integrações Atlas aprovados,0 falhas/skips; complemento final de unidade115/115 com testes de limite monetário =216 casos distintos aprovados. Categorias ContainerSecurity/ContainerScale permanecem obrigatórias no CI final sem filtros, conforme ADR-35. Nenhum teste foi desabilitado. Primeira regressão teve3 falhas em expectativas/preparação de testes, corrigidas e reexecutadas (relatório11).

M07-CA01–05: receita/canceladas/ticket/fuso/bordas/ranking/recentes/baixo estoque, limite20/totalItems, precisão acima2^53/limiteInt64, autorização e snapshot com commit concorrente comprovados. Duas jornadas HTTPS com API real e bancos Atlas exclusivos: confirmaçãoR$25/saldo8 → cancelamento/receita0/saldo10, filtros vazio/inválido/recuperação,0 JS/5xx. Capturas360/1280 revistas; tabelas têm rolagem horizontal/foco e filtro acessível. Limpeza dos bancos concluída. Gitleaks inicial213 arquivos sem segredos; auditoriaNuGet sem vulnerabilidades; arquitetura válida; .env hash inalterado. Sem schema/índice novo, explain do índice06 compatível. Comandos, arquivos, evidências e DoD no [relatório11](11-ENTREGA-DASHBOARD-RELATORIOS.md).

08 liberado e iniciado somente após esse gate. CI integral e publicação Git dos dois blocos ainda pendentes.09 não autorizado. Banco persistente continua intacto; pendência operacional antiga de inicialização não foi contornada.

### Bloco08 — APROVADO localmente, CI pendente, iniciado após gate07

Cinco relatórios (vendas/operadores/produtos/estoque/movimentos), cinco exportações CSV e handler BFF, sem PDF. Contratos/ordenações/filtros documentados antes de implementar; nomes/snapshots e centavos reusam07/06. Sem schema/índice/coleção adicional. Restore/build Release aprovados0 avisos/erros. Testes direcionados:11 unitários (CSV/OpenAPI)+6 integrações (cinco consultas, paginação21, consistência07, snapshot concorrente, policies10 endpoints e BFF),0 falhas/skips. Teste de escala novo executado sozinho em Atlas descartável:10.000 linhas CSV completo e10.001 rejeitadas400 sem attachment,1/1 aprovado,0 skips.

Regressão final00–08 em artifacts/block08/final:125 unitários+107 integrações reais =232 aprovados,0 falhas/0 ignorados. Com o teste de escala isolado,233 casos distintos aprovados localmente. Perfil completo sem filtros é obrigatório no CI (inclui2 cenários adicionais anteriores de container); não declarar CI aprovado antes de consultar GitHub. Jornada08 HTTPS real concluída:cinco telas360/1280, oito CSVs com eventos completed/arquivos conferidos, filtro/totais/cancelamento/estoque conciliados, vazio/erro/recuperação,0 JS/5xx. Capturas revistas; bancos removidos. Diagnóstico da primeira tentativa de download e correção do controlador de testes constam no relatório11; nenhuma proteção da aplicação foi desabilitada.

Gitleaks candidato229 arquivos/~1,17MB sem segredos; auditoriaNuGet sem vulnerabilidades. Revisão44 Markdown/179 links válidos,0 credenciais reais, .env hash preservado e0 artefatos locais rastreados. Arquitetura/diff válidos. Workflow existente .NET10 compila/testa os8 projetos e usa Testcontainers autenticado/replica set Mongo8.0.17, sem Atlas/sem filtros/sem continue-on-error. Próximo:commit08/push/PR/CI.09-HOMOLOGACAO continua aguardando nova solicitação; nenhum deploy/merge.

## Execução05/06 — 07/11/2026 (America/Cuiaba), CONCLUÍDA; aplicação no banco persistente aguarda autorização

Solicitação anexada autoriza Estoque05 e Vendas06 em sequência, validações, commit/push/PR/CI; sem07/deploy/merge. Base local feature/block-04-products limpa, nova branch codex/stock-sales. Gate04 histórico139 testes/CI aprovado; regressão atual em execução antes do05. Probe Atlas executado com acesso autorizado: conexão/topologia transacional disponíveis; sem alteração no .env. Primeira execução restrita não alcançou o testhost; não constitui regressão aprovada. Uma disputa de último administrador falhou com Mongo112/transiente na regressão autorizada; investigar sem ocultar falha.06 não iniciado.

### Bloco06 — CONCLUÍDO, gate aprovado no caminho Atlas isolado + CI

Commit812126c, push42379407973 e [CI42379411217](https://github.com/argemon/MVPMilton/actions/runs/42379411217) success em todas as etapas, confirmado até finalização/artefatos:102 unitários+97 integrações MongoDB8.0.17 Testcontainers autenticado/replica set =199 aprovados,0 falhas/0 skips; inclui ContainerSecurity/ContainerScale sem filtros. Build Release0 avisos/erros; auditoria sem vulnerabilidades, scanner~1,05MB sem segredos.07-DASHBOARD é o próximo bloco, disponível somente mediante nova solicitação; não iniciado. Nenhum deploy/merge executado pelo agente.

Aplicação persistente em Development: destino resolvido como gestao_comercial_dev, sem override Evolution. Comando `$env:ASPNETCORE_ENVIRONMENT='Development'; dotnet backend/GestaoComercial.Api/bin/Release/net10.0/GestaoComercial.Api.dll --initialize-persistence --Mongo:Database gestao_comercial_dev` foi rejeitado antes da execução pela revisão automática: alteração potencialmente custosa do banco persistente sem autorização explícita para esse destino; alternativa segura são bancos descartáveis. Não contornar a rejeição. Software/CLI/upgrade foram comprovados integralmente em bancos isolados; banco persistente e .env continuam intactos. Pedido de autorização explícita pendente. Esta pendência operacional não é apresentada como migração aplicada ou deploy aprovado.

Commit812126c enviado a origin/codex/stock-sales, PR2 atualizado para estoque/vendas e CI42379411217 em execução. Ao conferir metadata, PR1 consta mesclado externamente às12:57:52UTC de07/11/2026, commit5220d620; branch feature/block-04-products removida e PR2 agora tem base main. ls-remote confirma5220d620. Nenhum comando/ação de merge foi executado nesta sessão; não atribuir o merge externo ao executor. Inventário de56 arquivos do PR preservado; CI testa a base remota atual. Ajustar descrição final após conclusão do CI.

Validação local final concluída: restore locked nos8 projetos, build Release0 avisos/erros e arquitetura/diff válidos. `dotnet test ... --filter 'Category!=ContainerSecurity&Category!=ContainerScale' --logger trx --results-directory artifacts/block06/final`:95 integrações Atlas+102 unitários =197 aprovados,0 falhas/0 ignorados. Perfis de segurança/volume aguardam CI integral sem filtro. Jornada HTTPS final repetida após correções de layout/seletores: entrada10→venda2×12,50→saldo8→cancelamento→saldo10/histórico real,0 JS/5xx,360/1280 sem overflow, teclado na tabela e edição não salva bloqueada. Capturas revisadas, bancos exclusivos removidos. Gitleaks candidato203 arquivos/~1,04MB sem segredos; .env preservado. Código06 pronto para commit/push/CI;07 não iniciado. Relatório10 consolida endpoints, páginas, inventário, comandos, resultados e limites.

Iniciado após gate05/CI59acdbc, módulo06 e contratos05 relidos. RF-06/13–16: rascunho editável/versionado, preço servidor em centavos long, snapshots, confirmação/baixa, cancelamento/estorno, descarte, consulta/resumos/paginação e histórico real por proprietário. Reuso da atualização condicional de estoque e extração do coordenador concreto de recibos/transação comprovado05. Sem driver/BSON em Domain/Application/Contracts; sem reserva/financeiro/fiscal/07.

Evolução06-sales/version7 cria sales e expande movimentos/recibos de forma compatível, índices de consulta/baixa-estorno únicos, validators de itens/total/estados. Catálogos explícitos são conferidos antes de comandos; ausência503 sem coleção implícita. CLI/reexecução/readiness00–06. Instalação/upgrade05→06 preservam movimento manual/recibo e rejeitam checksum divergente. API protegida e Web /vendas, /vendas/nova, /vendas/{id}; histórico real em /clientes/{id}; tabelas/filtros com seleção por nome, estado/versão, preços, justificativa, carregamento, falha e repetição da mesma intenção. Confirmação bloqueada enquanto há edição não salva; GET após recibo apresenta estado atual.

Primeiros17 testes de integração de Vendas/upgrade/BFF aprovados em Atlas isolado,0 falhas/0 ignorados. M06-CA01–07 demonstrados: preço1250×2=2500, dois itens insuficientes/rollback integral, duas vendas disputando saldo1, idempotência paralela, estorno único mesmo inativo, rascunho rejeita inativos, propriedade/histórico vendedor404. Testados também saldo ausente, overflow/rollback do cancelamento, concorrência contra preço/cliente/produto e rótulos de commit real. Falhas iniciais de definição JSON e variável $$i no validator foram corrigidas, sem retirar verificação; build/testes repetidos. Erros de sintaxe dos novos testes/Razor corrigidos; tentativa executada antes da compilação final contou apenas1 caso antigo e não vale como prova dos8 novos (artifacts/block06/rules). O conjunto compilado foi executado em rules-current:8 aprovados; extended:17 aprovados. Jornada HTTPS de entrada10→venda2/R$25→saldo8→cancelamento→saldo10/histórico real passou,0 JS/5xx, viewport360/1280; layout/seletores do roteiro corrigidos após inspeção e repetidos. Verificação adicional de teclado na tabela em execução; regressão completa e CI06 pendentes. Inventário e resultados finais no relatório10. .env preservado, nenhuma evolução do banco de desenvolvimento nesta etapa.

### Estoque05 — CONCLUÍDO, gate aprovado;06 iniciado em sequência

Commit59acdbc, [CI42374796524](https://github.com/argemon/MVPMilton/actions/runs/42374796524) success em todas as etapas:93 unitários+80 integrações autenticadas Testcontainers/MongoDB8.0.17/replica set =173 aprovados,0 falhas/0 skips. Inclui os quatro cenários de controle de commit e runtime sem alteração de histórico/DDL/leitura cruzada. Build Release0 avisos/erros; auditoria sem vulnerabilidades, Gitleaks~940KB sem segredos. Gate05 demonstrado pelo caminho CI, sem transferir falhas Atlas para aprovação local. Banco exclusivo gc_m00_eb7f52728671465cab9787d5_test da fixture que perdeu conexão identificado por evolução05 aplicada às12:55:16.340UTC dentro da última execução; removido após reconferir nome/horário exatos, sem tocar em outro banco.06 iniciado somente após esta prova, leitura repetida do módulo06 e contratos reais05.07/deploy/merge permanecem excluídos.

Primeiro commit2016208 publicado, [PR2](https://github.com/argemon/MVPMilton/pull/2) sobre feature/block-04-products (PR1 ainda aberto, sem merge). [CI42373847763](https://github.com/argemon/MVPMilton/actions/runs/42373847763) success em todas as etapas: Ubuntu/.NET10,93 unitários+76 integrações MongoDB8.0.17 autenticado/replica set,0 falhas/0 skips; inclui ContainerSecurity. Build0 avisos/erros, auditoria sem vulnerabilidades e Gitleaks~931KB sem segredos. Prova adicional de rótulos de commit identificou correção pequena ainda não publicada: transiente no commit deve repetir corpo; ambíguo repete somente commit. Testes reais3 passaram; acrescentado caso de tentativas esgotadas e regressão final em execução, portanto06 ainda não iniciado. Uma regressão pós-prova recebeu503 em login de preparação de ProductTests; demais77 integrações e93 unitários passaram. Nova execução limita uma fixture Atlas por vez, preservando a concorrência interna; CI mantém2 fixtures.

RF-09–12 implementados (saldo, entradas/saídas/ajustes por delta, histórico, policies, transação/idempotência/auditoria). Evolução explícita05-stock/version6; nenhuma sales. Inventário, comandos e evidências completos em [10-ENTREGA-ESTOQUE-VENDAS.md](10-ENTREGA-ESTOQUE-VENDAS.md). Lidos índice/estado, arquitetura/requisitos, módulos05/06 e execução/ADRs. Restore locked autorizado passou; build Release final0 avisos/erros; arquitetura válida. `dotnet test GestaoComercial.slnx -c Release --no-build -m:1 --filter 'Category!=ContainerSecurity' --logger trx --results-directory artifacts/block05/gate`:75 integrações Atlas+93 unitários =168 aprovados,0 falhas/0 ignorados. O teste ContainerSecurity exige perfil Testcontainers autenticado e será executado no CI, ainda sem aprovação. Duas fixtures simultâneas mantêm disputas internas paralelas. Falhas transitórias anteriores e build concorrente a testes registrados no relatório, sem aumentar orçamento ou remover assert.

M05-CA01–05: saldo0→entrada10→saída2→ajuste-1=7; duas saídas independentes disputam saldo1 (uma201/outra409); repetição paralela e resposta perdida preservam recibo/um efeito, corpo divergente/outro ator409; corrupção sem saldo409 sem autocriar; Estoquista ajuste403. Validator da auditoria força rollback de saldo/movimento/recibo; mesma chave aplica uma vez após restauração. Concorrência com inativação de produto preserva status e saldo/razão. Upgrade04→05/idempotência/checksum/BSON inválido demonstrados. Query10mil com explain passou (302ms total/49ms servidor no primeiro ensaio); não é prova de carga/p95. Jornada HTTPS real e capturas360/1280 revisadas,0 JS/5xx; layout do fieldset corrigido após inspeção e roteiro completo repetido. Bancos isolados removidos. Gitleaks8.24.2 no snapshot preparado188 arquivos/~939KB:0 segredos;43 Markdown/117 links locais válidos; .env ignorado e hash preservado. Sem evolução do banco de desenvolvimento, deploy ou merge.06 aguarda prova final de runtime/CI05.

## Execução02/03/04 — 07/11/2026 (America/Cuiaba)

Solicitação inicial autorizou02 e03 sequencialmente. Solicitação complementar autorizou04 após os gates anteriores, além de commit/push/PR e acompanhamento do CI, sem deploy/merge. Base main/05ee7c5; checkout inicialmente limpo (727a563 é o commit da CI histórica).01 aprovado conforme evidências históricas abaixo. Não usar os parágrafos antigos de autorização como restrição à solicitação atual.

### Bloco02 — CONCLUÍDO, gate aprovado

Implementados CRUD lógico de employees, paginação/busca por prefixo/status, contato validado no Domain, conta humana via Identity, vínculo único inclusive inativos, senha temporária por transportes privados e formulário SSR/antiforgery, revogação atômica de conta/sessões ao desativar funcionário e reativação sem acesso automático. Ativação de conta exige funcionário ativo. Leitura atual de sessão confere funcionário vinculado no primary. Último Admin protegido também por funcionário; ADR-31 resolve a contradição após a remoção de Gerente.

Evolução explícita02-employees/version3: employees com validator strict/error, índice nome/id e users.employeeId unique parcial binData; preserva usuários/auditoria do01. Readiness exige02. Nenhuma DDL no startup. API GET/POST /api/v1/funcionarios e GET/PUT /{id}; POST /api/v1/usuarios. UI /funcionarios, /funcionarios/novo, /funcionarios/{id}, /usuarios/novo e vínculo no detalhe da conta.

Arquivos: Domain/Cadastros/Contato.cs; Application/Funcionarios/IFuncionarios.cs e Acesso/IAutenticacao.cs; Infrastructure/Funcionarios/{FuncionarioDocumento,FuncionariosSchema}.cs e Identity/{MongoFuncionarios,MongoAutenticacao,RegistroAcesso}.cs; Api/Acesso/{EndpointsFuncionarios,EndpointsAcesso,AutenticacaoHttp}.cs e Program.cs; Contracts/Funcionarios.cs; Web/ApiClients/{AcessoApiClient,FuncionariosApiClient}.cs, Security/ContasHttp.cs, Components/Pages/{Funcionarios,FuncionarioDetalhe,UsuarioNovo,UsuarioDetalhe}.razor, LayoutPrincipal e app.css; testes EmployeeTests, EmployeeBffTests, ContactTests e ajustes de expectativas/fixtures em HostTests, FoundationTests e AuthenticationTests; AGENTS e documentos normativos afetados.

Comandos reais: restore --locked-mode --disable-parallel -m:1 /nodeReuse:false passou nos8 projetos; build Release --no-restore -m:1 /nodeReuse:false -p:UseSharedCompilation=false passou0 avisos/erros. Execução inicial:41 integrações aprovadas,46 unitários aprovados/1 falha por expectativa antiga de ausência de POST usuários. Expectativa atualizada para o incremento02:47 unitários aprovados/0 falhas/0 skips. Verificação de arquitetura passou. As primeiras tentativas restritas falharam por DNS/NuGet e comunicação de processos; comandos com acesso autorizado e compilação sem servidor concluíram. Nenhuma falha foi escondida.

E2E real: .local/employees-e2e.cjs sobre Atlas exclusivo, bootstrap/login/troca obrigatória, criação de funcionário, conta via SSR, desativação confirmada, lista ativo/todos, cookie/HTML/storage/logs sem credenciais e teclado, layouts360/1280 sem overflow;0 erros JS/5xx. Capturas e resultado em artifacts/block02/ui; bancos exclusivos removidos. Imagens revisadas. Rótulos herdados do roteiro01 foram corrigidos no relatório para descrever somente as ações realmente executadas. Não houve teste adicional de revalidação60s neste roteiro; a regressão automatizada01 permanece.

M02-CA01–05 demonstrados pelos testes EmployeeTests e EmployeeBffTests; teste próprio do último Admin e rollback por validator de auditoria. Concorrência usa requests/sessões independentes com barreira de início. DoD financeiro/estoque/vendas não aplicável: nenhum desses recursos criado. Regressão final:41 integrações +47 unitários =88 aprovados,0 falhas/0 skips, TRX em artifacts/block02/final. Evolução aplicada explicitamente em Development por dotnet backend/GestaoComercial.Api/bin/Release/net10.0/GestaoComercial.Api.dll --initialize-persistence, exit0 e topologia transacional Atlas confirmada. Gate02 aprovado;03 iniciado em sequência;04 aguarda gate03.

### Bloco03 — CONCLUÍDO, gate aprovado

RF-05 entregue; RF-06 parcial planejado, histórico tipado vazio e sem coleção sales. Cliente PF/PJ, documento opcional com dígitos verificadores, nome/contatos normalizados, documento único inclusive inativos; pesquisa por prefixo de nome, documento/email exatos, tipo e status, paginação no servidor. POST/PUT transacionais com auditoria, versão e guarda técnica. Administrador/Vendedor permitidos e Estoquista403; anônimos401 em todos os verbos. Desativação/reativação lógica preservam dados. Nenhum login/usuário de cliente criado.

Evolução03-customers/version4 explícita, strict/error, unique parcial documento string, nome/id. Instalação limpa/upgrade/reexecução, preservação e rejeição BSON inválido comprovadas. Aplicada explicitamente no Development via CLI --initialize-persistence, exit0; .env preservado. Readiness exige03; startup não altera schema.

Arquivos novos: Domain/Cadastros/DocumentoCliente.cs; Application/Clientes/{IClientes,CadastroException}.cs; Infrastructure/Clientes/{ClienteDocumento,ClientesSchema,MongoClientes}.cs e Persistence/OperacoesCadastro.cs; Api/Acesso/EndpointsClientes.cs; Contracts/Clientes.cs; Web/ApiClients/ClientesApiClient.cs e Pages/{Clientes,ClienteDetalhe}.razor; testes CustomerDocumentTests, CustomerTests, CustomerBffTests. Alterados composição API, handler de erros/policies, Web policies/menu e fixtures/OpenAPI, além de documentos normativos. ADR-32 registrada antes do código.

Comandos reais: restore locked com -m:1/--disable-parallel, build Release --no-restore -m:1 /nodeReuse:false -p:UseSharedCompilation=false (0 avisos/erros), dotnet test solução --no-build -m:1 --logger trx:48 integrações +58 unitários aprovados,0 falhas/0 skips. Teste BFF acrescentado em seguida foi compilado e executado com --filter FullyQualifiedName~CustomerBffTests:1 aprovado; total de casos disponíveis107 (49 integração/58 unidade). Arquitetura aprovada. TRX em artifacts/block03/tests e bff. Concorrência documento/versão com requests/sessões independentes e barreira; falha deliberada da auditoria aborta POST/PUT sem alterar cliente. Sem falhas ignoradas.

E2E .local/clients-e2e.cjs: Atlas exclusivo, HTTPS confiado, Edge headless; cliente criado/alterado/desativado, documento pesquisado com ativo/todos, histórico vazio, tipo PJ incompatível com CPF rejeitado e foco no erro,360/1280 sem overflow, teclado, cookie seguro e HTML/storage/logs sem credenciais;0 erros JS/5xx, bancos removidos. Capturas revisadas e relatório em artifacts/block03/ui. M03-CA01–05 comprovados; financeiro/vendas/estoque não aplicáveis neste incremento. Q-01/03 preservadas para homologação/dados reais.

Gate03 aprovado;04 autorizado pela solicitação complementar e iniciado após esta evidência. Não iniciar05.

### Bloco04 — CONCLUÍDO, gate aprovado; publicação Git/CI em andamento

RF-07/08 entregues: categorias/marcas/produtos, SKU e nomes normalizados únicos, preço long centavos BRL com conversão HTTP exata, descrição simples, mínimo, status, saldo inicial incorporado zero e imagem privada. CRUD protegido, pesquisas por prefixo/SKU, filtros/paginação, versões e auditoria. Todos os três perfis leem; Admin/Estoquista escrevem. Guarda categoria→marca→produto protege referências contra inativação concorrente. Edição pontual não substitui saldo/imagem. API e UI completas conforme tabela do módulo04; endpoint Web de bytes /imagens/produtos/{id}, sem token em URL/HTML.

Evolução04-products/version5 cria categories/brands/products com validators strict/error e índices de nome/sku/referência-ativo-id. Produto/saldo zero/auditoria atômicos; não há coleções de movimentos/vendas. Upgrade03→04 preserva cliente anterior e reexecuta sem sobrescrita; checksum divergente falha; BSON double em preço e saldo negativo são recusados. Evolução aplicada explicitamente em Development, exit0, Atlas transacional confirmado. .env preservado por comparação byte/hash com registro anterior.

Imagens JPEG/PNG reais,2MiB/4096×4096, decodificadas/reencodadas por SkiaSharp3.119.4 sem metadados, volume privado e chave UUID.png. Arquivo novo fora da transação; metadado/auditoria/versão juntos; arquivo anterior removido após commit conhecido. Falha ou commit incerto mantém arquivos; manutenção explícita exige esquema04 e escritas paradas, remove somente UUIDs não referenciados. Compose acrescenta productimages e owner app; Docker não executado localmente. Nenhuma autorização de produção/deploy.

Arquivos novos04: Domain/Cadastros/DadosProduto.cs; Application/Produtos/IProdutos.cs; Infrastructure/Produtos/{DocumentosProduto,ProdutosSchema,ImagensPrivadas,MongoProdutos}.cs; Api/Acesso/EndpointsProdutos.cs; Contracts/Produtos.cs; Web/ApiClients/ProdutosApiClient.cs e Pages/{Produtos,ProdutoDetalhe,Referencias}.razor; testes ProductRulesTests, ProductTests, ProductBffTests, ProductsUpgradeTests. Alterados DI/CLI/readiness/policies, cliente HTTP/erros, proxy/menu/CSS, lockfiles/Directory.Packages, Dockerfile/Compose, .env.example e documentação. Inventário integral dos arquivos pendentes no [relatório](09-ENTREGA-CADASTROS.md).

Validação executada: restore locked nos8 projetos, build Release0 avisos/erros; suíte completa com77 unitários+61 integrações =138 aprovados. Acrescentada disputa de imagens e guarda da limpeza, ProductTests reexecutado:11 aprovados. E2E real04 passou em360/1280 com teclado, preço pt-BR/erro com foco, saldo0, categoria/marca, upload/proxy, referência em uso e busca SKU;0 JS/5xx, bancos isolados removidos, capturas revisadas em artifacts/block04/ui. Primeiro roteiro precisou esperar rerender após selecionar categoria; corrigido e repetido integralmente.

Nova regressão com139 casos:138 passaram e1 disputa02 recebeu503. API já prevê503 quando o orçamento de retries se esgota; não houve evidência de violação de integridade. Teste agora resolve o resultado por GET e, se ainda ativo, repete PUT com versão relida, no máximo2 vezes, exigindo desativação confirmada e nenhuma conta ativa. Não considera503 sucesso nem aumenta orçamento da aplicação. Teste afetado reexecutado:1 aprovado; suíte final em artifacts/block04/verified:62 integrações +77 unitários =139 aprovados,0 falhas/0 skips. Build final0 avisos/erros. Gate04 aprovado, sem pendência P0/P1 neste incremento. A resposta503 histórica acima não foi contada como sucesso nem removida da evidência. Próximo05 disponível somente mediante nova autorização; não iniciado.

Arquitetura passou; auditoria de dependências reportou0 vulneráveis. Snapshot Git preparado verificado com Gitleaks8.24.2 (~851KB),0 segredos; verificação documental42 Markdown/110 links locais,0 erros, .env não rastreado e nenhuma URI/senha real no conteúdo candidato. Branch feature/block-04-products criada a partir de05ee7c5; main remota coincide, sem proteção/rulesets definidos. PR preservará main. Commit de implementação6d21cb3935ab39c19e49104d29175c63383bc48a enviado com sucesso a origin/feature/block-04-products. [PR1](https://github.com/argemon/MVPMilton/pull/1) aberto para main, sem merge. CI de [push42346433471](https://github.com/argemon/MVPMilton/actions/runs/42346433471) e [PR42346477960](https://github.com/argemon/MVPMilton/actions/runs/42346477960) concluídas success, conferidas até o fim de todas as etapas em07/11/2026. Ubuntu24.04/SDK10.0.401, build Release0 avisos/erros,77 unitários+62 integrações Testcontainers/MongoDB8.0.17 replica set =139 aprovados,0 falhas/0 skips. SkiaSharp e imagens passaram também no Linux. Auditoria de dependências aprovada e Gitleaks do snapshot (~852KB no runner) sem segredos. Artefato evidencias-fundacao publicado com TRX, auditoria e Gitleaks. Nenhuma correção de CI foi necessária; workflow funcional anterior preservado. Este registro final é documental, sem alterar o código validado. Snapshot exato do commit:170 arquivos/~861KB, Gitleaks8.24.2 sem segredos. A última tentativa de copiar o índice antes do push falhou e produziu scan vazio, que não é evidência; a verificação anterior de851KB havia passado. A cópia foi refeita via git archive do commit enviado, contagem conferida e scanner válido repetido,0 ocorrências. Verificação documental final:42 Markdown/112 links locais,0 erros.

## Histórico aprovado — Bloco01, 06/11/2026 (America/Cuiaba)

### Publicação GitHub e primeira execução CI (06/11/2026)

Envio autorizado pelo usuário para argemon/MVPMilton/main: commit a8146cd. .env permaneceu ignorado, não rastreado e fora do índice;124 arquivos publicados, sem correspondência da URI/senha fornecida. Checkout local limpo após o commit.

[Execução42330495471](https://github.com/argemon/MVPMilton/actions/runs/42330495471): arquitetura, restore locked, build Release (zero avisos/erros),40 testes unitários e32 integrações passaram em Ubuntu24.04/SDK10.0.401 com Testcontainers/MongoDB8.0.17 replica set. Auditoria de dependências passou. Perfil Docker/CI agora possui evidência real; a indisponibilidade de Docker registrada abaixo refere-se somente à máquina Windows local. Nenhuma credencial Atlas foi enviada ao Actions.

O scanner de segredos sobre todo o diretório pós-build sinalizou1 ocorrência sem informar arquivo. A mesma versão oficial Gitleaks8.24.2 sobre um git archive exato de a8146cd encontrou zero segredos (~646KB). A CI foi corrigida para verificar somente o conteúdo integral publicado do commit, em diretório temporário/read-only, separado de .git/config, chaves/senhas efêmeras de testes e artefatos gerados. Não há regra desabilitada, allowlist ou supressão de erro: qualquer ocorrência no commit continua falhando a execução. Relatório JSON com redação100% é anexado às evidências.

**CI APROVADA:** [execução42330792233](https://github.com/argemon/MVPMilton/actions/runs/42330792233), commit727a563, conferida até a conclusão success de todas as etapas. Build Release0 avisos/0 erros;40 unitários +32 integrações aprovados,0 skips; auditoria de dependências sem vulnerabilidades reportadas; Gitleaks verificou o snapshot publicado (~640KB), resultado no leaks found. Relatórios TRX, vulnerabilidades e Gitleaks publicados no artefato evidencias-fundacao. A ocorrência do primeiro diretório de execução não foi reproduzida no conteúdo versionado; não se atribui origem exata a um arquivo que o primeiro log não identificou. .env/Atlas não fazem parte do Git nem da configuração Actions. Este registro é somente evidência;02 não foi iniciado.

**01 AUTENTICACAO concluído e aprovado para avançar no caminho de desenvolvimento .NET10 + MongoDB Atlas.**00 permanece aprovado, com regressões revalidadas.02 FUNCIONARIOS está disponível somente mediante nova autorização e não foi iniciado. Esta aprovação não libera publicação/produção.

RF-01/02 completos; RF-03 parcial conforme incremento aprovado: consulta/manutenção de perfil/status/redefinição. Criação humana e vínculo aguardam02. Nenhum cliente, produto, estoque, venda, dashboard ou relatório foi implementado.

### Entrega e decisões

Lidos AGENTS, arquitetura/requisitos, módulo01, execução03/04 e contratos existentes. Documentos sincronizados antes do código: ADR-30 registra os três perfis solicitados (Administrador, Vendedor, Estoquista), preservando permissões limitadas e BFF/cookie/Identity da arquitetura aprovada. Perfis e referências normativas foram atualizados nos documentos afetados; ADRs/auditorias históricas preservados. Nenhuma credencial foi colocada em Markdown.

- Modelo de acesso independente de MongoDB/Identity em Domain/Application; stores mínimos Identity na Infrastructure, hash oficial versionado com salt, rehash e email normalizado único inclusive inativos.
- Login sem enumeração por mensagem/status; cinco falhas bloqueiam15min. Limites: Web30/min/IP, API privada300/min/IP e10/min/hash do identificador, memória limitada, sem log de senha/email de tentativa.
- Sessões API:32 bytes aleatórios Base64URL, somente SHA-256 persistido;8h absolutas/30min inativas. Cada chamada lê sessão/usuário atuais no primary com readConcern linearizable, sessão não causal e maxTime3s. Me/health/revalidação não prolongam inatividade.
- Cookie __Host-GestaoComercial Secure/HttpOnly/Lax, referência ITicketStore. Tickets/token cifrados por Data Protection no banco Web, key ring persistente. Nada de JWT, token em localStorage, credencial em DefaultRequestHeaders compartilhado ou claims sensíveis.
- Login/logout/troca por HTTP SSR + antiforgery; returnUrl local; erros401/400/429/503 com formulário seguro. Revalidação de circuito60s, navegação completa ao perder sessão, autorização real na API. Logout com API offline remove ticket/cookie local; Origin cruzado é negado no hub.
- Bootstrap CLI único via segredo externo, sem signup/criação pública. Troca obrigatória, redefinição administrativa, mudanças de perfil/status revogam sessões transacionalmente. Perfil único e negação por padrão; policies de contas somente Administrador, troca pendente só permite me/logout/senha.
- PUT versionado, guarda administrativa efetiva antes da contagem de Admins, proteção do último acesso em disputa. Usuários/sessões/auditoria compartilham a mesma sessão snapshot/majority/primary, operações sequenciais. Repetições transitórias limitadas a duas e orçamento5s; commit incerto repete somente commit.
- Inicialização explícita aplica01 sobre00: users, api_sessions, audit_events, administration_control; Web tickets. Validadores strict/error, índices nome/id, email unique, token/handle unique, usuário/expiração e auditoria/recurso/data. Catálogo version2/checksum; reexecução sem perda nem sobrescrita incompatível. Nenhuma evolução no startup.

### Arquivos criados/modificados no01

| Área | Arquivos |
| --- | --- |
| Domain/Application | backend/GestaoComercial.Domain/Acesso/Perfis.cs; backend/GestaoComercial.Application/Acesso/IAutenticacao.cs |
| Infrastructure | GestaoComercial.Infrastructure.csproj e lockfile; Identity/Documentos.cs, MongoUserStore.cs, MongoAutenticacao.cs, AuthSchema.cs, RegistroAcesso.cs |
| API | Program.cs; Acesso/AutenticacaoHttp.cs e EndpointsAcesso.cs; lockfile afetado pela referência framework da Infrastructure |
| Contracts | shared/GestaoComercial.Contracts/Autenticacao.cs (somente DTOs públicos, sem senha/token) |
| Web | Program.cs, Configuracao.cs, Properties/launchSettings.json; Security/MongoTickets.cs, SessaoCircuito.cs, ContasHttp.cs; ApiClients/AcessoApiClient.cs |
| Componentes | _Imports.razor, Routes.razor, LayoutPrincipal.razor; FormularioConta.razor, DocumentoConta.razor, RedirecionarAcesso.razor; Pages/Entrar.razor, Senha.razor, AcessoNegado.razor, Usuarios.razor, UsuarioDetalhe.razor, InicioTecnico.razor, PaginaErro.razor, PaginaIndisponivel.razor; wwwroot/app.css |
| Testes | UnitTests/AuthenticationRulesTests.cs e HostTests.cs; IntegrationTests/AuthenticationTests.cs, AdministrationTests.cs, AuthAtomicityTests.cs, BffTests.cs e FoundationTests.cs; lockfiles afetados |
| Configuração/documentação | .env.example, deploy/compose.yaml, README.md, AGENTS.md; docs/README.md, arquitetura05/07 e demais referências de perfis, requisitos/matriz e referências de atores, módulos01 e referências normativas de perfis nos demais módulos, execução00/01/02/03/04 |

Arquivos de00 já estavam no workspace antes desta etapa e não constituem novas entregas de01. Sem commit/push ou envio de credenciais. .env foi preservado byte a byte e permanece ignorado. Scripts/evidências auxiliares locais em .local/artifacts não são código de produção nem incluem novas funcionalidades de blocos seguintes.

### Validação executada

| Verificação | Resultado real |
| --- | --- |
| dotnet restore GestaoComercial.slnx --locked-mode | Sucesso nos8 projetos, versões fixadas/lockfiles |
| dotnet build GestaoComercial.slnx -c Release --no-restore | Sucesso,0 avisos/0 erros |
| dotnet test GestaoComercial.slnx -c Release --no-build --logger trx | **72 aprovados:40 unitários +32 integrações;0 falhas/0 skips**. Inclui regressão00, hosts e BFF com API real e MongoDB Atlas isolado |
| scripts/verify-architecture.ps1 | Seis projetos de produção, referências corretas; Domain/Application/Contracts sem Driver/BSON/ASP.NET/Identity |
| Inicialização explícita nos dois hosts Development | Sucesso sobre baseline00 existente em gestao_comercial_dev/gestao_web_dev, sem alteração de credenciais ou criação de usuário. Reexecução/instalação vazia também cobertas por integração |
| Bootstrap real por CLI | Sucesso e troca obrigatória comprovados apenas em banco E2E descartável, com credenciais sintéticas efêmeras. Bootstrap de desenvolvimento não executado por falta de segredo de aplicação escolhido pelo responsável |
| API e Web reais, HTTPS local confiado, Edge headless | Login SSR → troca obrigatória → novo login → home → consulta/filtro/edição → último Admin409 → logout → rota protegida. Comunicação pelos hosts7143/7243 e health8081 sem bypass TLS |
| Circuito após logout em outra aba | Redirecionou para login em **58 segundos**, dentro do intervalo de60s;0 erros JavaScript |
| Layout/teclado | Capturas360/1280, sem overflow da página; Tab operável. Capturas inspecionadas localmente |
| Segredos/configuração | SHA-256 do .env idêntico ao anterior; arquivo ignorado/não rastreado; busca dirigida sem URI/senha fornecida em arquivos versionáveis/evidências; HTML/storage/logs do E2E sem credenciais |
| Auditoria de dependências | dotnet package list --vulnerable --include-transitive --format json, sucesso, nenhum pacote vulnerável reportado em artifacts/vulnerabilities-auth.json |
| Checagem final dirigida |124 arquivos versionáveis e42 evidências textuais locais verificadas,0 ocorrências dos segredos fornecidos,109 links locais válidos, .env intocado/não rastreado; resultado em artifacts/auth-final-check.json |
| OpenAPI | Contratos de auth/me/usuários presentes; sem POST usuários nem funcionários; validado em teste de host |

Evidências finais em artifacts/tests/entrega-01-final (TRX por assembly), artifacts/auth-e2e/resultado.json, capturas login/home360/1280 e logs locais. Bancos exclusivos de integração e os dois bancos E2E foram removidos pela própria fixture, limitados aos nomes reservados. Hosts/navegador do ensaio foram encerrados. O teste de falha de auditoria alterou temporariamente somente o validator do seu banco descartável e comprovou rollback de perfil/stamp/sessões.

### Rastreio dos critérios

| Critério | Evidência observada |
| --- | --- |
| M01-CA01 / CA-01 | Bootstrap CLI, sessão opaca/cookie de referência, login, troca, novo login e logout reais; senha/hash/store e login inválido/inexistente/inativo automatizados |
| M01-CA02 / CA-02 | Duas sessões, troca/perfil/inativação/redefinição revogam; expiração30min/8h com relógio controlado; revalidação Web e logout em outra aba58s |
| M01-CA03 / CA-17 | Dois Admins/sessões independentes e barreira concorrente: uma alteração, outro409 ULTIMO_ADMINISTRADOR, um Admin ativo preservado |
| M01-CA04 / CA-17 | Logout sem antiforgery400 e sessão continua válida |
| M01-CA05 / CA-01 | API offline, logout remove ticket/cookie e nova navegação exige login; aviso seguro sem token |
| Autorização / CA-17 | Anônimo401, Vendedor/Estoquista403 e Admin200, URL administrativa protegida no Web; perfil desconhecido e BSON inválido rejeitados |
| Integridade | Email único inativo/normalizado, stores CAS, falhas de login concorrentes sem perder contador, lockout/rate429, inicialização idempotente e divergência bloqueada, rollback real por falha de auditoria |

### Problemas resolvidos e limites reais

Falhas intermediárias foram corrigidas e a suíte foi repetida: ordem dos POSTs SSR para não disputar rota de componente; preservação do code customizado em ProblemDetails; inicialização01 exigida no readiness; separação de Api__BaseUrl de Api__HealthBaseUrl (porta privada recusa login por desenho); backoff do retry concorrente ajustado; ticket revogado removido antes de novo login. Correções não antecipam módulos futuros.

- Para usar o ambiente Development, o responsável deve escolher nome/email/senha de aplicação e executar --bootstrap-admin conforme README. Não usar a senha MongoDB como senha do sistema. Nenhuma senha padrão foi inventada; esta é configuração operacional, não funcionalidade ausente.
- Docker/Compose/Testcontainers/CI não foram executados: Docker não está instalado. Atlas isolado real foi o caminho comprovado conforme ADR-28; não houve substituto em memória.
- A conta Atlas de desenvolvimento continua compartilhada entre bancos somente conforme ADR-27. Usuários de runtime/evolução separados, RBAC de coleção/sem leitura cruzada, proteção operacional das chaves, domínio/proxy/TLS de produção, backup/restore/Q-02/Q-03 continuam gates de publicação. Não foram criadas contas/roles do cluster.
- Limitadores em memória pressupõem uma instância; TTL não habilitado (expiração sempre validada no backend). Logout com API indisponível pode deixar token remoto inacessível pelo navegador até expirar, risco já aceito na arquitetura07.
- Eleição/failover e failpoints de commit incerto não foram induzidos no Atlas compartilhado. O retry segue rótulos do driver; conflitos reais/rollback/transações foram executados. Nenhuma promessa de prontidão de produção ou HA.

**Próximo bloco:02 FUNCIONARIOS**, para vínculo e criação humana autorizada. Não iniciado automaticamente.

---

## Histórico preservado00 e auditorias anteriores

# 03 — Estado do projeto

## Ajuste da tela inicial — 09/11/2026

A pedido do responsável após captura da home: rota / agora apresenta área de trabalho com atalhos de nova venda, clientes, estoque e produtos, consultas secundárias e destaque ao dashboard somente para Administrador. Cada atalho usa a policy existente; troca obrigatória de senha continua bloqueando atalhos. Removida consulta técnica de readiness da home, mantendo MeAsync e tratamento de falha de sessão. Sem regra/contrato/persistência novos; .env e Atlas não acessados/alterados nesta correção. Alterações locais anteriores de login e .vs preservadas.

Arquivos desta correção: InicioTecnico.razor, app.css, complemento ADR-38 e este registro. Comando real aprovado: dotnet test tests/GestaoComercial.UnitTests --configuration Release --verbosity minimal -m:1 -p:UseArtifactsOutput=true -p:ArtifactsPath="$pwd/.local/home-build", DOTNET_CLI_HOME=.local/dotnet; 127 aprovados, 0 falhas/ignorados. Compilação dos sete projetos concluída. Primeiro teste no output padrão encontrou DLLs bloqueadas pelos hosts abertos; pasta isolada sem restore falhou por assets ausentes; restore inicial falhou por acesso NuGet e repetição com acesso autorizado concluiu. git diff --check sem erros. Sem novos testes de integração, pois apenas composição visual/atalhos, sem comportamento transacional alterado.

Limites: nova composição ainda não inspecionada em navegador real; aplicação já aberta continua executando a compilação anterior e precisa ser reiniciada para carregar a nova tela. Sem commit/push/CI desta correção; não atribuir CI histórico ao código atual. Próxima ação: reiniciar Web, conferir início nos três perfis e larguras do bloco10. Nenhum bloco adicional, deploy ou merge.

## Histórico aprovado — implementação00 com Atlas (06/11/2026, America/Cuiaba)

**00 Fundação concluído e aprovado no caminho de desenvolvimento .NET+Atlas autorizado pelas ADR-27–29.** Próximo bloco disponível:01 Autenticação, somente por nova solicitação; não iniciado. A aprovação não é prontidão de produção nem execução de Docker/CI. Base Git 89c3832, branch de trabalho existente, sem commit/push nesta tarefa.

| Bloco/ambiente | Estado real | Evidência / próximo passo |
| --- | --- | --- |
|00 — desenvolvimento Atlas | Concluído | Restore/build Release sem avisos,25 testes aprovados e fluxo Web/API/MongoDB em HTTPS |
|00 — Compose/Testcontainers/CI | Preparados, não executados localmente | Docker ausente; não criar instância para substituir Atlas funcional. Executar perfil opcional no ambiente com Docker |
|01 | Disponível após solicitação explícita | Reutilizar fundação, implementar somente autenticação/autorizações previstas |
|02–10 | Não iniciados | Aguardar gates anteriores; produção precisa usuários separados, RBAC, operação/TLS/backup/Q-02/Q-03 |
|11 | Proposta P2, não liberada | Aguardar00–10, aceite de escopo/Q-04 e privacidade |

### Escopo, documentos e decisões

Lidos AGENTS, arquitetura, requisitos, módulo00, execução e configuração local (somente chaves/classificação, sem imprimir valores). Solicitação atual superou a restrição documental anterior e autorizou converter00. ADR-27 registra Atlas/.env/ambientes/credencial de desenvolvimento; ADR-28 integração real em banco isolado remoto ou perfil Testcontainers; ADR-29 estrutura física conforme pedido e versões. A documentação foi sincronizada antes da adaptação e atualizada no fechamento. Requisitos comerciais/IDs/rotas e ordem de módulos preservados; nenhum login, funcionário, cliente, produto, estoque, venda, dashboard ou relatório implementado.

### Funcionalidades e arquitetura entregues

- Solução .NET10 com6 projetos de produção e2 projetos de testes. backend/Projeto, frontend/Projeto, shared/Contracts e tests/UnitTests/IntegrationTests. Domain sem dependências externas, Application referencia Domain, Infrastructure referencia Domain/Application, Api compõe Infrastructure/Application/Contracts; Web referencia só Contracts e possui armazenamento técnico independente.
- MongoDB.Driver oficial3.11.2, IMongoClient singleton por host/configuração e sessões curtas. Código EF/Npgsql/factories/SQL/migrations removido da árvore ativa; legado local guardado fora de versionamento para preservar trabalho anterior. Sem entidades fictícias em Domain/Application.
- Loader .env Development preserva o arquivo, aceita valores literais/aspas/vazio, nunca expande comandos e dá precedência ao ambiente do processo. Staging/Production/hosts Testing não carregam .env. Connection string validada sem ecoar segredo, ambiente/banco/API URL/TLS validados. Arquivo .env ignorado pelo Git e excluído de imagem/publish; .env.example ilustrativo criado.
- Atlas/SRV fornecido reutilizado com sucesso, sem criar outra instância. Nome do replica set descoberto por hello, sem fixar rs0. Primary/sessões/topologia conferidos; transações multi-documento comprovadas por commit/abort real em duas coleções descartáveis. Versão exata do servidor Atlas não foi consultada/fixada, por ser serviço gerenciado; compatibilidade usada comprovada na execução, sem atribuir versão fictícia ao servidor.
- Inicialização explícita --initialize-persistence de API/Web cria somente persistence_versions nos bancos gestao_comercial_dev/gestao_web_dev: validator strict/error, índices _id e version unique, ID/schemaVersion/checksum/appliedAt. Reexecução idempotente preserva documento e falha em checksum/definição incompatível; startup normal não muda schema. Em Staging/Production, tarefa exige Evolution__ConnectionString externo.
- Health mínimo live200/ready200 ou503, readiness somente listener interno e inclui conexão/topologia/baseline. Erros ProblemDetails genéricos com traceId/code, sem stack/URI; logs do driver e handlers HTTP sensíveis desativados. Rotas não liberadas negadas sem autenticação simulada.
- Blazor Interactive Server pt-BR, home/erro/indisponível, estado real da API e Repetir por HttpClient tipado HTTPS. TLS validado normalmente no ensaio local. CA privada opcional restrita ao cliente HTTP da API, sem substituir raízes do Atlas. Layout/teclado conferidos em360/1280.
- Compose de desenvolvimento API/Web usa Atlas externo e não contém serviço PostgreSQL/MongoDB substituto. Dockerfiles/pipeline/path checks adaptados. CI configura Testcontainers.MongoDb4.14.0/mongo8.0.17/rs0 isolado sem segredos Atlas. Chaves Web em volume app-owned; este perfil não foi executado aqui.

### Configuração e segurança observadas

O .env existente permaneceu byte a byte idêntico (SHA-256 comparado sem publicar o hash). Password/URI não foram copiados para Markdown nem encontrados nos arquivos versionáveis/evidências TRX escaneados; .env não está no índice Git. Foram examinados97 arquivos ativos e23 evidências TRX, zero ocorrência exata dos segredos fornecidos. Essa checagem dirigida não substitui um scanner completo de segredos; gitleaks permanece no CI e não foi executado localmente (Docker ausente).

Somente a conta Atlas fornecida é reutilizada em Development, explicitamente na ADR-27. Não foi criada/alterada role/conta do cluster nem alegada comprovação de RBAC de produção. Web usa apenas seu banco no código, porém isolamento de privilégio entre contas requer credenciais separadas externas para produção e é pendência operacional antes do10. Dados reais de negócio não foram usados.

### Comandos efetivamente executados e resultados

| Comando/roteiro | Resultado observado |
| --- | --- |
| dotnet --version; dotnet dev-certs https --check | SDK10.0.401; certificado local válido encontrado. Roundtrip HTTPS abaixo comprovou confiança sem bypass |
| dotnet restore --disable-parallel; dotnet restore --locked-mode | Sucesso;8 projetos restaurados, versões transitivas em lockfiles |
| dotnet build --no-restore -c Release | Sucesso final,0 avisos/0 erros |
| dotnet format whitespace GestaoComercial.slnx --no-restore --verbosity quiet | Concluído; build e testes repetidos depois |
| dotnet run --project backend/GestaoComercial.Api -c Release --no-build -- --probe-mongo | Atlas/SRV: conexão e topologia transacional disponíveis; somente resultado seguro, sem URI |
| dotnet run --project backend/GestaoComercial.Api -c Release --no-build -- --initialize-persistence | Sucesso; catálogo técnico apenas no banco API Development |
| Comando anterior no frontend/GestaoComercial.Web | Sucesso; catálogo técnico apenas no banco Web Development |
| Reexecução de --initialize-persistence nos dois hosts após encerrá-los | Sucesso, dados e índice preservados, sem recriar banco ou sobrescrever registro |
| dotnet test tests/GestaoComercial.UnitTests -c Release --no-build |20 testes passaram inicialmente; caso adicional do limite de banco fez total final21 |
| dotnet test tests/GestaoComercial.IntegrationTests -c Release --no-restore |4 testes reais passaram após corrigir limite do nome descartável; operação de commit/abort ampliada para duas coleções |
| dotnet test -c Release --no-build --logger 'trx;LogFilePrefix=entrega-atlas' --results-directory artifacts/tests | **25 aprovados:21 UnitTests (incluem hosts sem DB) +4 IntegrationTests reais Atlas;0 falhas e0 skips** |
| ./scripts/verify-architecture.ps1 | Seis projetos de produção presentes e referências/pacotes de camada válidos |
| dotnet package list --vulnerable --include-transitive --format json | Nenhuma vulnerabilidade de pacote reportada em artifacts/vulnerabilities-mongodb.json |
| Hosts reais dotnet run API e Web em Development/HTTPS | API7143 e ready8081, Web7243 e ready8082 iniciados; processos de validação encerrados ao final |
| GET HTTPS normal: API live7143, ready8081, Web ready8082 e home7243 | Todos200; HTML da home mostra API disponível e cliente Web chamou API real |
| Edge headless/CDP em360 e1280, Tab e Repetir | Largura/scrollWidth360/360 e1280/1280; focoBUTTON; estado API disponível. Após parar API local, indisponível com Repetir e sem overflow nas duas larguras |
| git check-ignore .env; checagem de segredos/hash/links; git diff --check | .env ignorado e preservado, zero vazamento dirigido, links locais válidos e diff sem whitespace inválido |

DOTNET_CLI_HOME foi apontado à pasta .local/dotnet na execução para evitar diretórios externos de templates/cache; não é requisito do produto. Pacotes Microsoft ASP.NET10.0.12, xUnit2.9.3, Test SDK17.14.1 e runner3.1.1 fixados. Docker/Compose não estava disponível; nenhum container foi criado nem imagem baixada. Pipeline não foi enviado/disparado no GitHub.

### Testes e aceites comprovados

- M00-CA01: build/restore, init técnica dos dois bancos, hosts/health e tela com API disponível comprovados no caminho Atlas; Compose não executado, não necessário para o caminho atual autorizado.
- M00-CA02: unidade de host com driver apontando para conexão indisponível confirmou ready503/live200; UI verificada com API local parada. Cluster Atlas não foi interrompido nem submetido a eleição/falha deliberada; essa falha operacional não foi alegada como executada.
- M00-CA03: init idempotente preservou registro/appliedAt/índices; nova execução de processo contra o mesmo Atlas continuou pronta. Não há volume local de banco a testar no caminho Atlas; ensaio de volume/container local é não aplicável ao desenvolvimento atual e pendente no perfil opcional.
- M00-CA04: configuração ausente/inválida impede startup; teste de host e validador não expõe os valores. Production não usa alias de desenvolvimento; validação de banco/TLS/API URL coberta.
- Integração: fixture reserva banco aleatório vazio gc_m00_[24hex]_test; usa e remove somente seu nome. Commit em duas coleções torna documentos visíveis, abort em duas não produz efeitos; baseline/checksum/índice/validador reais e cliente HTTP tipado com API real em WebApplicationFactory. Teardown final executado sem erro; nenhum banco de produção/development apagado.
- Interface: screenshots artifacts/screenshots/home-atlas-360.png, home-atlas-1280.png e home-atlas-unavailable-*.png; foco/retry e métricas de viewport observados. Não é auditoria completa de acessibilidade nem teste de autenticação de01.

### Problemas encontrados e resolvidos

1. Estrutura/código anterior eram relacionais e não correspondiam à documentação MongoDB. Projetos movidos, referências/pacotes/infra/tests substituídos, sem reutilizar aprovação antiga.
2. Build acusou construtor Testcontainers obsoleto com warnings-as-errors. Corrigido para construtor com imagem explícita; build final sem avisos.
3. Primeira integração falhou em comando Atlas código8000 porque o nome de44 caracteres da fixture excedia o limite de38 bytes do plano compartilhado. Corrigido para36 caracteres/96 bits aleatórios e validador de configuração conservador. Referência oficial vinculada nas ADRs; credencial/URI nunca alteradas nem expostas. Integração final passou.
4. Parser .env revisado para valor vazio sem indexar string vazia, com teste; defaults/overrides separados por ambiente. Arquivo real preservado.
5. Certificados locais/CA privada e raízes do Atlas não podem compartilhar um override global de trust. Configurada CA opcional somente no HttpClient interno; sem desabilitar validação de certificado/hostname.

### Pendências reais e avaliação do gate

Não há bloqueio conhecido para iniciar01 no ambiente Development Atlas validado, mediante pedido do usuário. **Não avançar automaticamente.** Docker/Compose/CI/Testcontainers e scanner completo ainda precisam execução no ambiente apropriado; nenhum sucesso desses perfis foi alegado. Publicação continua bloqueada antes de10 por credenciais separadas/RBAC/TLS operacional/backup/restore e Q-02/Q-03; Q-01 e Q-04 mantêm os prazos originais. As operações comerciais/transacionais do05/06 não foram implementadas nem aprovadas por um teste técnico de transação00.

Primeira ação ao retomar: ler este registro e conferir arquivos/ambiente sem remover bancos/volumes nem sobrescrever .env. Para01, reutilizar MongoFoundation, configuração, contratos e cliente existentes; criar somente coleções/policies/stores e fluxos autorizados por seu módulo. Operação de schema permanece explícita. Nenhum método fictício/TODO bloqueante ou endpoint de falha foi adicionado à aplicação.

### Arquivos ativos criados/atualizados de software/configuração

- `.dockerignore`
- `.editorconfig`
- `.env.example`
- `.gitattributes`
- `.github/workflows/fundacao.yml`
- `.gitignore`
- `Directory.Build.props`
- `Directory.Packages.props`
- `GestaoComercial.slnx`
- `backend/GestaoComercial.Api/Configuracao.cs`
- `backend/GestaoComercial.Api/ErroSeguro.cs`
- `backend/GestaoComercial.Api/GestaoComercial.Api.csproj`
- `backend/GestaoComercial.Api/Program.cs`
- `backend/GestaoComercial.Api/Properties/launchSettings.json`
- `backend/GestaoComercial.Api/packages.lock.json`
- `backend/GestaoComercial.Application/GestaoComercial.Application.csproj`
- `backend/GestaoComercial.Application/packages.lock.json`
- `backend/GestaoComercial.Domain/GestaoComercial.Domain.csproj`
- `backend/GestaoComercial.Domain/packages.lock.json`
- `backend/GestaoComercial.Infrastructure/GestaoComercial.Infrastructure.csproj`
- `backend/GestaoComercial.Infrastructure/Persistence/MongoConfiguration.cs`
- `backend/GestaoComercial.Infrastructure/Persistence/MongoFoundation.cs`
- `backend/GestaoComercial.Infrastructure/packages.lock.json`
- `build/LocalEnvironment.cs`
- `deploy/Api.Dockerfile`
- `deploy/Web.Dockerfile`
- `deploy/compose.yaml`
- `frontend/GestaoComercial.Web/ApiClients/EstadoApiClient.cs`
- `frontend/GestaoComercial.Web/Components/App.razor`
- `frontend/GestaoComercial.Web/Components/LayoutPrincipal.razor`
- `frontend/GestaoComercial.Web/Components/Pages/InicioTecnico.razor`
- `frontend/GestaoComercial.Web/Components/Pages/PaginaErro.razor`
- `frontend/GestaoComercial.Web/Components/Pages/PaginaIndisponivel.razor`
- `frontend/GestaoComercial.Web/Components/Routes.razor`
- `frontend/GestaoComercial.Web/Components/_Imports.razor`
- `frontend/GestaoComercial.Web/Configuracao.cs`
- `frontend/GestaoComercial.Web/ErroSeguro.cs`
- `frontend/GestaoComercial.Web/GestaoComercial.Web.csproj`
- `frontend/GestaoComercial.Web/Persistence/MongoConfiguration.cs`
- `frontend/GestaoComercial.Web/Persistence/MongoFoundation.cs`
- `frontend/GestaoComercial.Web/Program.cs`
- `frontend/GestaoComercial.Web/Properties/launchSettings.json`
- `frontend/GestaoComercial.Web/packages.lock.json`
- `frontend/GestaoComercial.Web/wwwroot/app.css`
- `global.json`
- `scripts/verify-architecture.ps1`
- `shared/GestaoComercial.Contracts/GestaoComercial.Contracts.csproj`
- `shared/GestaoComercial.Contracts/HealthResponse.cs`
- `shared/GestaoComercial.Contracts/packages.lock.json`
- `tests/GestaoComercial.IntegrationTests/FoundationTests.cs`
- `tests/GestaoComercial.IntegrationTests/GestaoComercial.IntegrationTests.csproj`
- `tests/GestaoComercial.IntegrationTests/packages.lock.json`
- `tests/GestaoComercial.UnitTests/ConfigurationTests.cs`
- `tests/GestaoComercial.UnitTests/GestaoComercial.UnitTests.csproj`
- `tests/GestaoComercial.UnitTests/HostTests.cs`
- `tests/GestaoComercial.UnitTests/packages.lock.json`

README, AGENTS, arquitetura03/05/09/10, escopo/RNF/aceites, módulos (norma transversal), execução00/01/02/03/04/05/06/07 e índice foram sincronizados nos pontos afetados. Auditorias antigas06/08 preservadas com nota histórica. O .env real não faz parte desse inventário porque não foi alterado/versionado; somente .env.example é entregue. Arquivos antigos EF/SQL/tests legados foram retirados da árvore ativa e preservados localmente sob .local ignorado; não são dependência de build/runtime.

## Registros anteriores — histórico, sem prevalência sobre o estado acima


## Auditoria documental anterior à implementação00 — registro histórico

**BLOQUEADO**, conforme [06-AUDITORIA-MONGODB.md](06-AUDITORIA-MONGODB.md). Corrigidas inconsistências de campos/índices, tickets/recibo P2, limites do domínio, bootstrap rs0/URI, termos remanescentes e rastreabilidade dos testes; ADR-26. A especificação permite a sequência00–11, mas o item6 não está integralmente atendido pelo artefato existente: deploy/compose.yaml e hosts continuam na stack anterior. Esta auditoria alterou somente Markdown. A futura adaptação autorizada do00 deve converter infraestrutura/hosts e produzir evidências de replica set/conexão/readiness/transações antes de reauditoria.01 permanece bloqueado; nenhuma implementação iniciada.

Fechamento desta auditoria:40 documentos existentes revisados,32 corrigidos e1 relatório novo;41 Markdown atuais,113 links locais válidos,12 módulos/15 seções/62 cenários e IDs originais preservados. Hashes SHA-256 de66 arquivos não-Markdown inalterados, sem novo arquivo não-Markdown visível ao Git; diff de whitespace sem erro. Nenhum restore/build/teste/Compose/MongoDB executado nesta auditoria. Inventários completo e de alterações no relatório06 solicitado.


## Histórico — revisão documental MongoDB, 06/11/2026

Solicitação atual: atualizar somente Markdown, sem implementar/adaptar nenhum bloco. Stack alvo .NET10/ASP.NET Core/Blazor Interactive Server/MongoDB/MongoDB.Driver oficial/xUnit/Testcontainers/Compose. ADR-20–25 registram modelagem documental, centavos Int64, transações/concorrência, stores Identity e infraestrutura. Escopo, requisitos, IDs, monorepo e ordem preservados; proposta P2 continua não liberada.

| Item | Estado real vigente | Próxima ação |
| --- | --- | --- |
| Documentação MongoDB | Atualizada para revisão; validações documentais no relatório08 | Revisar ADR-20–25 e incompatibilidades, sem confundir especificação com implementação |
| 00 Fundação | Código preexistente na stack PostgreSQL/EF, incompatível com a nova baseline; gate NÃO aprovado | Futura adaptação explicitamente autorizada para MongoDB.Driver/replica set/Testcontainers, com novas evidências |
| 01–10 | Não iniciados; ordem administrativa preservada | Aguardar aprovação de00 e predecessores |
| 11 | P2 proposto, não liberado | Aguardar00–10, Q-03/Q-04 e aceite de escopo |

Nenhum código, pacote, teste executável, script SQL, Compose, Dockerfile ou workflow foi alterado nesta revisão. Hashes SHA-256 dos arquivos não-Markdown existentes foram comparados no fechamento. A evidência antiga (build/17 testes/layout) permanece como histórico da stack antiga, sem valer como teste MongoDB. Não foram executados restore/build/testes de software, inicializador, transações, containers ou backup/restore nesta tarefa documental. O roteiro07 foi convertido para especificação de adaptação futura e não anuncia comandos inexistentes.

Incompatibilidades: pacotes/hosts/contexts/migrations SQL e pipeline existentes, modelo relacional/locks, armazenamento monetário anterior e stores Identity dependentes de persistência precisam futura substituição. Sem conversão/importação de dados feita. MongoDB replica set e versões fixas/validadas de driver/servidor/Testcontainers/Tools são pré-requisitos de implementação; nenhum patch novo é alegado como testado. Docker permanecia ausente na evidência anterior, não reavaliado nem instalado nesta revisão. Próxima ação de software é00, somente quando solicitado;01 continua bloqueado.

Relatório com auditoria, alterações incompatíveis, verificação documental, pendências e inventário completo: [08-REVISAO-MONGODB.md](08-REVISAO-MONGODB.md). Requisitos alterados tecnicamente (representação/persistência) são RN-01/04 e RNF-07/08/09; seus resultados funcionais e limites são preservados e registrados, sem mudança silenciosa. Matrizes RF/RN/CA e cenários locais mantêm os IDs originais.

## Evidência da revisão documental MongoDB

Verificação final:40 Markdown,103 links locais válidos,12 módulos com15 seções e62 cenários locais preservados;24 RF/12 RNF/24 RN/18 CA nos catálogos, sem alteração de IDs/rotas originais. Árvore do monorepo idêntica à planejada originalmente. Hashes de66 arquivos não-Markdown inalterados em comparação ao início desta revisão, incluindo código/infraestrutura já presentes e não rastreados. Sem arquivo não-Markdown novo visível ao Git. Nenhum build/teste de software ou MongoDB executado nesta revisão. Inventário completo dos39 documentos atualizados e relatório novo em08.

Documentação pronta para revisão; primeira ação futura de software é adaptar00 quando autorizado. Nenhum bloco iniciado/avançado nesta revisão.

## Histórico anterior à revisão — não representa a stack vigente

As seções seguintes preservam os registros das entregas anteriores, inclusive nomes de tecnologias, comandos, arquivos e resultados reais. Leitura histórica não autoriza executá-los contra a nova arquitetura nem transferir aprovação de testes para MongoDB. A identificação/quadro/pendências antigos abaixo são históricos e são substituídos pelo estado vigente acima.

## Identificação

- Projeto: GestaoComercial.
- Data desta especificação: 06/11/2026, referência America/Cuiaba.
- Etapa atual: implementação exclusivamente do bloco 00; validação de integração pendente.
- Ambiente atual: checkout do GitHub em D:\Projetos\MVPMilton, main, base 89c3832; SDK 10.0.401/runtime 10.0.12. Histórico documental abaixo preservado.
- Software atual: solução, API, Blazor, contexts, baselines, testes e Compose criados. Containers/banco não executados neste ambiente. O gate 00 não está aprovado.

## Quadro de blocos

| Bloco | Estado | Evidência atual | Próxima ação |
| --- | --- | --- | --- |
| Documentação | Revisada | 37 arquivos Markdown; módulos 00–11 detalhados e matriz consolidada | Validar decisões propostas/hipóteses; software continua pendente |
| 00 Fundação | Implementado com validação parcial; gate bloqueado | Build sem avisos, 17 testes passam; 2 falham por PostgreSQL ausente | Concluir integração, TLS/Compose e persistência |
| 01 Autenticação | Pendente | Sem código/testes | Aguardar gate 00 |
| 02 Funcionários | Pendente | Sem código/testes | Aguardar gate 01 |
| 03 Clientes | Pendente | Sem código/testes | Aguardar gate 02 |
| 04 Produtos | Pendente | Sem código/testes | Aguardar gate 03 |
| 05 Estoque | Pendente | Sem código/testes | Aguardar gate 04 |
| 06 Vendas | Pendente | Sem código/testes | Aguardar gate 05 |
| 07 Dashboard | Pendente | Sem código/testes | Aguardar gate 06 |
| 08 Relatórios | Pendente | Sem código/testes | Aguardar gate 07 |
| 09 Homologação | Pendente | Sem código/testes | Aguardar gate 08 |
| 10 Publicação | Pendente | Sem ambiente publicado | Aguardar gate 09 e Q-02/Q-03 |
| 11 Catálogo/pedidos | Fora do MVP; bloqueado por escopo | Proposta mínima detalhada ADR-18, RF-21–24/RN-21–24 | Aceitar/ajustar Q-04 e solicitar início |

## Verificações da primeira entrega (histórico)

Checagem automatizada documental realizada em 06/11/2026 por leitura dos arquivos no PowerShell: 36 Markdown (10 arquitetura, 6 requisitos, 12 módulos, 6 execução, índice e AGENTS), zero links locais quebrados, zero cercas de código sem par, zero referências a IDs inexistentes e zero arquivos de outra extensão. Catálogos identificados: 20 RF, 12 RNF, 20 RN e 18 CA.

Revisão de coerência conferiu incremento dos módulos 01/02/03/06, autorização do gerente sobre funcionário administrador, snapshots e recibos idempotentes, locks de preço/saldo e inativação de referências. Fontes oficiais de .NET, Blazor, EF e PostgreSQL consultadas estão vinculadas nos documentos de arquitetura. Uma primeira tentativa de checagem com Python não executou porque esse comando não estava disponível; a checagem foi concluída com PowerShell, sem instalar ferramentas.

Testes de software não são aplicáveis à entrega Markdown e não foram executados. Nenhum ambiente de produção foi criado e nenhuma migration foi aplicada.

## Revisão dos módulos — 06/11/2026

Arquitetura01–10, requisitos 01–06 e módulos 00–11 foram lidos integralmente. Os 12 módulos foram completados com15 seções cada, incluindo objetivo/valor, pré-requisitos, rastreabilidade/prioridades, escopo, atores, banco, fluxos, regras/validação, contratos, UI/estados, concorrência, testes, Given/When/Then, checklist e DoD/desbloqueio. Total previsto de aceites locais:62 cenários, sem substituir CA-01–18.

ADR-01–14 preservadas. ADR-15–18 são propostas identificadas: inativo/saldo/limites/arredondamento; migrations e locks administrativos incrementais; detalhes de contratos/UI; recorte mínimo P2 de catálogo/solicitação. Documentos normativos afetados foram sincronizados e a matriz foi acrescentada como37º Markdown. RF-01–20/RN-01–20 continuam MVP; RF-21–24/RN-21–24 são propostas P2. Q-04 continua aberta.

Validação documental final por leitura/expressões regulares no PowerShell:37 Markdown,12 módulos,15 seções por módulo,62 cenários locais,24 RF (20  MVP+4  P2 propostos), 12 RNF, 24 RN (20  MVP+4  P2 propostas),18 CA globais e 18 ADR. Zero links locais quebrados, cercas de código sem par, títulos vazios, IDs inexistentes ou arquivos de outra extensão. Todos os módulos contêm requests/responses, testes previstos, checklist, comandos de build/teste e DoD.

Auditoria dos pré-requisitos extraídos da seção 2 dos módulos:00 não depende de software anterior;01–11 exigem somente a cadeia 00 até o bloco anterior. Zero dependência de bloco futuro. Revisão semântica dos incrementos confirmou bootstrap sem Funcionário, histórico vazio sem Venda, produto+saldo0 antes de movimentações, FKs de venda somente em 06 e ensaio de homologação antes do destino final. A matriz consolidada documenta essas condições e os gates externos Q-02/Q-03/Q-04.

Nenhum build, teste de software, migration ou deploy foi executado; todos os blocos de implementação continuam pendentes. Foram executadas somente verificações documentais; não há código criado nesta revisão.

## Pendências e riscos conhecidos (atuais)

- Q-01: validar UN inteira, identificação do cliente e preço sem descontos com responsável/orientador. Baseline explícita H-02/03/05 permite especificar os módulos.
- Q-02: definir hospedagem, domínio, certificados, orçamento e operação antes de publicar.
- Q-03: definir tratamento/retenção de dados pessoais antes de dados reais.
- Q-04: definir catálogo/pedido antes de iniciar 11.
- Prazos de sessão, limites e metas de desempenho/backup são decisões propostas; medir na implementação.
- Sessão API residual após logout com API indisponível expira no limite absoluto; frontend elimina acesso local imediatamente (ADR-02).

## Modelo para cada implementação futura

Copiar e preencher ao final de cada bloco, preservando registros anteriores:

```text
Bloco / estado:
Início e término (fuso):
Executor / commit ou identificador de versão:
Documentos lidos / ADRs aplicadas:
RF/RN/RNF entregues e CA demonstrados:
Arquivos criados/alterados:
Migrations e forma de aplicar:
Comandos realmente executados e resultados:
Evidências de API/UI/concorrência/segurança:
DoD global e específica (itens + justificativas):
Pendências, limitações e bloqueios:
Decisões novas e documentos atualizados:
Próximo bloco liberado ou motivo de bloqueio:
Primeira ação para retomar:
```

Nunca substituir “pendente” por “concluído” porque o módulo correspondente está documentado. Interrupção no meio do bloco deve registrar último passo realizado, mudanças locais e validações ainda não executadas.


## Implementação 00 — 06/11/2026, America/Cuiaba, 20h30–20h50

Estado: implementação entregue com validação parcial; NÃO APROVADO para avançar. Base Git 89c3832; alterações locais sem commit/push. Autorização explícita do usuário prevaleceu sobre a etapa documental anterior (ADR-19). Lidos AGENTS, índice, arquitetura 01–10, requisitos 01–06, módulo 00 integral e execução 00–04. Não havia implementação/contratos compilados reutilizáveis; HealthResponse foi criado a partir do contrato normativo.

Entregas: 10 projetos na solução; camadas e hosts fisicamente separados; Contracts somente HealthResponse. Domain/Application vazios intencionalmente, sem entidades/serviços fictícios. Contexts comercial/web com factories e migrations independentes, Up cria somente schema, históricos separados, sem Identity. Configuração validada com mensagens sem valores; negação por padrão sem esquema simulado; ProblemDetails seguro e cancelamento propagado; liveness e readiness mínimo, porta privada 8081; readiness verifica conexão e migrations. Blazor Interactive Server pt-BR com home/erro/indisponível e cliente HTTP tipado, estados reais e Repetir. Compose local, roles runtime/migrator distintas, volumes, TLS externo, imagens fixadas e CI preparados. RNF-04/07/08/09/10 e RN-15 incrementais; nenhum RF comercial.

### Comandos e resultados reais

| Verificação | Comando/roteiro executado | Observado |
| --- | --- | --- |
| Ambiente | dotnet --info; dotnet --version; docker version | SDK 10.0.401, runtime 10.0.12. --info inicialmente teve acesso negado ao Service Control Manager; --version e build funcionam. docker não encontrado; psql/pg_ctl não encontrados (somente diretório data de instalação antiga). |
| Restore | dotnet restore --disable-parallel; dotnet restore --locked-mode | Ambos concluídos; pacotes transitivos fixados por lockfiles. DOTNET_CLI_HOME local .local/dotnet para evitar escrita em diretório de templates externo; acesso de rede autorizado na execução. |
| Build | dotnet build --no-restore -c Release | Última execução: sucesso, 0 avisos/0 erros. Primeiro build revelou conflito Relational 10.0.4/10.0.12; corrigido com referência explícita 10.0.12 e novo restore. |
| EF CLI | dotnet new tool-manifest; dotnet tool install dotnet-ef --version 10.0.12 | Ferramenta local registrada em dotnet-tools.json. |
| Migrations | dotnet ef migrations add BaselineComercial --project backend/src/GestaoComercial.Infrastructure --output-dir Persistence/Migrations --configuration Release; equivalente BaselineWeb em frontend/src/GestaoComercial.Web | Geradas, Up revisado com EnsureSchema; nenhuma tabela de negócio. Down não destrói schema. Nome temporal EF usa UTC. |
| SQL | dotnet ef migrations script --project backend/src/GestaoComercial.Infrastructure --configuration Release --no-build --idempotent --output deploy/baseline-comercial.sql; equivalente Web/baseline-web.sql | SQL gerado e inspecionado: schemas + __EFMigrationsHistory. Não aplicado a banco real. MIGRATION_CONNECTION temporária de design, sem credencial real. |
| Testes parciais | dotnet test -c Release --no-build --filter 'Category!=PostgreSQL' | API 5 aprovados. Após ajustes finais, dotnet test frontend/tests/GestaoComercial.Web.Tests -c Release --no-build --filter 'Category!=PostgreSQL': 12 aprovados. Nenhum ignorado. |
| Suíte completa final | dotnet test -c Release --no-build --logger 'trx;LogFilePrefix=entrega' --results-directory artifacts/tests | 19 testes: 17 aprovados, 2 falharam explicitamente por TEST_COMERCIAL_CONNECTION/TEST_WEB_CONNECTION ausentes, 0 skips; exit 1. Domain/Application.Tests sem casos porque não há lógica própria; não contabilizados como aprovação de regras. |
| Arquitetura | ./scripts/verify-architecture.ps1 | Referências válidas; frontend só Contracts; Domain/Application/Contracts sem pacote externo. |
| Vulnerabilidades | dotnet package list --vulnerable --include-transitive --format json | Relatório artifacts/vulnerabilities.json sem pacotes vulneráveis reportados. Não substitui scanner de segredos. |
| UI real | Web em Testing HTTP loopback temporário, Edge headless/CDP, viewport exata 360 e 1280, Tab e Repetir | scrollWidth igual à largura da viewport (360/1280); foco alcançou BUTTON; clique Repetir manteve indisponibilidade real da API; capturas revistas. Corrigido carregamento de static web assets encontrado na primeira inspeção. Host temporário encerrado. |

Evidências locais ignoradas pelo Git: artifacts/tests/entrega*.trx, artifacts/vulnerabilities.json, artifacts/screenshots/home-360.png e home-1280.png. UI testada somente no estado de API indisponível, sem alegar validação TLS/Compose. Configuração ausente testada no host Web (falha de startup) e no validador API. Erro injetado exclusivamente via StartupFilter de testes, sem endpoint de falha na aplicação. Testes verificam 401 em rota não liberada, 404 no readiness público, 503 no readiness sem DB e live 200.

### Aceites e DoD

- M00-CA01: parcial (restore/build/hosts/layout comprovados); falta baseline real + readiness 200 em Compose/TLS.
- M00-CA02: parcial (readiness 503 com conexão indisponível e live 200 via WebApplicationFactory); falta parar/reiniciar DB real.
- M00-CA03: pendente, Docker/DB indisponíveis; volume e reaplicação têm roteiro, sem comprovação.
- M00-CA04: comportamento comprovado por testes de configuração e startup sem conexão, sem valor secreto no erro.
- DoD arquitetura/escopo: atendida na inspeção. Sem dados, transações, concorrência comercial ou constraints comerciais neste bloco (não aplicáveis).
- DoD build: atendida. DoD testes integração/migrations do zero/TLS/roles/volumes: pendente. UI 360/1280 e foco básico demonstrados; não alegar auditoria completa de acessibilidade. CI criado, não executado no GitHub. Gitleaks não executado localmente (Docker ausente), etapa preparada em CI.

### Pendências e retomada

Providenciar Docker/Compose e certificados locais SAN api/localhost confiados; executar o roteiro 07 com banco isolado, conferir SQL e aplicar com migrator único. Executar os dois testes reais, validar permissões cruzadas/DDL, readiness 200/503 com parada real e persistência após reiniciar volume sem removê-lo. Validar build das imagens/Compose, certificados e pipeline com scanner. Nenhuma migration foi aplicada a banco disponível neste ambiente; nenhum container foi criado. Imagens declaradas não foram baixadas/testadas localmente. Não há bloqueio de negócio novo.

Próximo bloco: 01 Autenticação, ainda BLOQUEADO pelo gate 00. Não iniciado. Parar neste bloco. Primeira ação de retomada: conferir alterações e ambiente sem remover volumes existentes, então iniciar somente o banco conforme roteiro. Decisão nova ADR-19; requisitos não alterados.

### Inventário de arquivos criados/alterados nesta execução

- `.dockerignore`
- `.editorconfig`
- `.gitattributes`
- `.github/workflows/fundacao.yml`
- `.gitignore`
- `Directory.Build.props`
- `Directory.Packages.props`
- `GestaoComercial.slnx`
- `README.md`
- `backend/src/GestaoComercial.Api/Configuracao.cs`
- `backend/src/GestaoComercial.Api/ErroSeguro.cs`
- `backend/src/GestaoComercial.Api/GestaoComercial.Api.csproj`
- `backend/src/GestaoComercial.Api/Program.cs`
- `backend/src/GestaoComercial.Api/packages.lock.json`
- `backend/src/GestaoComercial.Application/GestaoComercial.Application.csproj`
- `backend/src/GestaoComercial.Application/packages.lock.json`
- `backend/src/GestaoComercial.Domain/GestaoComercial.Domain.csproj`
- `backend/src/GestaoComercial.Domain/packages.lock.json`
- `backend/src/GestaoComercial.Infrastructure/GestaoComercial.Infrastructure.csproj`
- `backend/src/GestaoComercial.Infrastructure/Persistence/ComercialDbContext.cs`
- `backend/src/GestaoComercial.Infrastructure/Persistence/ComercialDbContextFactory.cs`
- `backend/src/GestaoComercial.Infrastructure/Persistence/Migrations/20261107004116_BaselineComercial.Designer.cs`
- `backend/src/GestaoComercial.Infrastructure/Persistence/Migrations/20261107004116_BaselineComercial.cs`
- `backend/src/GestaoComercial.Infrastructure/Persistence/Migrations/ComercialDbContextModelSnapshot.cs`
- `backend/src/GestaoComercial.Infrastructure/packages.lock.json`
- `backend/tests/GestaoComercial.Api.IntegrationTests/FundacaoTests.cs`
- `backend/tests/GestaoComercial.Api.IntegrationTests/GestaoComercial.Api.IntegrationTests.csproj`
- `backend/tests/GestaoComercial.Api.IntegrationTests/packages.lock.json`
- `backend/tests/GestaoComercial.Application.Tests/GestaoComercial.Application.Tests.csproj`
- `backend/tests/GestaoComercial.Application.Tests/packages.lock.json`
- `backend/tests/GestaoComercial.Domain.Tests/GestaoComercial.Domain.Tests.csproj`
- `backend/tests/GestaoComercial.Domain.Tests/packages.lock.json`
- `deploy/.env.example`
- `deploy/Api.Dockerfile`
- `deploy/Web.Dockerfile`
- `deploy/baseline-comercial.sql`
- `deploy/baseline-web.sql`
- `deploy/compose.yaml`
- `deploy/init-db.sh`
- `docs/README.md`
- `docs/execution/03-ESTADO-PROJETO.md`
- `docs/execution/04-DECISOES-TECNICAS.md`
- `docs/execution/07-EXECUCAO-LOCAL-FUNDACAO.md`
- `dotnet-tools.json`
- `frontend/src/GestaoComercial.Web/ApiClients/EstadoApiClient.cs`
- `frontend/src/GestaoComercial.Web/Components/App.razor`
- `frontend/src/GestaoComercial.Web/Components/LayoutPrincipal.razor`
- `frontend/src/GestaoComercial.Web/Components/Pages/InicioTecnico.razor`
- `frontend/src/GestaoComercial.Web/Components/Pages/PaginaErro.razor`
- `frontend/src/GestaoComercial.Web/Components/Pages/PaginaIndisponivel.razor`
- `frontend/src/GestaoComercial.Web/Components/Routes.razor`
- `frontend/src/GestaoComercial.Web/Components/_Imports.razor`
- `frontend/src/GestaoComercial.Web/Configuracao.cs`
- `frontend/src/GestaoComercial.Web/ErroSeguro.cs`
- `frontend/src/GestaoComercial.Web/GestaoComercial.Web.csproj`
- `frontend/src/GestaoComercial.Web/Persistence/Migrations/20261107004124_BaselineWeb.Designer.cs`
- `frontend/src/GestaoComercial.Web/Persistence/Migrations/20261107004124_BaselineWeb.cs`
- `frontend/src/GestaoComercial.Web/Persistence/Migrations/WebDbContextModelSnapshot.cs`
- `frontend/src/GestaoComercial.Web/Persistence/WebDbContext.cs`
- `frontend/src/GestaoComercial.Web/Persistence/WebDbContextFactory.cs`
- `frontend/src/GestaoComercial.Web/Program.cs`
- `frontend/src/GestaoComercial.Web/packages.lock.json`
- `frontend/src/GestaoComercial.Web/wwwroot/app.css`
- `frontend/tests/GestaoComercial.Web.Tests/FundacaoWebTests.cs`
- `frontend/tests/GestaoComercial.Web.Tests/GestaoComercial.Web.Tests.csproj`
- `frontend/tests/GestaoComercial.Web.Tests/packages.lock.json`
- `global.json`
- `scripts/verify-architecture.ps1`
- `shared/GestaoComercial.Contracts/GestaoComercial.Contracts.csproj`
- `shared/GestaoComercial.Contracts/HealthResponse.cs`
- `shared/GestaoComercial.Contracts/packages.lock.json`

Revisão suplementar final: quebra natural de palavras corrigida nas tabelas (perfil Administrador legível sem divisão interna).16 páginas com tabelas×4 larguras=64 medições adicionais,0 overflow de página/JS (artifacts/block10/tables-final.json). Build table-build.log:0 avisos/erros. Correção visual sem alteração de regras; acompanhar o novo HEAD no CI do PR5.

Correção final da navegação real: no celular, cliques com navegação aprimorada reabriam o menu. O script inicial foi substituído pelo inicializador GestaoComercial.Web.lib.module.js, afterWebStarted/enhancedload, conforme [documentação oficial Blazor](https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/static-server-rendering?view=aspnetcore-10.0). Sem nova biblioteca. Abertura direta de URLs e navegação por clique são conferências distintas; 12 navegações por clique aprovadas nas quatro larguras, menu fechado no celular/tablet e aberto no desktop; teclado e zoom reconferidos. Evidências: artifacts/block10/navigation-final.json e zoom-keyboard.json. Build navigation-build.log:0 avisos/erros.

### Gate final do código — 07/11/2026,20h25 America/Cuiaba

Commit0d49db9 aprovado integralmente: [CI PR42429190111](https://github.com/argemon/MVPMilton/actions/runs/42429190111) e [push42429186208](https://github.com/argemon/MVPMilton/actions/runs/42429186208), todas as etapas success.126 unitários+116 integrações=242 aprovados,0 falhas/skips; build0 avisos/erros; auditoria e scanner aprovados. Log real: artifacts/block10/ci-code-final.log. Inclui a quebra natural das tabelas e o inicializador de navegação aprimorada. Roteiros locais finais:132 medições de33 páginas/fichas,64 medições adicionais de tabelas e12 navegações por clique,0 overflow de página/JS; zoom equivalente e teclado reconferidos. Cinco problemas de apresentação corrigidos; nenhum defeito crítico funcional identificado.

Hosts/navegador temporários encerrados após logout; dados e imagem privada persistente preservados. .env idêntico por SHA-256;241 arquivos candidatos sem credenciais conhecidas/Gitleaks,45 Markdown/142 links locais válidos e diff limpo. Acesso somente nos arquivos privados ignorados; não versionar/exibir. Iniciar para a banca conforme README. Os commits seguintes apenas registram evidências; conferir seu CI no PR antes de encerrar a entrega, sem repetir a carga ou abrir outro bloco.

## Correção de usabilidade e cenário de mercado — 09/11/2026

Solicitação: esclarecer Marcas, tornar a execução da venda visível e substituir dados genéricos por um supermercado. Complemento ADR-38 registrado antes da implementação. Nova venda agora tem atalho direto no menu e destaque no histórico; tela existente orienta montagem, salvamento e confirmação. Marcas/fabricantes e categorias ficam acessíveis em Produtos, mantendo contratos e permissões.

CenarioMercado/DadosDemonstracao representam 100 apresentações ilustrativas de Camil, Nestlé, Piracanjuba, Ypê e Colgate, cinco categorias, 52 combinações de nomes sintéticos. Geração atualiza nomes genéricos antigos comprovados pela auditoria da geração; novas etapas idempotentes reutilizam serviços de cadastro. Preços originais da seed passam a valores por produto e tamanho de embalagem; preços manuais diferentes são preservados. IDs, saldos e snapshots de vendas/movimentos permanecem. Sem restauração, exclusão, novo banco ou edição do .env.

Arquivos desta correção: CenarioMercado.cs, DadosDemonstracao.cs, LayoutPrincipal.razor, Produtos.razor, Vendas.razor, VendaDetalhe.razor, DadosDemonstracao.razor, DemonstrationTests.cs e registros03/04. Alterações anteriores de home/login/CSS e .vs já existiam no início e foram preservadas.

Execuções reais: dotnet build -c Release --no-restore -m:1 aprovado inicialmente, zero avisos/erros; dotnet test tests/GestaoComercial.UnitTests -c Release --no-restore -m:1 aprovado,127/127, zero skips. Primeira tentativa de build encontrou hosts com arquivos abertos; reiniciados localmente. Build posterior da solução encontrou testhost com arquivos abertos; execução final usa OutputPath isolado. Teste DemonstrationTests ampliado para conversão legada, repetição, saldo/preço e preservação integral das vendas; resultado final ainda pendente neste registro. Sem afirmar regressão integral/CI aprovado.

Atualização de nomes aplicada por POST administrativo autenticado no Atlas existente,200; busca Arroz encontrou10 apresentações e hash .env preservado. Complemento de preços em aplicação pela mesma operação. Navegador real: login e /vendas/nova, atalho presente,21 opções em cada seletor (20 registros+padrão),390px sem overflow e1366px conferidos. Capturas locais artifacts/block10/screens/market-sale-mobile.png e market-sale-desktop.png. Não repetir jornadas transacionais antigas como evidência nova. Pendência: finalizar teste de integração, atualização de preços e revisão Git/CI. Nenhum bloco seguinte, deploy ou merge.

Complemento final desta correção: preços revisados por embalagem (arroz1kg R$7,00;2kg R$14,00;4kg R$28,00), lidos da API após atualização200. Saldo dessas apresentações275/114/332 permaneceu. .env novamente idêntico por SHA-256. Build isolado `dotnet build -c Release --no-restore -m:1 -p:OutputPath=D:/Projetos/MVPMilton/.local/market-ready/` aprovado0 avisos/erros. `dotnet test tests/GestaoComercial.UnitTests -c Release --no-restore -m:1 -p:OutputPath=D:/Projetos/MVPMilton/.local/market-verified/ --verbosity minimal`:128 aprovados,0 falhas/skips, incluindo CenarioMercadoTests (100 preços positivos e crescentes por embalagem,100 nomes de produto e52 pessoas distintos). Teste intermediário MongoDB com expressão de preço anterior falhou1/3 por500 em vez de503; corrigida precedência do switch antes da versão56ad70d. Testes locais finais MongoDB ainda em andamento; não confundir binários intermediários com versão final.

[PR6](https://github.com/argemon/MVPMilton/pull/6), código final56ad70d publicado; CI PR42724517999/push42724511218 em andamento neste registro. Não incluídas alterações locais preexistentes de DocumentoConta, InicioTecnico, app.css, AccountDocumentTests ou .vs. Aplicação local API/Web reiniciada com as alterações; sem deploy/merge.

Gate do código56ad70d: [CI PR42724517999](https://github.com/argemon/MVPMilton/actions/runs/42724517999) concluído success;127 unitários+116 integrações MongoDB reais/Testcontainers=243 aprovados,0 falhas/skips;build0 avisos/erros, auditoria e scanner aprovados. Diferença local128 versus CI127 corresponde ao AccountDocumentTests local preexistente não incluído neste PR. Carga final por embalagem aplicada200 e .env preservado; preços e saldos conferidos por leitura. Testes Atlas locais adicionais ainda em andamento, sem atribuir-lhes o resultado do CI. Código funcional validado; commits posteriores somente registram evidências e têm seu próprio CI. Não há próximo bloco; sem deploy/merge.

## PDV solicitado e implementado — 09/11/2026

Substituído formulário vertical por frente de caixa nas rotas de venda; complemento ADR-38 registrado antes. Referências oficiais Bling/Syndata consultadas; produto pesquisável/paginado, Enter/SKU, inclusão repetida na mesma linha, cupom com quantidades/remoção/cliente, total e revisão→confirmação. Controles bloqueados até conectar, intenção/chave preservadas em resultado incerto e ações de cancelamento/descarte secundárias. API/contratos/persistência/regras mantidos. Sem pagamento/fiscal ou novo bloco.

Arquivos e comandos reais no [relatório PDV](13-AJUSTE-PDV.md). Builds Web/isolado0 avisos/erros;128 unitários locais e2 integrações Atlas aprovados,0 falhas/skips. Teste BFF ampliado para tela/permite acesso/rascunho vazio; regra de ciclo completo MongoDB reexecutada. Jornada no navegador: SKU repetido→2,quantidade+→3,review e confirmação R$10,50, saldo94→91; cancelamento com motivo/checkbox→94,−3/+3 preservados. Venda sintética31a386a7-94dd-4140-ad88-de1b6ccfc87e preservada. Busca vazia/recuperação,paginação,Enter,foco,remoção,cliente conferidos;1920/1366/768/390 sem overflow/JS,finalização visível em desktop. .env não editado; único banco Atlas e hosts locais reutilizados. Mudanças anteriores de home/login/app.css e .vs preservadas e fora deste commit. Sem deploy/merge; CI do código PDV ainda deve concluir antes de declarar gate integral. Não há próximo bloco.

Gate do PDV47ac7e6 aprovado: [CI PR42727683240](https://github.com/argemon/MVPMilton/actions/runs/42727683240) e [push42727675126](https://github.com/argemon/MVPMilton/actions/runs/42727675126),ambos success.127 unitários+116 integrações=243 aprovados,0 falhas/skips;build0 avisos/erros,auditoria e scanner aprovados. Log .local/pdv-ci-final.log. Validação local final repetida com OutputPath=.local/pdv-release:build0 avisos/erros,128 unitários+2 integrações aprovados;128 inclui AccountDocumentTests preexistente local fora do PR. Próximo atendimento com cupom vazio conferido;captura final artifacts/block10/screens/pdv-preview.png com20 produtos/2 itens/R$17,50 representa prévia não persistida. Hosts locais atualizados e permanecem disponíveis;nenhum deploy/merge. Gate acima é do código47ac7e6;commit seguinte somente registra evidências e recebe CI próprio. Nenhum bloco posterior.
