# Ajuste da venda para PDV — 09/11/2026

Solicitação explícita: pesquisar referências na internet e substituir o formulário vertical por uma frente de caixa operacional. Complemento ADR-38 registrado antes da implementação. Continuação de acabamento10; nenhum novo bloco.

Referências consultadas: [Bling](https://www.bling.com.br/funcionalidades/frente-de-caixa), seleção rápida e registro de venda com estoque; [Syndata](https://www.syndata.com.br/Central_de_Ajuda/Versao_8.4.9/Vendas/Venda_PDV/Venda_PDV.html), entrada de produto/quantidade, lista de itens, total e fechamento. Adotada a composição de operação, sem copiar identidade visual nem introduzir financeiro/fiscal fora do MVP.

## Resultado

Produtos à esquerda, cupom à direita e navegação administrativa recolhida nesta rota. Busca por nome/SKU, paginação e Enter em SKU exato; inclusão repetida soma quantidades e mantém uma linha por produto. Controle +/−, entrada numérica e remoção; cliente obrigatório com busca; total em destaque. Revisão salva e recupera os valores do servidor antes da confirmação explícita. Após conclusão, resumo e próximo atendimento. Cancelamento/descarte continuam com motivo e confirmação. Erros, resultado incerto e chave idempotente preservados. Controles desabilitados até a tela interativa conectar, evitando clique perdido durante abertura/navegação. Sem mudança de API, regras comerciais ou persistência.

## Validação real

- Build Release isolado e build Web padrão: zero avisos/erros. Comandos e logs em .local/pdv-ready.log, pdv-web-final-build.log.
- `dotnet test tests/GestaoComercial.UnitTests -c Release --no-build --no-restore -p:OutputPath=D:/Projetos/MVPMilton/.local/pdv-ready/ --verbosity minimal`:128 aprovados,0 falhas/skips; inclui teste de conta local preexistente fora deste PR.
- `dotnet test tests/GestaoComercial.IntegrationTests -c Release --no-restore -m:1 -p:OutputPath=D:/Projetos/MVPMilton/.local/pdv-final-tests/ --filter 'FullyQualifiedName~SalesBffTests|FullyQualifiedName~SalesTests.FullLifecycleUsesExactServerPricesSnapshotsAndOneRestock' --verbosity minimal`:2 aprovados,0 falhas/skips, Atlas real isolado, sem substituto em memória. SalesBffTests ampliado para verificar composição, CSS, busca, bloqueio de confirmação de cupom vazio e permissões. Sem modificar os testes transacionais existentes.
- Navegador Edge real no banco persistente: inclusão repetida de TCC-DEMO-001 →2; botão + →3; cliente sintético; revisão R$10,50 → confirmação. API conferiu saldo94→91, uma baixa−3. Cancelamento pela tela com motivo/checkbox →saldo94, um estorno+3 e baixa−3 preservada, venda Cancelada e snapshots intactos. Registro31a386a7-94dd-4140-ad88-de1b6ccfc87e preservado no histórico. Sem restaurar carga.
- Paginação1→2→1, busca vazia/recuperação, Enter real com campo focado, foco retornando à busca após inclusão, inclusão/remoção de segundo produto e busca de cliente aprovados. Tentativas iniciais da automação omitiram evento de texto do Enter e clicaram em HTML antes da conexão interativa; corrigido roteiro e bloqueio inicial, sem atribuir falha ao backend.
- 1920×1080,1366×768,768×1024,390×844: sem overflow de página/erros JS. Ação de finalizar dentro do viewport nas duas larguras desktop; em telas estreitas os painéis se empilham e a página rola. Capturas artifacts/block10/screens/pdv-final-{1920,1366,768,390}.png; também revisão/conclusão/cancelamento. Capturas1920/1366/390 revisadas visualmente. Não afirmar zoom nativo/auditoria completa de acessibilidade neste ajuste.

Arquivos: VendaDetalhe.razor, novo wwwroot/pdv.css, SalesBffTests.cs, ADR/estado, módulo06 e este relatório. Home/login/app.css e .vs preexistentes preservados; não incluir no commit desta solicitação. .env não editado; aplicação usa o Atlas existente. Sem deploy/merge. CI do novo código deve ser acompanhado no PR; gates anteriores não são prova da versão PDV.
