# Documentação acadêmica do Gestão Comercial

Versão para revisão acadêmica, elaborada em **24 de novembro de 2026**. Código analisado: [`1940b83460ec93d8efa2011db9dc9177570b0586`](https://github.com/argemon/MVPMilton/commit/1940b83460ec93d8efa2011db9dc9177570b0586). A documentação descreve um protótipo acadêmico com dados fictícios e apresentação local. Não representa submissão aprovada nem nova homologação funcional.

## Documentos para leitura e edição

| Documento | PDF | Word | Texto |
| --- | --- | --- | --- |
| Monografia | [Ler](entrega/monografia.pdf) | [Editar](entrega/monografia.docx) | [Markdown](entrega/monografia.md) |
| Documentação técnica | [Ler](entrega/documentacao-tecnica.pdf) | [Editar](entrega/documentacao-tecnica.docx) | [Markdown](entrega/documentacao-tecnica.md) |
| Manual do usuário | [Ler](entrega/manual-do-usuario.pdf) | [Editar](entrega/manual-do-usuario.docx) | [Markdown](entrega/manual-do-usuario.md) |

O [pacote completo](Pacote-documentacao.zip) contém documentos, diagramas, fontes e registros de verificação. Os PDFs são convertidos dos respectivos Word, com paginação conferida. Não há uma versão PDF escrita independentemente do Word.

## Conteúdo

A monografia contém elementos pré-textuais, resumo e abstract, introdução, fundamentação, metodologia, desenvolvimento, avaliação, conclusão, referências e apêndices. O título é provisório: **Desenvolvimento de um sistema web de gestão comercial com ASP.NET Core Blazor e MongoDB**.

A especificação técnica consolida RF-01–20, RN-01–20, RNF, CA e a exceção ADR-39, com **29 casos de uso**, **61 rotas API**, **63 contratos compartilhados**, dicionário de **14 coleções**, índices e protocolos de segurança/concorrência. A matriz relaciona requisitos, casos, implementação e testes existentes; o inventário registra **142 métodos Fact/Theory**, sem apresentá-los como casos executados ou aprovados nesta tarefa.

O manual descreve instalação já preparada, execução, acesso, os três perfis e os procedimentos dos módulos. Explica rascunho, revisão e conclusão da venda, descarte, cancelamento, estoque, relatórios, CSV e as diferenças entre geração, restauração e limpeza.

Os **22 diagramas** incluem visão geral e detalhes de casos de uso, classes conceituais e recortes de classes reais, pacotes, componentes, modelo documental, sequências de acesso/confirmação/cancelamento/estoque, atividades, estados e execução local. Cada um tem `.puml`, `.svg` e `.png` em [diagramas](diagramas/). O [índice dos diagramas](diagramas/indice.json) identifica os artefatos de código de origem. Ligações invisíveis em PlantUML organizam a posição e não representam relações adicionais.

## Fontes e limites

- [Referências verificadas](fontes/referencias.json): autoria, título, edição/versão, endereço, data, conteúdo utilizado e ressalvas. São 14 fontes existentes, incluindo OMG, IEEE Computer Society, Pearson, Microsoft, MongoDB, OWASP, Testcontainers e orientação de biblioteca universitária.
- [Inventário das fontes públicas](evidencias/inventario-fontes.json): commit, caminhos e hashes dos arquivos examinados. Não contém `.env`, credenciais nem conteúdo do Atlas.
- [Inventário de testes](evidencias/testes-inventario.json) e [contratos](evidencias/contratos.json): extração estática dos arquivos desta versão.
- [Verificações documentais](evidencias/validacao-documental.json), [paginação](evidencias/paginacao.json), [revisão visual](evidencias/revisao-visual.json) e [manifesto](evidencias/manifesto-entrega.json): controles locais dos artefatos entregues.

Resultados de software registrados em 07/11, 09/11 e 23/11/2026 são identificados como **históricos**, com abrangência e limitações. A pasta de capturas `artifacts` citada nos relatórios não está disponível neste checkout. Imagens `.local/unigran-*.png` são montagens estáticas e não foram apresentadas como jornadas funcionais. O manual utiliza procedimentos textuais, sem fabricar capturas de operações autenticadas.

A inspeção encontrou diferenças entre textos antigos e o código, como nome físico do usuário, policies conceituais, paginação de usuários, campos técnicos e escopo idempotente. A documentação técnica explica essas diferenças. Metas antigas de carga e backup não são apresentadas como resultados demonstrados do escopo acadêmico atual.

## Pendências acadêmicas

Preencher autor, instituição, curso, orientador, natureza/titulação, cidade e ano acadêmico. A menção a TCC Milton Paes/Unigran na aplicação não foi tomada como identificação completa ou confirmação institucional do manuscrito. Preencher aprovação e banca somente quando houver dados reais; ficha catalográfica, assinaturas e declarações dependem da instituição.

O padrão é **ABNT provisório**: A4, Times New Roman 12, espaçamento 1,5 no corpo, margens superior/esquerda de 3 cm e inferior/direita de 2 cm, exceções de tabelas, fontes e legendas. A capa não é contada; a contagem textual considera as folhas a partir da folha de rosto, com número exibido na parte textual. Sumário e legendas possuem campos Word com cache de paginação preenchido. A identificação das edições das normas usa orientação pública de biblioteca; não se presume consulta integral licenciada nem validação pelas regras específicas do curso.

Antes de submissão, revisar com o orientador a metodologia, o conteúdo, a normalização, o resumo e o abstract. Eventual exigência de capturas reais, ensaio de desempenho ou avaliação com usuários precisa de evidências próprias e autorização operacional correspondente.

## Reprodução dos documentos

As fontes autorais ficam em [fontes](fontes/): a monografia e o manual em Markdown, base técnica em Markdown, casos de uso e bibliografia em JSON. A especificação técnica completa é consolidada pelo script; não editar somente o arquivo gerado se a mudança precisar ser reproduzível. Os três Markdown em entrega são versões completas para leitura sem processar marcadores.

Usar os runtimes e bibliotecas retornados por `load_workspace_dependencies` no Codex. Bibliotecas necessárias: python-docx, Pillow, pypdf, pdf2image e Poppler. Para conversão, utilizar o `render_docx.py` da habilidade documents e LibreOffice. PlantUML foi fixado em1.2024.8, com Java8 e layout Smetana, sem Graphviz externo. LibreOffice25.2.7.2 foi extraído administrativamente em cache local; não é dependência do sistema comercial. Ferramentas binárias e imagens de revisão ficam em `.local`, fora do pacote.

Sequência de reprodução, com caminhos adequados à máquina:

```powershell
# Defina $docPython, $docRenderer, $docPoppler e $docJava com os caminhos disponíveis.
# Use o runtime indicado pelo Codex, sem instalar dependências no código da aplicação.
& $docPython 'Documentação/recursos/criar_diagramas.py'
& $docJava '-Djava.awt.headless=true' -jar '.local/documentacao-tools/plantuml-1.2024.8.jar' -charset UTF-8 -tsvg 'Documentação/diagramas/*.puml'
& $docJava '-Djava.awt.headless=true' -jar '.local/documentacao-tools/plantuml-1.2024.8.jar' -charset UTF-8 -tpng 'Documentação/diagramas/*.puml'
& $docPython 'Documentação/recursos/renderizar_documentos.py' --renderer $docRenderer --poppler-dir $docPoppler
& $docPython 'Documentação/recursos/exportar_textos.py'
& $docPython 'Documentação/recursos/preparar_revisao_visual.py'
# Inspecione as páginas renderizadas antes de atualizar o registro de revisão visual.
& $docPython 'Documentação/recursos/validar_entrega.py'
& $docPython 'Documentação/recursos/empacotar.py'
```

Se as ferramentas não estiverem no cache, `recursos/preparar_ferramentas.py` baixa os binários oficiais de versões fixadas. O LibreOffice precisa ser extraído para `.local/documentacao-tools/libreoffice`, sem substituir a instalação do usuário; o gerador aceita outro diretório com `--soffice-dir`. O pacote não inclui o código-fonte ou binários das ferramentas de terceiros, apenas URLs de obtenção e scripts autorais.

A geração pode realizar múltiplas passagens até estabilizar sumários e paginação. Após qualquer alteração do conteúdo, repetir renderização, revisão visual e validação. Alterações em Word fora das fontes não são preservadas numa geração posterior. A produção deste pacote não executou testes da aplicação, comandos de inicialização, operações no banco, commit, push, merge ou deploy.
