# M10 — Publicação e entrega

## 1. Identificação, objetivo e valor

**Bloco 10, P0 operacional.** Disponibilizar o MVP no destino escolhido com TLS, acesso seguro, recuperação demonstrada e documentação de operação para o responsável.

## 2. Pré-requisitos e dependências concluídas

Normas transversais: arquitetura05/09 e ADR-20–29, MongoDB.Driver oficial restrito à infraestrutura, transações sequenciais em topologia compatível e testes MongoDB reais isolados (Atlas ou perfil Testcontainers explícito) conforme ADR-28. Classes/serviços de módulo futuro não são pré-requisitos ocultos.

00–09 aprovados; evidências de qualidade e backup/restore de ensaio. Q-02 resolvida: host/domínio/TLS/custo/operador. Q-03 resolvida para produção com dados reais; demonstração sintética pode prosseguir sem dados reais. Credenciais/certificados fornecidos fora do Git. Não há dependência de catálogo/pedido.

## 3. Requisitos e prioridades

Entrega operacional de RF-01–20 já implementados, sem novo RF comercial. RNF-01/04/05/06/07/08/10 P0/P1 originais; RN-15/16 e preservação RN-07/09. Repetir CA-16/17 e smoke CA-18 na topologia final; desempenho completo já medido em 09, repetir se recursos/configuração mudarem materialmente.

## 4. Dentro e fora do escopo

Dentro: Compose de produção, proxy HTTPS/WebSocket, API/DB privados, volumes, secrets, versões fixas, evoluções de persistência explícitas, backup externo diário, logs/alertas, rollback e entrega. Fora: cluster/HA/autoscaling, contratação sem definição, microsserviços, catálogo, fiscal/financeiro e exposição pública da API interna.

## 5. Atores e permissões

Operador nomeado administra host/backup com mínimo privilégio; não é novo perfil de negócio. Admin inicial é pessoa designada e troca senha temporária. Perfis do sistema seguem matriz inalterada. Somente proxy publica porta; readiness/DB/API não são públicos. Segredos do operador de evolução são usados apenas na tarefa de evolução de persistência, não no runtime.

## 6. Entidades e alterações de banco

Sem coleção comercial nova. Aplicar evoluções de coleções/validadores/índices do commit aprovado por operador separado; hosts runtime não administram schema nem alteram/remove stock_movements/audit_events. Backup consistente inclui os bancos MongoDB comercial e Web, imagens e key ring. Nenhuma evolução automática no startup. Ajuste descoberto exige ensaio e retorno ao gate afetado.

## 7. Fluxo principal de publicação

1. Registrar destino/commit/tag/imagens/schema/operador e verificar certificados/volumes/espaço.
2. Validar configuração sem segredo padrão, rede interna e upstream TLS confiável; WebSocket funciona pelo proxy.
3. Em atualização, manutenção e drenagem de escritas/circuitos → backup completo consistente → evoluções de persistência por tarefa única.
4. Iniciar serviços, liveness/readiness, primeiro bootstrap somente se não existir Admin, remover segredo de bootstrap e trocar senha.
5. Antes de liberar uso real, executar smoke com dados sintéticos no ambiente de validação da mesma topologia; em destino novo ainda vazio podem ser identificados como demonstração. Não contaminar relatórios reais nem apagar histórico para limpar teste.
6. Demonstrar restauração do conjunto em instalação isolada, registrar RPO/RTO e rotina de backup diário externo.
7. Liberar tráfego e observar métricas/logs; entregar runbook, URL, versão e responsável.

## 8. Alternativas, regras e validações

RNF-06: backup diário criptografado, externo, retenção30 dias, RPO<=24 h/RTO<=4 h demonstrados. Conexões/certificados/key ring não entram na imagem/repositório/logs. Certificado inválido não é contornado desabilitando validação. Instalação sem Q-03 usa exclusivamente sintéticos e é identificada como demonstração.

Falha de evolução de persistência → manter manutenção, não iniciar app incompatível; recuperar por evolução de persistência corretiva ou restore validado. Rollback de binário só com schema compatível. Restore destrutivo em destino real exige procedimento autorizado pelo responsável, registro de ponto/impacto e validação prévia em destino isolado; não improvisar exclusão de volumes.

## 9. Endpoints REST e verificações

Nenhum endpoint novo. Requests/responses permanecem os dos módulos01–08.

| Método / rota existente | Request | Response / comprovação |
| --- | --- | --- |
| GET `/health/live` | Sem corpo | 200 mínimo no caminho permitido pelo proxy |
| GET `/health/ready` | Acesso interno | 200 pronto ou 503 indisponível; inacessível externamente |
| POST `/api/v1/auth/login`; GET me; POST logout | Via BFF, credenciais externas | 200/204, cookie seguro; 401 inválido; sem token no navegador |
| POST estoque entrada; POST venda/confirmacao/cancelamento | Dados sintéticos, chaves e versões | 201/200; saldo/razão íntegros, 409 nos conflitos esperados |
| GET dashboard e relatórios/exportações | Filtros e perfil permitido | 200/CSV seguro; perfil negado403 |

Erro TLS/conexão no proxy é falha operacional, não ProblemDetails da API; UI mostra indisponibilidade. Não criar endpoint público de backup/bootstrap/reset. Dados do health não revelam versão/conexão/segredo.

## 10. Páginas, componentes e estados

Reusar `/conta/entrar`, `/`, páginas comerciais, `/erro` e `/indisponivel`. Proxy mostra página estática de manutenção somente durante deploy; não exige componente comercial novo. Vazio: instalação sem dados reais, listas vazias. Carregando: inicialização/reconexão com mensagem. Sucesso: jornada e URL verificadas. Erro: indisponibilidade segura, sem stack trace, e orientação de contato do responsável definido em Q-02. Comprovar responsividade/teclado e cookie após domínio final.

## 11. Erros e concorrência

Drenar escritas antes do backup/evolução de persistência, apenas um executor de evoluções de persistência e uma réplica conforme arquitetura. Reinício deve preservar volumes/chaves, sem misturar sessões. API indisponível bloqueia comandos; logout limpa ticket local. Backup/restore não pode gerar saldo divergente ou apontar metadado para arquivo perdido. Uma falha de readiness bloqueia liberação de tráfego.

## 12. Testes unitários e integração

Sem nova regra de domínio para teste unitário. Executar suíte do commit e regressão de configuração alterada. Integração/smoke na topologia final: TLS/proxy/WebSocket, isolamento de rede, menor privilégio DB, reinício/volumes/sessões/imagens, backup externo e restore isolado. Confirmar que role runtime não administra coleções/validadores/índices nem altera movimento/auditoria. Não afirmar RTO sem medição real.

## 13. Given/When/Then

| ID / rastreio | Given | When | Then |
| --- | --- | --- | --- |
| M10-CA01 / CA-16 | Backup completo e versões conhecidas | Restaurar em instalação isolada | Login/jornada/imagens funcionam e RPO/RTO medidos nas metas |
| M10-CA02 / RNF-01/04 | URL pública e rede configurada | Acessar site e tentar DB/API por fora | HTTPS funciona, DB/API privados, sem token no navegador |
| M10-CA03 / RNF-08 | Atualização testada e tráfego drenado | Aplicar evoluções de persistência explicitamente | Uma evolução por banco lógico, histórico preservado, runtime sem administração do schema |
| M10-CA04 / CA-17 | Sessões e imagem existentes | Reiniciar Web/API com volumes persistidos | Sem mistura de usuário, imagem presente, login/expiração respeitados |
| M10-CA05 / RNF-06 | Rotina de backup agendada pelo operador | Forçar falha controlada em ensaio | Alerta ao responsável, falha registrada e backup anterior preservado |

## 14. Checklist de execução

- [ ] Resolver Q-02 e Q-03 conforme destino; registrar responsáveis e configuração sem valores secretos.
- [ ] Executar `dotnet restore`, `dotnet build --no-restore -c Release`, `dotnet test -c Release --no-build` do commit em ambiente de validação.
- [ ] Conferir imagens fixas, Compose final, certificados e privilégios; guardar backup antes de atualização.
- [ ] Aplicar evoluções de persistência backend/web por tarefa única e registrar comandos/versões exatos.
- [ ] Executar smoke/restore/isolamento; medir RTO/RPO e registrar M10-CA01–05.
- [ ] Entregar runbook Markdown em `docs/execution/evidencias/10-ENTREGA.md` durante implementação com URL, versão, backup/restore/rollback, observação e suporte.
- [ ] Atualizar estado como MVP publicado somente após todos os gates; não iniciar11 automaticamente.

## 15. DoD e desbloqueio

DoD global, URL segura/jornada/backup/restore/operador demonstrados. Entrega do MVP termina aqui. **11 permanece condicionado à definição/aceitação de seu escopo posterior**; não há novo software futuro necessário para aprovar10.
