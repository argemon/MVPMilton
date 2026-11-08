# Relatório de homologação — Bloco 10

Execução em 07/11/2026, America/Cuiaba. **Em andamento; aprovação final depende das jornadas restantes e do CI.** Base `b3b7b1d`, branch `codex/final-homologation`. ADR-38 registra a autorização e as decisões antes das alterações.

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

Captura anterior: `screens/before-dashboard.png`. A primeira revisão visual encontrou menu móvel aberto e campos colados; ajustes posteriores exigem nova varredura. Referências Bling/Omie/Tiny não foram consultadas; nenhuma comparação atual é afirmada.

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

## Pendências desta execução

Concluir restauração/repetição da carga e preservação dos manuais, CSV, login/navegação real dos perfis não administrativos, revisão final após polimento, build/testes finais, scanner, push/PR e CI. Não declarar o Bloco10 concluído antes dessas evidências.
