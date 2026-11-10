# 06 — Critérios de aceite

> **Planejamento oficial vigente (07/11/2026):** TCC para uma apresentação local em um único ambiente e banco MongoDB Atlas. Blocos 00–08: concluídos conforme evidências do projeto; 09: carga de dados fictícios no banco existente; 10: homologação funcional e acabamento visual. Sem publicação, hospedagem, catálogo ou pedidos online. Orientações históricas divergentes abaixo não prevalecem sobre este escopo.


Todos os dados são sintéticos. `Dado / Quando / Então` define resultado observável, incluindo ausência de efeitos parciais. A coleção é o roteiro mínimo e não substitui casos de limite dos módulos.

| ID / rastreio | Dado | Quando | Então |
| --- | --- | --- | --- |
| CA-01 — RF-01/02, RNF-01 | Conta ativa com senha válida e sessão inexistente | Entrar por formulário e depois sair | Cookie HttpOnly/Secure contém referência; token só no servidor; login abre home, logout impede próximo comando; senha inválida retorna mensagem genérica |
| CA-02 — RF-02/03, RN-16 | Usuário conectado em dois circuitos | Admin inativa conta ou muda perfil; outro teste avança relógio 30 min sem uso | Próxima chamada API recebe 401; UI perde acesso em até 60 s na revalidação; nenhuma escrita é aplicada |
| CA-03 — RF-04, RN-08/16/18 | Funcionário ativo com conta vinculada | Editar contato e desativar | Versão cresce; conta e sessões são revogadas na transação; autoria antiga permanece; último Admin não pode ser inativado (ADR-31) |
| CA-04 — RF-05/06, RN-12/17 | Cliente com CPF sintético válido, duas vendas de vendedores distintos | Pesquisar pelo documento e consultar histórico como um vendedor | Localiza uma ficha, documento duplicado dá 409; histórico contém apenas venda própria; inativo permanece consultável com filtro |
| CA-05 — RF-07/08, RN-08/12 | Categoria e marca ativas | Criar produto e repetir SKU normalizado, depois inativar categoria usada | Produto tem saldo 0; duplicata e inativação bloqueada dão 409; GET preserva vínculo e versão |
| CA-06 — RF-09/10/11/12, RN-09/14 | Produto com saldo 0 | Entrada 10, saída 2, ajuste -1 com motivos válidos | Saldo 7, três movimentos com ator/data/motivo/saldoAntes/Depois; soma de deltas 7; histórico paginado |
| CA-07 — RF-10/11, RNF-02 | Saldo 1 | Saída 2 ou ajuste -2 e insert/update inválido direto no MongoDB de teste | API 409 sem efeitos; validador MongoDB rejeita saldo negativo e filtro condicional impede baixa excessiva |
| CA-08 — RF-13/14/15, RN-01/10/11 | Cliente ativo, produto a 12,50, saldo 10 | Criar com 2 unidades e confirmar | Total 25,00 calculado no servidor; saldo 8; uma baixa; confirmada não aceita editar itens |
| CA-09 — RF-14, RN-03 | Dois produtos: saldo 10 e 0 | Confirmar venda de uma unidade de cada | 409; primeiro saldo continua 10, nenhum movimento, venda Rascunho |
| CA-10 — RF-14, RN-04 | Produto saldo 1 e duas vendas de 1, sessões independentes | Confirmar simultaneamente com barreira de teste | Uma 200 e outra 409; saldo 0; uma única baixa comercial |
| CA-11 — RF-14, RN-05 | Venda válida e uma chave idempotente | Repetir em paralelo e simular perda da resposta após commit | Mesmo status/corpo para a chave; uma baixa; chave com corpo diferente 409; nova chave após confirmar 409 |
| CA-12 — RF-16, RN-06/07 | Venda confirmada de 2 com baixa registrada | Administrador cancela com motivo e repete a chave | Um estorno +2, estado Cancelada e metadados completos; baixa anterior intacta; nova chave não estorna novamente |
| CA-13 — RF-11/15/16, RN-15/17 | Venda de A e usuário B vendedor; estoquista conectado | B lê/edita venda de A, vendedor cancela própria ou estoquista ajusta | 404 para escopo alheio; 403 para policies negadas; nenhum efeito/auditoria de sucesso |
| CA-14 — RF-15/17/18/19, RN-07/13 | Vendas 25 e 15 confirmadas no período, segunda cancelada; produto saldo 0/mínimo 0 | Renomear produto e abrir dashboard/relatório | Receita 25, confirmadas 1, canceladas 1, ticket 25; produto aparece em estoque baixo; nome histórico permanece |
| CA-15 — RF-12/19/20, RNF-12, RN-20 | Eventos na borda inicial e final do intervalo, mais de uma página | Consultar e exportar filtro | Início incluído/fim excluído no fuso; total geral independe de página; >366 dias ou >10 mil linhas é rejeitado, sem truncar |
| CA-16 — RNF-06/08 | Backup de banco, imagens, key ring e versão anterior conhecida | Restaurar em instalação isolada e aplicar upgrade testado | Login e jornada comercial funcionam, imagens presentes, razão/saldo conciliam; tempo e RPO registrados dentro da meta |
| CA-17 — RF-03/08/19/20, RNF-01/04/10 | Dois usuários no BFF; payloads CSRF, imagem falsa e CSV com fórmula | Alternar chamadas, forjar POST, enviar SVG disfarçado e exportar texto malicioso | Sem mistura de sessão; CSRF rejeitado; imagem 415/400; fórmula neutralizada; logs/Git/HTML/storage sem credenciais; último Admin protegido inclusive em disputa |
| CA-18 — RNF-03/05/07 | Build Release, ambiente medido e UI em 360/1280 px | Executar suíte, carga definida e jornada por teclado | Zero falhas críticas; p95 registrado nas metas; formulários sem perda de ação por layout e erros associados ao campo |

## Evidência e incremento

Cada execução registra CA, versão/commit, pré-condições, comando/roteiro, esperado, observado e evidência. CA-04 no bloco 03 aceita histórico vazio; repetir com vendas reais sintéticas no 06. CA-16/18 devem ser interpretados somente no contexto de homologação e apresentação local; antes disso módulos verificam somente o subconjunto já entregue.

Falhas em P0 impedem avanço. Não marcar cenário aprovado porque existe endpoint, porque compilou ou porque o documento foi escrito.

## Evidências técnicas após revisão MongoDB

CA-01–18 não mudam de resultado comercial. Nova implementação deve comprová-los com MongoDB real isolado via Atlas ou perfil Testcontainers com replica set, sessões independentes, updates condicionais, índices únicos/validadores e transações snapshot/majority. Em CA-05/06/08/14, persistência armazena 1250/2500 centavos e resposta mantém 12.50/25.00 reais; média 1.005 arredonda só a apresentação final para1.01. CA-07 inclui validação direta do servidor e usuário runtime sem bypass. CA-10/11 incluem conflito de escrita e commit incerto, sem efeito duplicado. A restauração exigida no Bloco 09 refere-se apenas a dados demonstrativos identificados no único banco Atlas, com preservação de dados manuais. Documentação não é evidência de execução.

CA-ADR39 — limpeza explícita: sem sessão401; Vendedor/Estoquista403; confirmação inválida400 sem efeito; operação concorrente409. Com confirmação, transação remove dados comerciais e demais usuários, preservando executor/vínculo/sessão. Falha na auditoria reverte todas as exclusões. Gerar depois recria cenário e repetir o UUID antigo não remove a nova carga. Testar somente em banco isolado, nunca acionar a limpeza do banco de apresentação como verificação.
