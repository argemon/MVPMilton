# 02 — Definition of Done (TCC)

A definição de pronto prioriza **funcionamento real e aparência adequada à banca**, não prontidão comercial para produção. Registrar apenas verificações efetivamente executadas.

## Comum aos blocos

- [ ] Funcionalidades previstas no MD do bloco implementadas e verificadas.
- [ ] Regras de autorização e validação do backend respeitadas.
- [ ] `dotnet restore`, `dotnet build -c Release` e `dotnet test -c Release` executados com resultados anotados quando o ambiente permitir.
- [ ] Erros relevantes corrigidos e testes afetados repetidos.
- [ ] Sem segredos ou `.env` no Git.
- [ ] Estado do projeto e decisões realmente novas atualizados.

## Gate do Bloco 09

- [x] Um único MongoDB Atlas existente, sem criar outro banco ou ambiente.
- [x] Carga gera usuários/perfis, funcionários, clientes, categorias, produtos, estoque e vendas históricas coerentes.
- [x] Reexecutar não duplica dados indevidamente; restauração opera só em registros identificados da carga e preserva dados manuais.
- [x] Estoque, cancelamentos/estornos, Dashboard e Relatórios conciliados.
- [x] Interface administrativa funcional e confirmação antes de restaurar.

## Gate do Bloco 10

- [ ] Todos os módulos 00–09 conferidos com operações reais, inclusive entrada 20 → venda 3 → saldo 17 → cancelamento → saldo 20.
- [ ] Autenticação/permissões, cadastros, vendas, estoque, Dashboard, relatórios, carga e restauração verificados.
- [ ] Páginas abertas no navegador e layout avaliado em 1920×1080, 1366×768, 768px e 390px, incluindo estados vazio/erro/carregamento.
- [ ] Falhas críticas e relevantes para a apresentação corrigidas; interface consistente, legível e responsiva.
- [ ] Roteiro de 10–15 minutos e instruções de inicialização local preparados.
- [ ] CI verificado, se disponível, com resultado real registrado.

**Fora da DoD:** publicar na internet, outros ambientes/bancos, testes de carga empresariais, alta disponibilidade, catálogo e pedidos online.

## Escopo09 vigente — ADR-37

Carga acadêmica usa exclusivamente banco Atlas e conexão existentes no .env, sem banco Demo/instância/URI/ambiente adicional. Gate09: [carga](../modules/09-CARGA-DEMONSTRACAO.md), com quantidades, conciliação, reexecução, restauração confirmada, dependências manuais protegidas, autorização e resultados reais de restore/build/test/CI. Não exige nova infraestrutura nem libera10. Homologação/backup e metas comerciais antigas não são o escopo desta carga. Testes técnicos existentes mantêm ADR-28.

Resultado09: DoD comum/específica demonstrada no relatório12; CI integral c622648 aprovado com242 testes,0 falhas/skips.10 não iniciado.
