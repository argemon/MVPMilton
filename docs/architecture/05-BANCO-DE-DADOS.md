# 05 — Banco de dados

## Tipos e convenções

PostgreSQL 18 como baseline proposta (ADR-06); fixar patch suportado no bloco 00. EF Core 10 com provedor Npgsql compatível. Schema `comercial` para negócio/Identity/sessões API; `web` para tickets BFF, com usuários distintos e sem leitura cruzada. Colunas `snake_case`, UUID para PK, `timestamptz` UTC para instantes, `date` somente quando existir data civil. `numeric(18,2)` para dinheiro, `integer` para quantidades; não usar ponto flutuante ou tipo `money`.

Tabelas mutáveis têm `created_at`, `updated_at`, `version bigint` inicial 1. Toda edição incrementa versão; EF configura token de concorrência e UPDATE condicionado à versão, inclusive comandos SQL explícitos. Movimentos e auditoria são imutáveis.

## Dicionário mínimo

| Tabela | Campos além da PK e metadados | Constraints / índices |
| --- | --- | --- |
| funcionarios | nome varchar(150), email varchar(254) null, telefone varchar(20) null, ativo bool | nome não vazio; índice nome normalizado |
| usuarios e tabelas Identity | campos Identity, nome_exibicao varchar(150), funcionario_id null, perfil varchar(20), ativo, must_change_password | username/email de login normalizado UNIQUE; funcionario_id UNIQUE onde não nulo; perfil permitido |
| sessoes_api | usuario_id, token_hash bytea, criado_em, expira_em, ultimo_uso_em, revogada_em null | token_hash UNIQUE; índice usuário/expiração; nunca token bruto |
| clientes | nome varchar(150), tipo char(2), documento varchar(14) null, email/telefone null, ativo | tipo PF/PJ; documento normalizado UNIQUE parcial; nome não vazio |
| categorias / marcas | nome varchar(80), nome_normalizado varchar(80), ativo | nome_normalizado UNIQUE, inclusive inativos |
| produtos | sku varchar(40), sku_normalizado, nome varchar(150), descricao varchar(2000) null, categoria_id, marca_id null, preco numeric(18,2), estoque_minimo int, ativo | SKU normalizado UNIQUE inclusive inativos; preco > 0 e <= 9999999.99; mínimo entre 0 e 1000000 |
| imagens_produto | produto_id, chave_arquivo varchar(100), mime varchar(30), tamanho_bytes int | produto_id UNIQUE; tipos permitidos; tamanho <= 2097152 |
| saldos_estoque | produto_id como PK/FK, quantidade int, version bigint | CHECK quantidade >= 0; um saldo por produto |
| vendas | cliente_id, criador_id, status, total numeric(18,2), cliente_nome_snapshot, cliente_documento_snapshot null, confirmado_em/por null, cancelado_em/por null, motivo_cancelamento null | status permitido; total >= 0; metadados compatíveis com estado; índice confirmado_em/status e cliente_id/criado_em |
| itens_venda | venda_id, produto_id, quantidade int, preco_unitario numeric(18,2), total numeric(18,2), nome_snapshot, sku_snapshot | UNIQUE(venda_id,produto_id); quantidade entre 1 e 1000000; preço válido; total = quantidade × preço |
| movimentos_estoque | produto_id, delta int, saldo_antes int, saldo_depois int, tipo, motivo varchar(500), ator_id, ocorrido_em, venda_id null, item_venda_id null, operacao_id null | delta <> 0; saldos >= 0; saldo_depois = saldo_antes + delta; sinal por tipo; índice produto/ocorrido_em/id; UNIQUE(item_venda_id,tipo) parcial para venda/estorno |
| operacoes_idempotentes | id, ator_id, escopo, chave UUID, hash_request, status_http, recurso_id, resposta_json, criado_em | UNIQUE(ator_id,escopo,chave); índice criado_em; DTO original protegido pelo mesmo controle de acesso do recurso, sem credenciais |
| auditorias | ator_id nullable para evento anônimo, origem, acao, recurso_tipo/id, ocorrido_em, correlation_id, detalhes_json | índice recurso_tipo/recurso_id/ocorrido_em; ator obrigatório em mutação administrativa/comercial, acesso administrativo |
| web.tickets | handle_hash, ticket_protegido, token_api_protegido, expira_em | hash UNIQUE; expiração indexada; Data Protection persistido fora do banco |

FKs usam RESTRICT/NO ACTION, inclusive Identity/autoria. Não usar cascade para histórico. Sem DELETE público; inativação e descarte lógico. Remover itens de rascunho dentro de PUT é permitido; itens de venda confirmada nunca são removidos. Restrição de 100 itens e soma de total são validadas no serviço transacional, pois CHECK de linha não soma outras linhas.

## Integridade adicional

Tipos de movimento: Entrada (+), SaidaManual (-), Ajuste (delta com sinal), Venda (-), EstornoVenda (+). CHECK exige venda/item em Venda e EstornoVenda, e operação manual nos demais. FK composta `(item_venda_id,venda_id,produto_id)` aponta para chave única correspondente do item, impedindo referência cruzada. `operacao_id` manual UNIQUE limita um movimento por comando. Ajuste recebe delta, não saldo absoluto, com justificativa obrigatória.

Cancelada conserva confirmado_em/por e exige cancelado_em/por/motivo; Confirmada exige confirmação e proíbe cancelamento; Rascunho/Descartada não têm esses metadados. Limites e nulabilidade são aplicados em migrations e não apenas na UI.

Venda conserva também identificação de exibição dos operadores na confirmação/cancelamento, além das FKs de autoria; renomear funcionário não reescreve esses snapshots. Para descarte, gravar `descartado_em`, `descartado_por` e `motivo_descarte`, exigidos somente no estado Descartada. Usuário permanece preservado mesmo inativo. Índices de busca inicial atendem prefixo do nome normalizado e igualdade de documento/SKU; busca não promete substring sem índice adequado.

## Protocolo transacional obrigatório

Isolamento READ COMMITTED, bloqueios explícitos e mesma conexão. Ordem global para comandos comerciais: registrar/bloquear chave idempotente → venda (quando houver) → cliente (confirmação) → produtos em UUID crescente → saldos na mesma ordem. Atualizações de cadastros seguem a ordem de suas linhas; nenhum comando de cadastro adquire venda depois de produto. Usar `SELECT ... FOR UPDATE` parametrizado na infraestrutura; preço/status também são lidos sob bloqueio. Nunca manter locks durante chamada HTTP ou arquivo.

Nos cadastros de produto, bloquear primeiro categorias antigas/novas por UUID, depois marcas antigas/novas por UUID, depois produto e saldo se necessário. Inativação de categoria/marca bloqueia a própria linha antes de verificar produtos ativos. Criação/reativação/troca de vínculo de produto toma o mesmo lock e revalida referência ativa. Isso impede corrida entre vincular produto e inativar referência; cadastro nunca adquire cliente/venda depois desses locks. Confirmação não precisa bloquear categoria/marca: a integridade dos vínculos ativos é mantida pelo protocolo de cadastro.

Operações idempotentes mantêm resposta original inclusive snapshots pessoais mínimos já contidos no DTO da venda. Não duplicar payload de entrada livre, senha ou token. Acesso e retenção acompanham o recurso comercial; respostas originais só são devolvidas depois de revalidar sessão, policy e propriedade atual.

### Criar/editar rascunho

Criação/edição de rascunho também bloqueia cliente e produtos ordenados para validar ativo/preço no mesmo instante da gravação, sem bloquear/consumir saldo. Em PUT, bloquear venda antes do cliente; em POST com chave, UUID novo é reservado em memória e a linha é inserida depois de validar referências. Não há linha de venda anterior para bloquear. Produto inativado depois do commit do rascunho será rejeitado na confirmação.

### Confirmar

1. Validar sessão/policy e formato. Abrir transação; inserir a operação idempotente ou aguardar a concorrente terminar, conforme protocolo API.
2. Bloquear venda. Checar dono/perfil, versão esperada e estado Rascunho. Não repetir baixa se já Confirmada.
3. Bloquear cliente/produtos; validar ativos, itens e preço atual igual ao snapshot.
4. Bloquear todos os saldos em ordem; validar saldo suficiente para todos os itens.
5. Atualizar saldos (defesa extra: condição saldo >= quantidade), inserir uma baixa por item, snapshots finais, total, status, versão, auditoria e resposta idempotente.
6. Commit único. Exceção em qualquer etapa faz rollback de tudo. Não confirmar parcialmente.

### Cancelar

Mesma ordem e transação; exigir Confirmada e motivo, incrementar saldos pelos itens originais, inserir EstornoVenda por item, marcar Cancelada e auditar. Produtos/clientes inativos não impedem. Repetição da mesma chave retorna resposta registrada; nova chave em Cancelada retorna 409, sem movimento. Não apagar baixa anterior.

### Movimentos manuais

Transação com chave idempotente, produto e saldo bloqueados; produto deve estar ativo. Validar delta, impedir saldo negativo/overflow, gravar saldo, movimento, auditoria e resultado juntos. A soma de movimentos desde a criação zerada deve ser igual ao saldo. Correção ocorre por ajuste autorizado, nunca UPDATE manual de saldo.

## Concorrência e falhas

- Cadastros/rascunhos: token de versão e 409 `VERSAO_DESATUALIZADA`; não sobrescrever silenciosamente.
- Duas vendas com saldo 1: uma confirma, outra recebe 409 `ESTOQUE_INSUFICIENTE`; saldo final 0.
- Deadlock ou serialização: rollback; até duas novas tentativas da transação completa com a mesma chave e pequeno atraso aleatório. Esgotado: 503 e `Retry-After`; não repetir efeitos isolados.
- Falha de resposta após commit: repetir chave recupera o resultado; não pressupor rollback por timeout no cliente.
- Segurança: alterações de conta/perfil e proteção do último administrador usam lock de uma linha singleton `controle_administracao`, depois usuário/sessões. Impede duas desativações simultâneas eliminarem todos os administradores.

Auditoria de falha de login/solicitação pública proposta pode ter ator_id null com origem explícita; não criar usuário fictício para visitante. Mutação administrativa, movimento e venda exigem ator autenticado e FK válida. Não guardar contato público/token/senha indiscriminadamente nos detalhes de auditoria.

Detalhamento proposto ADR-16: em comandos que também envolvam vínculo/funcionário, ordem administrativa é controle_administracao → usuários existentes em UUID crescente → funcionários em UUID crescente → sessões. Criar conta, ativar conta e desativar funcionário compartilham esse protocolo e revalidam ativo/vínculo após adquirir locks. Não adquirir controle depois de funcionário nem usar check sem lock seguido de INSERT. Em atualização de produto, versão/vínculos devem ser revalidados após locks das referências; se a leitura preliminar estiver obsoleta, conflito409 em vez de adquirir nova referência fora da ordem.

ADR-15 esclarece que ausência de saldo aborta o comando com SALDO_INEXISTENTE, sem autocorreção. Soma/estorno além do int máximo2.147.483.647 aborta com SALDO_EXCEDE_LIMITE e rollback total. A integridade histórica prevalece sobre tentar estorno parcial.

## Migrations e operações

Migrations backend e web separadas, versionadas, revisadas com SQL gerado. Bloco 00 cria baseline e mecanismo; cada módulo adiciona suas tabelas. Aplicação não executa migration automaticamente em produção. Usuário de deploy tem DDL; runtime tem apenas DML necessário; auditorias/movimentos sem UPDATE/DELETE para runtime.

Sequência de FKs proposta ADR-16:00 apenas schemas;01 Identity/sessões sem FK para funcionário inexistente;02 cria funcionário e FK/índice;04 cria produto/saldo zero;05 cria movimentos manuais sem FK futura de venda;06 adiciona vendas/itens e suas FKs/índices nos movimentos. Dicionário deste documento descreve estado final do MVP, não uma migration monolítica inicial. Schema proposto do 11 fica no módulo11 e só é criado após liberação P2.

Testar criação do zero e upgrade da versão anterior em banco descartável; validar constraints via escrita inválida. Backup antes de deploy; downgrade destrutivo é substituído por restore validado ou migration corretiva. Persistir backups criptografados fora do host; retenção definida no documento de deploy.

## Fontes

O protocolo aplica o comportamento documentado de [bloqueios de linha do PostgreSQL](https://www.postgresql.org/docs/18/explicit-locking.html) e [concorrência do EF Core](https://learn.microsoft.com/en-us/ef/core/saving/concurrency). A ordem e invariantes acima são decisões deste projeto.
