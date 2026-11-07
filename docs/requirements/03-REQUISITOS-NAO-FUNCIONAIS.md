# 03 — Requisitos não funcionais

| ID | Prioridade | Requisito e medição | Gate / evidência |
| --- | --- | --- | --- |
| RNF-01 | P0 | API nega requisição anônima, sem policy ou com sessão revogada; token API ausente de navegador/HTML/storage | 01 e 09; CA-01/02/13/17, testes BFF e HTTP |
| RNF-02 | P0 | Saldo nunca negativo, confirmação/cancelamento indivisíveis, um efeito por intenção | 05/06; CA-07/09/10/11/12 em MongoDB |
| RNF-03 | P1 | UI pt-BR responsiva em 360 e 1280 px, teclado operável, inputs com labels e erro associado | Cada tela e 09; CA-18 com roteiro |
| RNF-04 | P0 | Nenhum segredo versionado/logado; dados pessoais mínimos; TLS e arquivos privados | 00/01/04/10; revisão e CA-17 |
| RNF-05 | P1 | Listas p95 <= 2 s e confirmação p95 <= 3 s com 20 usuários, base/medição da arquitetura 09 | 09; relatório de medição com ambiente e consultas |
| RNF-06 | P0 | Backup diário externo criptografado, RPO <= 24 h, restore medido <= 4 h, retenção operacional 30 dias | 10; CA-16, cópia e restauração sintéticas |
| RNF-07 | P0 | Build Release e testes xUnit passam; regras críticas possuem testes de integração reais em MongoDB isolado com suporte transacional (Atlas ou perfil Testcontainers) | Cada bloco; logs reais da execução |
| RNF-08 | P1 | Evoluções de persistência criam do zero e atualizam base anterior sem perder histórico | Cada alteração DB e 09; teste upgrade/validadores e filtros condicionais |
| RNF-09 | P1 | Frontend depende só de contratos; Domain sem BSON/driver/HTTP; Application sem Infrastructure | 00 e revisão por bloco; inspeção referências |
| RNF-10 | P1 | Logs rastreiam traceId, usuário/recurso permitido e erro, sem credenciais; health sem detalhes | 00/01/09; CA-17 e simulação de falha |
| RNF-11 | P1 | Operação sensível audita ator/data/motivo junto ao efeito, sem permitir editar/apagar histórico | 02–06; CA-03/06/12 e validadores e filtros condicionais |
| RNF-12 | P1 | Paginação no servidor, máximo 100 por página, relatórios com período <= 366 dias e exportação <= 10 mil linhas | 03–08; CA-15 e limites HTTP |

Metas de infraestrutura são hipóteses mensuráveis, não garantias já demonstradas. Responsável pela implementação registra falhas e ações; não reclassifica silenciosamente critério para aprovar entrega. Não exigir métricas de disponibilidade 24×7 não contratadas para o TCC.

## Ambiente de medição

Dados sintéticos, sem modo debug, container aquecido, versões e recursos documentados. Medir latência no limite HTTP e concorrência real, não tempo unitário de método. Relatar separadamente conflito esperado por saldo e erro inesperado.

Revisão ADR-20–25: RNF-02 requer updates condicionais e transação snapshot/majority/primary em replica set para saldo/movimento/venda/auditoria/recibo; sem concorrência de comandos na mesma sessão. RNF-04 exige RBAC por coleção, credenciais separadas e sem leitura cruzada; RNF-08 usa evoluções documentais explícitas, não alteração automática no startup. RNF-10 inclui conflitos/retries/commit incerto com traceId sem URI/BSON sensível. Metas existentes não foram relaxadas; testes antigos não demonstram MongoDB.
