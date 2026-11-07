# 09 — Testes e qualidade

> Atualização vigente00 — ADR-27–29: implementação autorizada com Atlas já configurado em .env, sem sobrescrever segredos. Development reutiliza MONGODB_URI; Production/Staging exigem configuração externa específica. Integração pode executar transações reais em bancos efêmeros exclusivos no mesmo cluster ou perfil Testcontainers explícito; não criar outro MongoDB quando Atlas atende. rs0 é nome apenas do perfil local, não do Atlas. As exigências universais de Docker/rs0, somente Markdown e adaptação ainda não autorizada nas seções históricas anteriores ficam superadas nesses pontos.


## Estratégia

| Nível | Ferramenta / alvo | Evidência mínima |
| --- | --- | --- |
| Unidade | xUnit, Domain/Application, relógio controlado e portas falsas | Regras de dinheiro, status, permissão contextual e validação |
| Integração | xUnit, WebApplicationFactory e MongoDB real isolado em container | HTTP, Identity, evoluções de persistência, índices, escritas de guarda e transações |
| Web/BFF | xUnit para serviço/handler e integração host Web | CSRF, cookie, isolamento e falhas da API |
| Jornada | Roteiro de navegador em homologação; automação se disponível | Login → entrada → venda → cancelamento → relatório |

MongoDB real é a dependência obrigatória de integração: o caminho atual usa Atlas com bancos exclusivos por execução (ADR-28); Testcontainers para .NET é o perfil opcional local/CI. Compose serve aos hosts e ensaio operacional, sem criar outro banco no desenvolvimento Atlas. Nunca apontar para produção, nunca usar substitutos em memória para provar escritas de guarda ou validadores e filtros condicionais MongoDB. Usar bancos lógicos isolados por execução e limpeza apenas do ambiente identificado como teste. Não desligar paralelismo para esconder erro de concorrência.

## Casos que não podem faltar

1. Saldo inicial 1, duas sessões e transações independentes confirmando vendas de 1: barreira sincroniza disputa; exatamente uma 200 e uma 409, saldo 0 e uma baixa.
2. Venda com dois itens, um insuficiente: zero baixas, saldo de ambos intacto e estado Rascunho.
3. Duas chamadas com a mesma chave: mesmo resultado e um efeito; mesma chave/corpo diferente: 409. Testar rollback antes de commit e resposta perdida após commit.
4. Confirmação repetida com nova chave: 409 sem efeito. Cancelamento simultâneo/repetido: um estorno por item, saldo restaurado uma vez.
5. Atualização concorrente de preço/inativação versus confirmação: resultado deve respeitar conflitos das escritas de guarda, nunca usar leitura isolada sem a guarda efetiva que gera conflito com a alteração concorrente.
6. Duas edições com mesma versão: uma vence, outra 409. Soma dos movimentos igual ao saldo após todas as operações.
7. Vendedor tentando cancelar ou ler venda alheia; Estoquista tentando confirmar; chamadas diretas à API devem falhar.
8. Dois administradores tentando remover o último acesso simultaneamente: pelo menos um continua ativo.
9. Circuito antigo após inativação/troca de perfil, expiração por relógio controlado, duas sessões BFF sem mistura de token, CSRF e logout com API indisponível.
10. Snapshot histórico preservado após renomear produto/cliente; filtros por período na virada de dia/fuso; CSV com texto que inicia fórmula.

## Pipeline planejado

No bloco 00, preparar restore bloqueado por versões, build Release, testes unitários e integração, análise estática, verificação de dependências vulneráveis e ausência de segredos. A etapa de integração deve falhar explicitamente se MongoDB não estiver disponível, sem marcar testes como aprovados por skip.

Comandos obrigatórios por bloco: `dotnet restore`, `dotnet build --no-restore -c Release`, `dotnet test -c Release --no-build`. A forma exata de subir o banco deve ser registrada pelo executor. Executá-los na implementação e registrar resultados reais no estado03.

## Critérios mensuráveis

- Zero testes críticos falhando e zero avisos novos no build.
- Todo RF entregue mapeado a CA e evidência; segurança e integridade exigem teste automatizado, não apenas captura de tela.
- Cobertura serve para identificar lacunas; não aprovar regra crítica somente por percentual.
- RNF-05: medição com 20 usuários concorrentes, base sintética de 10 mil produtos/50 mil vendas, aquecimento e janela de 5 min. p95 de listas <= 2 s, confirmação <= 3 s, excluindo inicialização do container. Registrar CPU/RAM, volume e scripts. Meta sujeita à revisão documentada após baseline, sem mascarar falha.
- Verificação visual em 360 px e 1280 px, navegação por teclado, labels e foco em erros.

## Gate

Bloco só termina com [DoD](../execution/02-DEFINITION-OF-DONE.md) e critérios específicos satisfeitos. Falha de integridade ou autorização bloqueia avanço. Evidências ficam no estado por bloco; resultados desta etapa se limitam à consistência documental.

## Fixture MongoDB real e testes da mudança de persistência

Usar Testcontainers para iniciar MongoDB em replica set de um nó rs0; não presumir que o módulo padrão já habilite transações. Configurar mongod com replSet, inicializar uma vez, aguardar hello com setName=rs0 e isWritablePrimary=true, autenticar e comprovar transação com commit/abort na fixture. Credenciais efêmeras e banco(s) únicos por execução; limpar apenas recursos com identidade de teste validada. Dentro da rede Docker usar o hostname anunciado; do runner com porta dinâmica usar URI de teste com replicaSet=rs0 e directConnection=true para não descobrir endereço interno inacessível. Esse modo é para fixture de um nó, não topologia de produção. Se não houver Docker, falhar explicitamente ou registrar teste não executado, nunca usar mock/standalone como prova transacional.

Verificar instalação sem coleções, reexecução do inicializador sem duplicar/apagar dados, evolução incremental com documentos da versão anterior, validadores strict/error e índices únicos parciais (inclusive null/ausente/conta inativa). Índice multikey não deve ser tomado como garantia de produto único no mesmo array. Exercitar permissão runtime contra update/remove de histórico, mudança de schema e leitura cruzada entre bancos.

Casos monetários: 12.50↔1250, terceira casa rejeitada, 1250×2=2500, limites de preço/total, overflow checked, agregação que excede Int64 sem promoção para double, média de 100/101 centavos arredondando para101, CSV/HTTP preservando reais. Não usar doubles nos asserts de dinheiro.

Disputas usam sessões independentes, barreira no início de tentativa e repetição compatível com retries (não bloquear barreira já consumida). Testar: duas baixas saldo1; múltiplos itens com um insuficiente; produto/cliente inativado ou preço alterado versus rascunho/confirmação; inativação de categoria versus vínculo; vínculo/último Admin concorrente. Injetar falhas TransientTransactionError e UnknownTransactionCommitResult sem endpoint de falha em produção, comprovar que só commit é repetido no segundo caso e que mesma chave recupera um efeito. Sem Task.WhenAll dentro de sessão; concorrência é entre sessões.

Evidências anteriores da fundação não validam MongoDB. A adaptação00 e o bloco01 possuem novas evidências Atlas no estado03; não transferir aprovação das provas antigas para módulos seguintes. Consulte [o módulo MongoDB na lista oficial Testcontainers .NET](https://dotnet.testcontainers.org/modules/) e [transações do driver](https://www.mongodb.com/docs/drivers/csharp/current/crud/transactions/).

## Matriz explícita dos testes exigidos pela auditoria

| Cenário | Preparação e ação | Assert obrigatório | Rastreio/bloco |
| --- | --- | --- | --- |
| Venda suficiente | Cliente/produto ativos, saldo10, preço1250 centavos, confirmar2 unidades |200, total2500 centavos interno/25.00 HTTP, saldo8 e uma baixa | CA-08/M06-CA01,06 |
| Venda insuficiente | Saldo1 e confirmação de2, ou dois itens sendo um insuficiente |409, sale Rascunho, todos os saldos/movimentos/auditoria/recibos de sucesso intactos | CA-09/M06-CA02,06 |
| Duas vendas concorrentes | Saldo1, sessões/transações independentes e barreira, ambas pedem1 | Uma200/outra409, saldo0, uma baixa | CA-10/M06-CA03,06 |
| Falha durante confirmação | Injetar exceção após primeiro decremento e antes de commit (por exemplo falha de auditoria) somente no host de teste |500 seguro ou falha transitória contratada, rollback de todos os documentos, sem recibo de sucesso; mesma chave pode executar uma vez depois | RN-03/CA-09,06 |
| Repetição da requisição | Mesma chave/corpo em paralelo e após resposta perdida; depois corpo divergente | Mesmo recibo, um efeito; divergência409; commit incerto resolvido sem nova intenção | CA-11/M06-CA04,06 |
| Cancelamento e estorno | Confirmada de2, produto depois inativo, cancelar e repetir |200 Cancelada, um estorno+2, baixa original imutável; nova chave409 | CA-12/M06-CA05,06 |
| Falha de conexão MongoDB | Fixture00 derruba conexão/primary e depois recupera, sem remover volume |ready503/live200, mensagem sem URI/segredo, ready200 após reconectar e versão técnica válida | M00-CA01/02/03,00 |
| Permissões | Anônimo/perfil negado/vendedor sobre venda alheia e runtime sem privilégio de histórico |401/403/404 conforme matriz, nenhum efeito; MongoDB nega update/remove de histórico | CA-13/17/M05-CA05/M06-CA07,01/05/06 |

Estes são testes previstos e asserts objetivos, não testes executados pela auditoria. Fixtures técnicas isoladas não criam endpoints de produção nem dependências de módulos futuros.
