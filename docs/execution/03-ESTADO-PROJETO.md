# 03 — Estado do projeto

## Identificação

- Projeto: GestaoComercial.
- Data desta especificação: 06/11/2026, referência America/Cuiaba.
- Etapa: documentação inicial.
- Ambiente observado: pasta de trabalho vazia, sem repositório Git no início.
- Software implementado: nenhum. Não há solução, banco, migrations, testes executáveis nem containers criados.

## Quadro de blocos

| Bloco | Estado | Evidência atual | Próxima ação |
| --- | --- | --- | --- |
| Documentação | Revisada | 37 arquivos Markdown; módulos 00–11 detalhados e matriz consolidada | Validar decisões propostas/hipóteses; software continua pendente |
| 00 Fundação | Pendente; próximo bloco de implementação | Somente especificação | Iniciar quando a implementação for solicitada |
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

Arquitetura01–10, requisitos 01–06 e módulos 00–11 foram lidos integralmente. Os12 módulos foram completados com15 seções cada, incluindo objetivo/valor, pré-requisitos, rastreabilidade/prioridades, escopo, atores, banco, fluxos, regras/validação, contratos, UI/estados, concorrência, testes, Given/When/Then, checklist e DoD/desbloqueio. Total previsto de aceites locais:62 cenários, sem substituir CA-01–18.

ADR-01–14 preservadas. ADR-15–18 são propostas identificadas: inativo/saldo/limites/arredondamento; migrations e locks administrativos incrementais; detalhes de contratos/UI; recorte mínimo P2 de catálogo/solicitação. Documentos normativos afetados foram sincronizados e a matriz foi acrescentada como37º Markdown. RF-01–20/RN-01–20 continuam MVP; RF-21–24/RN-21–24 são propostas P2. Q-04 continua aberta.

Validação documental final por leitura/expressões regulares no PowerShell:37 Markdown,12 módulos,15 seções por módulo,62 cenários locais,24 RF (20  MVP+4  P2 propostos),12 RNF,24 RN (20  MVP+4  P2 propostas),18 CA globais e 18 ADR. Zero links locais quebrados, cercas de código sem par, títulos vazios, IDs inexistentes ou arquivos de outra extensão. Todos os módulos contêm requests/responses, testes previstos, checklist, comandos de build/teste e DoD.

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
