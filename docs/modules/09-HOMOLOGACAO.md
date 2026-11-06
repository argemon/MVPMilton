# M09 — Homologação e qualidade

## 1. Identificação, objetivo e valor

**Bloco 09, P0 como gate de qualidade.** Validar o MVP integrado e produzir evidências reproduzíveis antes de publicar, identificando perda de integridade, acesso indevido e requisitos incompletos.

## 2. Pré-requisitos e dependências concluídas

00–08 aprovados, todos os RF-01–20 concluídos nos incrementos previstos e ambiente de homologação local equivalente à topologia de produção. Não exigir host/domínio final do 10; usar TLS e volumes de ensaio. Ler todos RN/RNF/CA e arquitetura 09/10.

## 3. Requisitos e prioridades

Validar RF-01–20 nas prioridades originais, RNF-01–12 e RN-01–20. CA-01–18 no ambiente de ensaio, com CA-16 de restore local; repetir CA-16 no destino final em 10. RF do catálogo não integra gate do MVP.

## 4. Escopo e exclusões

Dentro: regressão, matriz de acesso, jornada, contratos, carga, migrações, falhas, conciliação e relatório Markdown de qualidade/defeitos. Fora: novo requisito comercial, publicação definitiva, dados reais, catálogo e contratação de infraestrutura.

## 5. Atores e permissões

Homologador executa roteiros sintéticos com cada perfil; acesso à infra/testes é técnico, não nova role de negócio. Repetir Admin/Gerente/Vendedor/Estoquista/anônimo pela API. Não usar conta Admin para substituir testes de perfil negado ou propriedade.

## 6. Entidades e alterações de banco

Nenhuma nova entidade/tabela planejada. Fixtures incluem ativos/inativos, todos os perfis, saldos0/limite, vendas em todos estados e bordas de período. Banco de teste isolado. Correção descoberta deve ter migration específica e upgrade testado se necessária; nunca editar migration já aplicada para esconder defeito. Reconciliação somente leitura: saldo=soma(deltas), uma baixa/estorno por item conforme estado e nenhuma venda parcialmente confirmada.

## 7. Fluxos principais e alternativos

1. Fixar commit/versões/recursos → instalar do zero → executar build/suítes → executar cada CA e registrar esperado/observado.
2. Restaurar conjunto sintético banco/imagens/key ring em instalação isolada e testar jornada; medir RPO/RTO no ensaio.
3. Testar upgrade da versão anterior com dados → verificar histórico e constraints.
4. Medir carga: 20 usuários concorrentes, 10 mil produtos/50 mil vendas sintéticas, aquecimento e janela5 min; registrar p95/erros/CPU/RAM.
5. Testar DB/API indisponíveis, reinício Web, ticket antigo, resposta perdida e concorrência de confirmação/cancelamento.
6. Defeito encontrado → classificar, corrigir no escopo, acrescentar teste de regressão e repetir o cenário afetado; repetir suíte quando a mudança for transversal.

## 8. Regras e validações

Todas as RN/RNF. Nenhum ajuste de massa pode escrever saldo isolado; fixture cria saldo/movimentos coerentes, salvo teste explicitamente de corrupção. Não usar InMemory/SQLite para provar PostgreSQL. P0=segurança/integridade; P1=requisito/meta obrigatória ausente; P2=cosmético sem perda funcional. Zero P0/P1 para avançar; metas não são reduzidas silenciosamente para aprovar.

## 9. Contratos REST sob validação

Nenhum endpoint novo. Usar requests/responses normativos dos módulos01–08 e arquitetura06.

| Família / métodos existentes | Request representativo | Resultado esperado / falhas |
| --- | --- | --- |
| GET health; POST auth e GET me | Configuração/corpo de login válido/inválido | 200/204; 400/401/429/503 conforme cenário |
| GET/POST/PUT cadastros e imagem POST/GET/DELETE | DTO/versão/payload válido e malicioso | 200/201/204; 400/401/403/404/409/413/415 |
| GET estoque; POST entrada/saída/ajuste | Quantidade/delta/motivo/chave | 200/201; 409 sem efeitos parciais |
| GET/POST/PUT venda; POST confirmação/cancelamento/descarte | Versão/chave e motivo quando exigido | Estado/transação corretos; 403/404/409 e retry503 |
| GET dashboard/relatórios/exportações | Filtros e perfis distintos | Totais corretos; 400 limite,403 policy, CSV seguro |

Não adicionar endpoint de bypass/teste/reset em produção. OpenAPI comparado com contratos e HTTP real.

## 10. Páginas, componentes e estados

Nenhuma página nova obrigatória. Percorrer `/conta/entrar`, `/conta/senha`, `/usuarios`, `/funcionarios`, `/clientes`, `/produtos`, `/categorias`, `/marcas`, `/estoque`, `/vendas`, `/dashboard` e `/relatorios/*`, incluindo formulários/detalhes. Registrar para cada fluxo vazio, carregando, sucesso e erro; validar `/erro` e `/indisponivel`. Componentes existentes devem preservar filtros/formulários, foco e mensagem pt-BR em 360/1280 px. Relatório da homologação é arquivo Markdown, sem painel novo.

## 11. Erros e concorrência

Testar disputas com conexões/transações independentes e barreiras, não sleep como única sincronização. Repetir falha antes/depois do commit, timeout e revogação. A operação em andamento antes da revogação pode concluir conforme matriz; próxima requisição após commit da revogação não pode escrever. Não desligar paralelismo para esconder falhas.

## 12. Testes unitários e integração

Executar todas as suítes xUnit já existentes. Acrescentar teste somente para lacuna/defeito real. Integração PostgreSQL comprova constraints/locks/rollback/idempotência/upgrade/restore; testes BFF comprovam cookie/CSRF/isolamento. Journey de navegador pode ser roteiro manual verificável; segurança/integridade precisam automação. Relatório de carga não é teste unitário de duração de método.

## 13. Given/When/Then

| ID / rastreio | Given | When | Then |
| --- | --- | --- | --- |
| M09-CA01 / CA-01–18 | Commit fixo e fixtures coerentes | Executar roteiro completo | Cada CA tem resultado/evidência, zero P0/P1 |
| M09-CA02 / CA-16 | Backup sintético completo | Restaurar em ambiente isolado | Login/jornada/imagens funcionam, razão concilia, RPO<=24 h/RTO<=4 h medidos |
| M09-CA03 / CA-18 | Base/carga especificadas | Medir após aquecimento por 5 min | p95 listas<=2 s, confirmação<=3 s; recursos/erros registrados |
| M09-CA04 / RNF-11 | Vendas e cancelamentos do ensaio | Reconciliar saldos e movimentos | Zero divergência e nenhum histórico apagado |
| M09-CA05 / RNF-03 | UI em 360/1280 px | Usar teclado em vazio/carregando/sucesso/erro | Ações acessíveis, labels/foco/erros associados |

## 14. Checklist de execução

- [ ] Fixar commit/imagens/SDK/recursos/massa e identificar bancos descartáveis antes de preparar fixtures.
- [ ] Executar `dotnet restore`, `dotnet build --no-restore -c Release`, `dotnet test -c Release --no-build` com PostgreSQL real.
- [ ] Aplicar todas as migrations do zero e testar upgrade anterior; restaurar backup de ensaio sem tocar produção.
- [ ] Executar CA-01–18, carga, falhas, matriz e reconciliação; registrar comandos/observações.
- [ ] Produzir relatório em `docs/execution/evidencias/09-HOMOLOGACAO.md` durante implementação, sem dados reais/segredos.
- [ ] Corrigir P0/P1 com teste de regressão; registrar P2 e decisão sobre impacto.
- [ ] Atualizar estado e runbook sem declarar publicação concluída.

## 15. DoD e desbloqueio

DoD global, todos RF do MVP e CA de ensaio demonstrados; zero P0/P1, backup restaurável e desempenho medido. Desbloquear **10**, condicionado também a Q-02 e Q-03 quando houver dados reais. A validação local não depende de infraestrutura ainda não publicada; topologia final é responsabilidade do 10.
