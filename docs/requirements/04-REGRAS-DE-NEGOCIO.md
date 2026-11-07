# 04 — Regras de negócio

| ID | Regra normativa | Exemplo / implementação verificável |
| --- | --- | --- |
| RN-01 | Dinheiro interno em centavos Int64 BRL; preço 1–999.999.999 centavos e total <=99.999.999.999.999 centavos; HTTP/formatação em reais com até duas casas | 3 × 12,50 = 37,50; terceira casa de entrada é rejeitada, não truncada |
| RN-02 | Quantidade em UN inteira; saldo >= 0, sem reserva em rascunho | Saldo 1, saída 2 → 409, nenhum movimento |
| RN-03 | Confirmar grava estado, snapshots, saldo, movimentos, auditoria e idempotência na mesma transação | Um item insuficiente mantém toda a venda Rascunho |
| RN-04 | Disputas usam updates condicionais, guardas efetivas e revalidação de saldo/preço/status após conflito transacional | Saldo 1, duas vendas de 1 → uma confirma |
| RN-05 | Repetir intenção não duplica efeitos; estado e índices também protegem nova chave | Duplo clique/retry → uma baixa; cancelar duas vezes → um estorno |
| RN-06 | Cancelamento só por Administrador de Confirmada, motivo 10–500 caracteres, estorno integral por item | Inativar produto não impede devolver estoque; sem devolução parcial |
| RN-07 | Histórico confirmado, cancelado e movimentos é imutável; snapshots preservam dados comerciais | Renomear produto não altera nome/preço histórico |
| RN-08 | Cadastros com referências não são apagados; usar ativo. Inativo não é selecionável/confirmável | Produto inativo continua em relatório e histórico; categoria com produto ativo não inativa |
| RN-09 | Toda alteração de saldo gera movimento assinado; saldo = soma de deltas; saldo inicial zero | Entrada 10, venda -2, estorno +2 → 10 |
| RN-10 | Estados permitidos: Rascunho → Confirmada → Cancelada ou Rascunho → Descartada | Confirmada não aceita PUT de itens; Descartada não confirma |
| RN-11 | Cliente ativo e pelo menos um item para confirmar, com todos os produtos ativos; POST/PUT de rascunho também rejeitam produto inativo. Preço vem do cadastro e mudança exige revisão explícita | Preço 10 vira12 →409 PRECO_ALTERADO até PUT atualizar rascunho; inativo não entra em nova venda |
| RN-12 | SKU, documento informado e nome de marca/categoria são únicos após normalização, inclusive inativos | SKU ` abc ` e `ABC` colidem; CPF só dígitos, válido se informado |
| RN-13 | Indicadores usam fórmulas RF-17/19; não contam rascunho nem receita cancelada | Duas vendas 25/15, segunda cancelada → receita 25 |
| RN-14 | Entrada/saída manual exige quantidade positiva; ajuste delta não zero; motivo obrigatório 10–500 | Ajuste -3 é rejeitado com saldo 2; ajuste não reescreve histórico |
| RN-15 | Conta não tem cadastro público, perfil único e autorização atual validada na API | UI ocultar botão não concede permissão nem basta como proteção |
| RN-16 | Inativação de usuário/funcionário, troca de perfil/senha revogam sessões; preservar último Admin | Duas inativações concorrentes nunca deixam zero administradores |
| RN-17 | Vendedor só consulta/edita/confirma/descarta vendas próprias; administrador acessam todas | Vendedor B obtém 404 ao consultar venda de A |
| RN-18 | Conta humana vinculada a funcionário ativo, uma por funcionário; bootstrap Admin é exceção única | Cadastrar funcionário não cria credencial automaticamente |
| RN-19 | Imagens/CSV seguem validação e tratamento seguro; sem arquivos executáveis ou fórmula injetada | SVG rejeitado; texto `=1+1` exportado neutralizado |
| RN-20 | Intervalo `[inicio,fim)` usa fuso da instalação; relatórios usam confirmação, listas de venda usam criação | Venda às 00 h locais entra no início, instante igual ao fim fica fora |

## Regras P2 propostas — aplicação somente no 11 após liberação

| ID | Regra proposta | Verificação |
| --- | --- | --- |
| RN-21 | Catálogo só expõe produto ativo+publicado e DTO público sem saldo/dados pessoais | Inativo ou não publicado resulta404 público, inclusive imagem |
| RN-22 | Pedido online é solicitação Recebido→Encerrado, sem reserva, baixa ou venda automática | Solicitação com saldo 0 não gera movimento nem garantia de disponibilidade |
| RN-23 | Preço estimado e snapshots vêm do servidor; até 100 itens distintos, quantidades/centavos/total nos limites do MVP | Preço/total enviados pelo visitante são rejeitados; preço estimado é congelado no pedido |
| RN-24 | Dedupe público com recibo sem PII, rate limit e triagem autorizada/versionada | Repetição não duplica; corpo divergente409; encerramento concorrente preserva um motivo original |

Estas regras são propostas da ADR-18; aceites locais completos em módulo11. RN-01–20 continuam a baseline do MVP.

## Normalização e limites (MVP)

Nome: trim, espaços consecutivos reduzidos; chave normalizada de categoria/marca em maiúsculas invariant, mantendo acentos (não prometer busca fonética). SKU: trim/maiúsculas, caracteres A–Z, 0–9, ponto, hífen e sublinhado, 1–40. Nome de pessoa/produto 2–150, categoria/marca 2–80. Documento: CPF 11 ou CNPJ 14 dígitos conforme tipo, validar dígitos verificadores e rejeitar sequências inválidas. Documento ausente fica null, não string vazia.

Email opcional nos cadastros tem formato válido quando informado; email de login é obrigatório, normalizado pelo Identity e distinto do contato do funcionário. Telefone opcional aceita 10–15 dígitos após normalização. Descrição de produto no máximo 2.000 caracteres, sem HTML. Limites completos de campos e concorrência no [banco](../architecture/05-BANCO-DE-DADOS.md).

O MVP não valida situação cadastral por serviço externo. Mudança de tipo PF/PJ exige validar o documento informado no mesmo comando. Não tornar documento obrigatório sem resolver Q-01 e revisar os testes.

Esclarecimento proposto ADR-15: saldo ausente é erro de integridade409 SALDO_INEXISTENTE, sem criação automática; confirmação/saída sem saldo suficiente dão409 sem efeito. Saldo final máximo do int é2.147.483.647; qualquer soma/estorno excedente reverte integralmente com SALDO_EXCEDE_LIMITE. Multiplicação checked de preço em centavos Int64 por quantidade inteira não exige arredondamento; somente médias usam AwayFromZero final, conforme RF-17.

ADR-21–23 preservam RN-02–11: produto incorpora saldo; itens/snapshots são incorporados na venda, movimentos permanecem imutáveis e separados. Nenhuma reserva. Validações locais do documento e índices não substituem integridade de referências, autorização e transação multi-documento. Preço/ativo do cliente/produto e vínculo administrativo requerem escrita efetiva contra write skew; verificar resultado de updates. Recibo/chave preservam intenção em retries e commit incerto; não repetir com chave nova.
