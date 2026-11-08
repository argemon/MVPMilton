# Relatório de homologação — Bloco 10

Execução em 07/11/2026, America/Cuiaba. **Homologação funcional e visual aprovada; CI integral do commit912eb15 aprovado.** Base `b3b7b1d`, branch `codex/final-homologation`. ADR-38 registra a autorização e as decisões antes das alterações.

## Escopo e método

API e Blazor reais em HTTPS local, Edge headless controlado por CDP e único banco `gestao_comercial_dev` no Atlas existente. `.env` preservado por SHA-256, sem novos bancos locais, simulações, deploy ou merge. Registros manuais fictícios têm prefixo `HOMO10`; não são apagados nem confundidos com a carga demonstrativa. A suíte MongoDB existente cria bancos descartáveis e fica no CI Testcontainers já existente, sem alterar fixtures para usar o banco persistente.

## Evidências já executadas

| Área | Resultado observado | Evidência local |
| --- | --- | --- |
| Fundação | Restore locked e build Release; API/Blazor/Atlas/login disponíveis | `artifacts/block10/restore.log`, `build.log`, `build-layout-final.log`, `before/{api,web}.log` |
| Unitários | 126 aprovados, 0 falhas/ignorados | `artifacts/block10/unit-retry/`, `unit-retry.log` |
| Autenticação e permissões | 57 verificações HTTP: senha incorreta, três perfis, conta vinculada, troca obrigatória, revogação, logout e policies | `artifacts/block10/api-checks.json` |
| Cadastros | Funcionário criado/editado/recarregado; cliente criado/editado; categoria/marca criadas/pesquisadas; produto com saldo inicial0 | `artifacts/block10/journey.json` |
| Estoque e venda | Entrada20; 3×12,50=37,50; confirmação/saldo17; cancelamento/saldo20; histórico cancelado preservado; saída21 rejeitada | `journey.json`, `screens/jornada-cancelamento.png`, `api-checks.json` |
| Dashboard/Relatórios | Mesmos totais no período01/08–08/11/2026; cancelamento não altera receita líquida | `artifacts/block10/totals.json` |
| Integridade | Zero divergências produto×soma dos movimentos, inclusive histórico manual | `artifacts/block10/persistent-check.json` |
| Visual inicial | 29 rotas×4 larguras, nenhum overflow horizontal de página ou exceção JS | `artifacts/block10/responsive.json`, `screens/` |

A primeira execução unitária foi anulada por falha de conexão do testhost; a repetição com comunicação local autorizada passou. Uma compilação diagnóstica com saída comum da solução produziu NETSDK1194; o build normal posterior passou com0 avisos/erros. Duas esperas da automação ocorreram antes da conexão interativa de formulários; as mesmas operações foram repetidas/retomadas, sem atribuir falha funcional ao produto nem apagar registros. Não são testes aprovados até a conclusão real da jornada.

## Defeitos e correções

- Navegação em oito faixas acima do cabeçalho: organizada em barra lateral agrupada por área, preservando policies e rotas. Menu recolhível e inicialmente fechado em telas menores.
- Conteúdo limitado a70rem: área principal fluida, com limites apenas para campos e formulários.
- Campos de busca fora de formulários colados aos links: labels/campos em blocos com espaçamento.
- Acabamento: tipografia, cards, cores, tabelas, cabeçalhos, bordas, foco e estados de erro consistentes. Sem nova biblioteca ou regra comercial.

Captura anterior: `screens/before-dashboard.png`. A primeira revisão visual encontrou menu móvel aberto e campos colados; a varredura final repetiu33 páginas/fichas×4 larguras (132 medições),0 overflow de página e0 exceções JavaScript. Referências Bling/Omie/Tiny não foram consultadas; nenhuma comparação atual é afirmada.

## Comandos reais

`dotnet restore --locked-mode`; `dotnet build --no-restore --configuration Release`; `dotnet test tests/GestaoComercial.UnitTests -c Release --no-build --logger trx --results-directory artifacts/block10/unit-retry`; `./scripts/verify-architecture.ps1`; hosts executados pelos DLLs Release em7143/8081 e7243/8082; scripts locais CDP para auditoria e jornadas; verificador HTTP .NET e reconciliação MongoDB somente leitura. Logs e roteiros auxiliares privados ficam em `.local/`/`artifacts/`, ignorados pelo Git, sem credenciais nos documentos versionados.

## Roteiro para a banca — 12 minutos

| Tempo | Demonstração |
| --- | --- |
| 0–1 min | Objetivo: cadastros, estoque e vendas; execução local com Atlas; sem emissão fiscal/pagamento |
| 1–2 min | Login administrativo e perfis Administrador/Vendedor/Estoquista |
| 2–4 min | Funcionários, clientes, categorias/marcas e produto de homologação |
| 4–5 min | Entrada20 e histórico rastreável |
| 5–7 min | Rascunho de3×12,50, confirmação e saldo17 |
| 7–8 min | Cancelamento justificado, estorno e saldo20; histórico original preservado |
| 8–10 min | Dashboard com período01/08/2026–08/11/2026; indicadores e gráficos |
| 10–12 min | Relatórios com mesmo período e CSV; explicar repetição/restauração protegida da carga |

Usar produto fictício próprio para demonstrar movimentos, evitando alterar os produtos da carga. Não restaurar a carga durante a fala: mostrar a tela/confirmar proteção e explicar que a operação leva minutos.

## Checklist antes da apresentação

- Internet e Atlas acessíveis; computador conectado à energia.
- SDK.NET10 instalado e certificado HTTPS local confiável (`dotnet dev-certs https --trust`, se necessário).
- `.env` existente disponível, sem expor seu conteúdo.
- Restore/build concluídos; API e Blazor iniciados conforme README.
- Login válido, sem lockout; acesso guardado localmente, nunca em slides ou Git.
- Carga disponível; Dashboard e relatórios com período explícito01/08–08/11/2026.
- Produto fictício para a jornada; conferir saldo inicial antes de repetir a demonstração.
- Navegador em zoom100%, tamanho desktop e páginas prontas; fechar consoles com informações privadas.

## Resultado final e limites

| Módulo | Homologação |
| --- | --- |
| 00 Fundação | Hosts HTTPS, health, Atlas, navegação e páginas de erro disponíveis |
| 01 Autenticação | Login/logout reais dos três perfis; erro de senha no formulário; CSRF400; expiração real de sessão própria401; trocas e revogações |
| 02 Funcionários | Criação/edição/persistência, ativar/desativar, conta vinculada e paginação |
| 03 Clientes | PF sintético válido, CPF/CNPJ inválidos, duplicidade409, busca/paginação e histórico cancelado |
| 04 Produtos | Categoria/marca, cadastro e busca, SKU duplicado409, precisão monetária400, PNG privado carregado na página |
| 05 Estoque | Entrada/saída/ajustes, negativo bloqueado, repetição idempotente e corpo divergente409; saldo20 final |
| 06 Vendas | Rascunho3×12,50, confirmação, baixa, cancelamento, estorno e histórico preservado |
| 07 Dashboard | Indicadores e gráficos reais; receita final18401569 centavos e totais iguais aos relatórios |
| 08 Relatórios | Cinco filtros/consultas/CSV;251 vendas no arquivo,26 canceladas contribuindo0; soma18401569 centavos |
| 09 Carga | Repetição→restauração→repetição aprovadas no único banco; carga final completa e manuais preservados |

75 verificações HTTP específicas (57+18),16 verificações de perfis no navegador e expiração real401 têm registros próprios; não somar essas verificações como se fossem novos testes xUnit. PNG foi reconferido no caminho persistente de apresentação após reinício. O verificador da conciliação inicialmente gravou no caminho histórico09: a cópia prévia efetivamente existente em .local/demo-before-repeat.json foi restaurada e o resultado10 separado em artifacts/block10/persistent-check.json; nenhum histórico versionado foi removido.

Totais finais no banco:16 funcionários,15 contas,42 clientes,101 produtos,6 categorias,6 marcas,251 vendas (225 confirmadas/26 canceladas),857 movimentos;0 divergências. Incluem a carga12/12/40/100/5/5/250/850, administrador inicial e registros fictícios manuais criados na homologação. Todos os manuais permanecem identificados;4 funcionários adicionais incluem o primeiro cadastro da tentativa de automação. Não houve limpeza de coleções. Receita líquida18401569 centavos, compartilhada por Dashboard, relatório e soma exata do CSV, reconferida após a restauração. A carga levou vários minutos; sua execução integral não é recomendada durante a fala.

Evidências finais adicionais: demo.json, browser-roles.json, expiry.json, extra-checks.json, totals.json, csv-checks.json, csv-totals.json, responsive-final.json, zoom-keyboard.json, login-ui.json, extra-ui.json e image-final.log em artifacts/block10. Revisão em1920×1080,1366×768,768×1024 e390×844. Zoom125%/150% foi emulado por viewport CSS equivalente1092/910px e escala de dispositivo, não acionado pelo menu de zoom do navegador. Menu móvel também validado com foco real e sequência completa de Enter. Ficha de usuário e imagem privada visitadas adicionalmente. Capturas inspecionadas, incluindo Dashboard, relatórios, formulários, tabelas e telas de permissão.

Build final Release0 avisos/erros;126 unitários locais finais,0 falhas/ignorados. Nenhum teste transacional foi substituído por memória. [CI PR42427827241](https://github.com/argemon/MVPMilton/actions/runs/42427827241) e [push42427798381](https://github.com/argemon/MVPMilton/actions/runs/42427798381), commit912eb15: sucesso;126 unitários+116 integrações=242 aprovados,0 falhas/ignorados, sem filtros, MongoDB8.0.17/Testcontainers autenticado/replica set. Todas as etapas, auditoria e scanner aprovadas; logs guardados em ci-pr.log/ci-pr.json. Auditoria local NuGet sem vulneráveis e Gitleaks241 arquivos sem segredos. .env e acesso privado ignorados/preservados. Ajuste final de script de navegação removeu um listener sem uso, recompilado e conferido localmente; o CI do HEAD final será acompanhado no PR antes da entrega.

[PR5](https://github.com/argemon/MVPMilton/pull/5), sem merge/deploy. Sem defeito crítico funcional identificado na vistoria. Corrigidos3 problemas de apresentação (navegação, largura e campos colados), mais polimento simples. Limites: não se afirma enumeração exaustiva de todo estado transitório nem teste nativo de zoom; concorrência/rollback/segurança/volume são comprovados pela suíte integral no CI, não executada contra o banco persistente. Requisitos comerciais históricos de publicação/backup isolado/HA não fazem parte da aprovação acadêmica ADR-37/38. Não foram implementadas novas funcionalidades ou bibliotecas. Não há próximo bloco: preparar a máquina e apresentar à banca.
