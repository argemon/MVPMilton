# Bloco 10 — Homologação final e acabamento visual do TCC

## 1. Objetivo

**Testar e corrigir todos os módulos 00–09 e deixar o Web App bonito, consistente e pronto para uma apresentação presencial à banca.** Trata-se de uma demonstração local única, não de um sistema comercial em produção.

## 2. Premissas e exclusões

- .NET 10, ASP.NET Core API, Blazor Interactive Server e **um único MongoDB Atlas**, usando a configuração atual.
- O sistema roda na máquina do apresentador; conexão com a internet é necessária para acessar o Atlas.
- **Não criar** ambientes/bancos adicionais, deploy público, monitoramento avançado, alta disponibilidade, catálogo ou pedidos online.
- Não redesenhar o sistema nem adicionar features. Seguir SOLID e KISS; corrigir apenas o que realmente atrapalha qualidade, uso ou demonstração.
- Precondição: Bloco 09 concluído e a carga demonstrativa disponível.

## 3. Vistoria funcional obrigatória

Executar o aplicativo e verificar os fluxos reais, não apenas ler o código ou rodar testes unitários:

| Área | Cenários principais |
| --- | --- |
| Fundação | Inicializar API e Blazor, verificar comunicação e Atlas |
| Autenticação | Login, senha inválida, logout, perfis e acesso negado |
| Funcionários | Cadastrar, listar, pesquisar, editar, desativar/reativar |
| Clientes | Cadastrar, editar, pesquisar, validações e duplicidade |
| Produtos | Categorias, marcas, produto, SKU, preço, imagem, ativação |
| Estoque | Entrada, saída, ajuste, saldo, histórico e autorização |
| Vendas | Itens, totais, confirmação, baixa, cancelamento e estorno |
| Dashboard | Faturamento, ticket, vendas, estoque baixo e gráficos |
| Relatórios | Filtros, totais, paginação, CSV e coerência com Dashboard |
| Carga | Gerar, repetir e restaurar dados fictícios sem apagar dados manuais |

Testar pelo menos três jornadas completas: (a) login e cadastros; (b) entrada **20** → venda **3** → saldo **17** → cancelamento → saldo **20**, com histórico; (c) carga → Dashboard → Relatórios → CSV → restauração.

## 4. Auditoria visual no navegador

Abrir **todas as páginas** e estados principais (carregando, vazio, erro e sucesso) quando possível. Inspecionar e corrigir:

- **Layout:** sidebar, cabeçalho, largura fluida do navegador, grids, alinhamentos, espaçamentos e ausência de sobreposição.
- **Design:** cores consistentes, tipografia legível, ícones coerentes, botões e ações principais claros.
- **Cadastros:** formulários alinhados, campos agrupados, validações visíveis e mensagens compreensíveis.
- **Tabelas:** colunas legíveis, filtros organizados, valores alinhados, paginação e rolagem horizontal somente na tabela quando necessário.
- **Dashboard:** cards, indicadores, gráficos, legendas e números fáceis de entender.
- **UX:** navegação intuitiva, confirmação antes de ações destrutivas, loading/erro/vazio e feedback de sucesso.
- **Acessibilidade básica:** navegação por teclado, foco visível, labels e contraste adequado.

Usar padrões visuais modernos de sistemas de gestão (Bling, Omie, Tiny) como referência de usabilidade, **sem copiar telas ou marcas**. Não afirmar comparação atual sem consultá-los.

### Responsividade

Web App normal, **sem tamanho de tela fixo**, aproveitando a largura disponível. Priorizar desktop e verificar **1920×1080, 1366×768, 768px e 390px**, além de zoom 125% e 150% quando possível. Se a tela já estiver adequada, não alterá-la.

## 5. Correções e prioridade

1. **P1 — Obrigatório:** login, permissões, persistência, venda, estoque, cálculo ou fluxo essencial quebrado.
2. **P2 — Corrigir:** layout quebrado, navegação confusa, tabela/formulário difícil de usar, responsividade comprometida.
3. **P3 — Se simples:** polimento de bordas, espaçamentos, ícones, animações e detalhes estéticos sem efeito funcional.

Corrigir os problemas encontrados; repetir fluxos afetados e garantir que as correções não causem regressão. Não fazer refatorações extensas por preferência estética.

## 6. Verificações técnicas

- Executar `dotnet restore`, `dotnet build -c Release` e `dotnet test -c Release` quando o ambiente permitir; registrar números de testes, falhas e ignorados.
- Conferir as regras críticas já implementadas: estoque não negativo, concorrência, confirmação/cancelamento idempotentes, permissões e dinheiro em centavos.
- Executar navegação real pelo Blazor; automação de navegador é bem-vinda se estiver disponível, sem criar infraestrutura complexa para isso.
- Não realizar cargas massivas, ensaios de produção ou deploy.
- Testar operações de demonstração com dados fictícios identificáveis; **não limpar coleções inteiras**.

## 7. Preparação para a banca

- Preparar instruções curtas para iniciar API e frontend, checar conexão Atlas e fazer login com conta demonstrativa.
- Confirmar que carga, produtos, estoque, vendas, Dashboard e Relatórios exibem dados úteis.
- Criar roteiro de **10 a 15 minutos**: login → cadastros → produtos → entrada de estoque → venda → cancelamento → Dashboard → Relatórios; explicar carga/restauração se relevante.
- Preparar checklist pré-apresentação (internet, Atlas, processos em execução, dados restaurados, navegador aberto).

## 8. Documentação e evidências

Atualizar `docs/execution/03-ESTADO-PROJETO.md` e registrar um relatório conciso em `docs/execution/07-RELATORIO-HOMOLOGACAO.md` (se o nome já estiver ocupado, usar novo nome sem sobrescrever histórico). Informar módulos testados, cenários, resultados, bugs corrigidos, pendências, fotos/capturas quando disponíveis e resultado do CI. Não inventar evidências.

## 9. Definition of Done

- [x] Blocos 00–09 verificados com fluxos reais; erros críticos corrigidos.
- [x] Estoque/vendas/estorno e números de Dashboard/Relatórios coerentes.
- [x] Carga e restauração funcionando no único banco Atlas.
- [x] Todas as telas principais vistoriadas visualmente e layout profissional/responsivo.
- [x] Build/testes aplicáveis aprovados, ou limitações explícitas sem declarar aprovação indevida.
- [x] Instruções de execução local e roteiro da apresentação preparados.
- [x] Documentação e estado atualizados; GitHub/CI consultados quando disponíveis.

**Fim do projeto:** apresentar o sistema localmente à banca. Não há Bloco 11 nem publicação posterior.

## Evidência de execução — 07/11/2026

Homologação real no banco existente, jornadas/carga/CSV/perfis e acabamento visual comprovados no [relatório](../execution/07-RELATORIO-HOMOLOGACAO.md).126 unitários locais finais e242 testes no CI integral do commit912eb15 (126+116),0 falhas/skips.33 páginas/fichas×4 larguras,0 overflow de página/JS; zoom equivalente125/150 e teclado conferidos. Sem deploy/merge, novo banco local ou alteração do .env. Os detalhes, limites e links de CI ficam no relatório e estado.
