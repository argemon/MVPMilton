# 04 — Decisões técnicas, hipóteses e questões

## Natureza deste registro

Decisões abaixo são baseline de projeto propostas para implementação, salvo stack/restrições explicitamente obrigatórias pelo usuário. Não se atribuem ao TCC antigo. Ao alterar, registrar motivo, data, substituição e impactos nos documentos; preservar histórico da decisão anterior.

| ADR | Decisão e justificativa | Consequência / alternativa avaliada |
| --- | --- | --- |
| ADR-01 | Monólito modular Clean Architecture com API e Web separados por HTTP, monorepo | Atomicidade local e deploy simples; microsserviços/CQRS/MediatR/mensageria/Event Sourcing sem justificativa e fora de escopo |
| ADR-02 | Blazor Interactive Server como BFF, cookie de referência e sessão API opaca; Identity para contas | Sem JWT próprio/OAuth externo; token protegido em servidor, API consulta sessão; logout offline deixa token inacessível até expirar, conforme segurança |
| ADR-03 | PostgreSQL READ COMMITTED com locks explícitos ordenados para vendas/estoque | Impede overselling e preço/status obsoleto; versão otimista sozinha não protege todo o conjunto; transação única com auditoria |
| ADR-04 | Idempotency-Key por ator/escopo/chave, resultado e hash na mesma transação | Resposta perdida é recuperável; UNIQUE por item/tipo e estado reforçam garantia; nenhuma purga automática no MVP |
| ADR-05 | Portas específicas e EF em Infrastructure; contratos de transporte sem domínio | Evita Generic Repository e dependência do frontend no backend; mapeamento manual inicial |
| ADR-06 | .NET 10 LTS, EF Core 10 e Npgsql compatível; PostgreSQL 18 baseline proposta | Stack .NET obrigatória; fixar patches verificados no 00, não inventar compatibilidade de pacote |
| ADR-07 | UUID, decimal/numeric(18,2), estoque inteiro UN, instante UTC e fuso configurável | Limites protegem overflow; fracionamento demanda mudança transversal, não ajuste isolado |
| ADR-08 | Venda Rascunho/Confirmada/Cancelada/Descartada; preço servidor e snapshots | Sem reserva; preço alterado exige PUT explícito antes de confirmar; histórico não depende do cadastro atual |
| ADR-09 | Quatro perfis fixos, um por usuário; vendedor só próprias vendas | Sem editor dinâmico; API nega por padrão; último Admin protegido por lock singleton |
| ADR-10 | Cadastros inativáveis, movimentos imutáveis e estorno compensatório | Sem DELETE comercial; itens de rascunho e imagens substituíveis são exceções delimitadas |
| ADR-11 | Relatório é posição atual das vendas confirmadas no intervalo | Cancelamento posterior retira contribuição no período original; não é fechamento contábil nem fluxo de caixa |
| ADR-12 | Imagens JPEG/PNG privadas em volume; relatórios em CSV | Menor dependência e entrega rápida; sem PDF/SVG; validar conteúdo/limites e prevenir fórmula |
| ADR-13 | Uma réplica, Docker Compose, deploy em manutenção e migrations explícitas | Sem HA ou migration no startup; backup diário externo e restauração validada antes de produção |
| ADR-14 | Incrementos declarados para conta humana e histórico de cliente | 01 usa bootstrap; 02 conclui vínculo; 03 histórico vazio tipado; 06 o substitui; evita dependência circular |

## Esclarecimentos da revisão dos módulos — 06/11/2026

As ADR-01–14 permanecem preservadas. Os itens seguintes são **decisões propostas**, acrescentadas para fechar ambiguidades de execução; não representam aprovação de negócio nem autorização para produzir código nesta etapa.

| ADR | Ambiguidade e decisão proposta | Impacto e validação |
| --- | --- | --- |
| ADR-15 | Produto inativo é rejeitado já em POST/PUT do rascunho e novamente ao confirmar. Todos os itens devem estar ativos. Rascunho não reserva nem exige saldo suficiente; confirmação exige linha de saldo e quantidade disponível. Linha ausente dá409 SALDO_INEXISTENTE sem autocorreção. Saldo final int é limitado a2.147.483.647; overflow de entrada/estorno dá409 SALDO_EXCEDE_LIMITE e rollback total. | Esclarece RN-01/02/08/11 e RF-08–16; arquitetura04/05/06 e módulos04/05/06. Preço com terceira casa continua rejeitado; multiplicação por inteiro é exata; média arredonda somente no final por AwayFromZero, sem alterar regra aprovada. |
| ADR-16 | Banco é incremental:00 só schemas/trilha;01 Identity/sessão sem FK de funcionário;02 acrescenta FK/UNIQUE de vínculo;04 produto+saldo 0;05 movimentos apenas manuais sem FK futura;06 acrescenta referências/constraints de venda. Comandos de conta/vínculo/inativação adquirem controle_administracao→usuários ordenados→funcionários ordenados→sessões e revalidam após locks. | Remove dependências circulares de migrations e criação de conta versus inativação; preserva ADR-03/14. Testes locais de cada bloco usam somente tabelas existentes, exceto fixtures técnicas explicitamente isoladas. |
| ADR-17 | Fixar nomes mínimos de DTOs/campos/rotas de UI para executores; usuário tem nome de exibição (bootstrap externo não secreto; conta humana copia nome do funcionário ao criar), sem sincronização automática posterior. Listas de usuário incluem email/ativo/versão somente para Admin. Health usa JSON mínimo próprio; demais erros usam ProblemDetails. Datas opcionais são ambas presentes ou ambas ausentes; exportação não usa paginação. Estoque baixo ordena saldo/nome/id; relatório de movimentos soma positivos, absolutos negativos e delta líquido. | Detalhamento de transporte/apresentação, sem substituir BFF, camadas ou políticas existentes. Códigos complementares: EMAIL_DUPLICADO, VINCULO_OCUPADO, REFERENCIA_EM_USO e os conflitos da ADR-15. Módulos00–10 descrevem request/response/erro/estados; arquitetura06 consolida. |
| ADR-18 | Recorte P2 proposto para11: produto ativo+publicado explicitamente; catálogo público sem saldo; solicitação anônima nome/telefone/itens, preço estimado em snapshot, Recebido→Encerrado com motivo. Sem identidade de consumidor, reserva, baixa, pagamento, entrega ou conversão automática em venda. Admin/Gerente publicam/triam. Dedupe público por chave aleatória e hash separado de sessão interna; recibo sem PII, sem GET público de pedido. Limites iniciais10 envios/10 min e 60 GET/min por IP confiável; corpo64 KiB; no-store para catálogo/imagem. | RF-21–24/RN-21–24 são propostos P2, não gates MVP. Implementar11 só após aceite/ajuste registrado de Q-04 e Q-03. Modelo/rotas/testes completos em módulo11; nenhum serviço financeiro/fiscal ou módulo12 é necessário. |

Q-04 continua aberta: a proposta mínima acima oferece uma resposta concreta para aceite/ajuste, não simula decisão do responsável. Rotas e DTOs P2 não devem ser criados antecipadamente.

Detalhes complementares das propostas: ADR-15 aplica locks de cliente/produtos também ao gravar rascunho, sem reserva/lock de saldo. ADR-17 permite ator ausente somente em auditoria explicitamente anônima (falha de login e futura solicitação pública), sem usuário fictício; mutações administrativas/comerciais continuam exigindo ator. Respostas Web de credencial inválida usam401 com formulário e mensagem genérica. Esses detalhes estão propagados ao banco e módulos01/06/11.

## Hipóteses a validar (baseline preservada)

| ID | Hipótese | Impacto se mudar |
| --- | --- | --- |
| H-01 | Uma empresa/um estoque por produto na instalação | Multiempresa/depósito exige remodelagem e autorização |
| H-02 | UN inteira | Fracionado muda tipos, limites, UI, movimentos e testes |
| H-03 | Cliente ativo obrigatório | Venda anônima exige regra de histórico e privacidade |
| H-04 | BRL, pt-BR e America/Cuiaba inicial | Outra moeda exige modelagem monetária; fuso já é configuração |
| H-05 | Sem descontos/fiscal/pagamento no MVP | Total e confirmação terão novas regras/contratos |
| H-06 | Uma réplica, desempenho e backup conforme RNF | Recurso operacional precisa ser medido e financiado |
| H-07 | Perfis fixos e vendedor restrito às próprias vendas | Mudança exige matriz, consultas e testes negativos |

## Questões abertas

| ID | Questão / responsável esperado | Prazo e condição |
| --- | --- | --- |
| Q-01 | Responsável de negócio/orientador confirma UN, cliente obrigatório e dados mínimos/preço? | Antes de homologar 03/04/06; enquanto isso seguir baseline e registrar hipótese |
| Q-02 | Responsável do projeto define host/domínio/certificado/custo e operador de backup? | Bloqueia 10; não bloqueia desenvolvimento local |
| Q-03 | Responsável pelos dados define finalidade, retenção e atendimento de solicitações sobre dados pessoais? | Bloqueia produção com dados reais; demonstração sintética permitida |
| Q-04 | Responsável de negócio define catálogo, pedido, identidade, reserva, entrega e pagamento? | Bloqueia 11; sem contratos públicos aprovados |

## Registro de mudança futura

Formato: `data | ADR/Q | estado anterior | decisão e justificativa | responsável | RF/RN/CA e documentos impactados | evidência`. Questão aberta só muda para resolvida com resposta registrada; hipótese não vira fato por repetição em outro documento.
