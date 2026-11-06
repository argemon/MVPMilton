# M04 — Produtos

## 1. Identificação, objetivo e valor

**Bloco 04, P1.** Entregar catálogo interno de produtos, categorias, marcas, preço e imagem; preparar saldo zerado sem permitir operação de estoque. Produto passa a ter identidade e dados válidos para uso pelo 05.

## 2. Pré-requisitos e dependências concluídas

00–03 aprovados; contas/policies/auditoria e armazenamento configurável disponíveis. Ler arquitetura 04–07 e RN-01/08/12/19. Movimentação e venda não existem nem são usadas nos testes deste bloco.

## 3. Requisitos e prioridades

RF-07/08 P1 completos; RNF-01/03/04/07/08/11/12; CA-05 e parte de CA-17 de arquivos. Preparar RN-02/09 somente com criação de saldo zero. H-02 permanece UN inteira.

## 4. Escopo e exclusões

Dentro: categoria, marca, produto, preço, estoque mínimo, status, imagem e saldo inicial zero. Fora: entrada/saída/ajuste, saldo inicial informado, venda, custo/lucro, variações/lotes, catálogo público e integração fiscal/financeira.

## 5. Atores e permissões

Todos os quatro perfis: Produtos.Ler, inclusive categorias/marcas/imagens. Admin/Gerente/Estoquista: Produtos.Gerenciar. Vendedor só consulta. A API valida a permissão em todos os métodos, inclusive upload e DELETE exclusivo de imagem.

## 6. Entidades e migrations

Criar categorias/marcas (nome normalizado único, ativo, versão), produtos (SKU único, nome, descrição, categoria obrigatória, marca opcional, preço numeric(18,2), mínimo int, ativo, versão), imagens_produto e saldos_estoque. Produto e saldo zero são criados juntos. FKs RESTRICT. Não criar Movimento/Venda antecipadamente. Saldo CHECK >=0 e PK produto_id. Imagens ficam em volume privado fora de webroot; banco guarda metadados/chave, não caminho fornecido pelo usuário.

## 7. Fluxos principais e alternativos

1. Criar categoria/marca → criar produto ativo com referências ativas → transação cria produto e saldo 0 → detalhe.
2. Editar preço/dados/status com versão → validar referências e unicidade → auditar.
3. Inativar categoria/marca → bloquear referência e verificar produto ativo; se houver, 409 sem efeito.
4. Inativar produto preserva cadastro/saldo; reativar exige categoria/marca ativas.
5. Upload → validar/reencodar arquivo temporário → atualizar metadado e versão → após commit remover arquivo anterior. Falha mantém imagem antiga; órfão não referenciado fica para limpeza segura.
6. Remover imagem → conferir versão e desassociar metadado → limpar arquivo após commit, sem excluir produto.

## 8. Regras e validações

RN-01/08/12/19. SKU 1–40, trim/uppercase e caracteres permitidos; nome 2–150; descrição até 2000 sem HTML; categoria obrigatória, marca opcional. Preço >0 até 9.999.999,99, decimal com até duas casas: terceira casa é rejeitada, não arredondada. Quantidade não existe no cadastro. Mínimo inteiro 0–1.000.000. Arquivo JPEG/PNG real, até 2 MiB e 4096×4096, sem SVG; validar assinatura/decodificação e remover metadados. Backend repete tudo; não confiar no MIME/extensão enviados.

## 9. Endpoints REST

Base `/api/v1`; `ProdutoDetalhe={id,sku,nome,descricao,categoriaId,marcaId,preco,estoqueMinimo,saldo:0,imagemUrl?,ativo,versao,createdAt,updatedAt}` neste bloco. Categoria/Marca: `{id,nome,ativo,versao}`. Falhas ProblemDetails.

| Método / rota | Request | Response | Erros |
| --- | --- | --- | --- |
| GET `/categorias` ou `/marcas` | `search,ativo,page,pageSize` | 200 página | 400; 401/403 |
| GET `/categorias/{id}` ou `/marcas/{id}` | UUID | 200 detalhe | 401/403/404 |
| POST `/categorias` ou `/marcas` | `{nome}` | 201 detalhe + Location | 400; 401/403; 409 NOME_DUPLICADO |
| PUT `/categorias/{id}` ou `/marcas/{id}` | `{nome,ativo,versao}` | 200 detalhe | 400; 401/403/404; 409 versão/NOME_DUPLICADO/REFERENCIA_EM_USO |
| GET `/produtos` | `search,categoriaId,marcaId,ativo,page,pageSize` | 200 página de resumos com saldo informativo | 400; 401/403 |
| GET `/produtos/{id}` | UUID | 200 detalhe | 401/403/404 |
| POST `/produtos` | `{sku,nome,descricao?,categoriaId,marcaId?,preco,estoqueMinimo}` | 201 detalhe + Location | 400; 401/403; 404 referência; 409 SKU_DUPLICADO/RECURSO_INATIVO |
| PUT `/produtos/{id}` | Mesmos campos + `{ativo,versao}` | 200 detalhe | 400; 401/403/404; 409 versão/SKU/referência inativa |
| POST `/produtos/{id}/imagem` | Multipart `arquivo,versao` | 200 detalhe com nova versão/imagemUrl | 400 arquivo corrompido; 401/403/404; 409 versão; 413 tamanho; 415 tipo |
| GET `/produtos/{id}/imagem` | Sem corpo | 200 bytes com Content-Type validado | 401/403; 404 produto/imagem |
| DELETE `/produtos/{id}/imagem?versao=N` | Sem corpo | 204 | 400 versão; 401/403/404; 409 versão |

POST/PUT criam ativos por padrão sem campo de saldo. Listas máximo 100, busca prefixo de nome/igualdade SKU. Não há DELETE de entidades comerciais. Para falta de imagem já removida, GET/DELETE retorna 404; UI relê antes de apresentar estado atual.

## 10. Páginas, componentes e estados

| Rota | Componentes propostos | Vazio / carregando / sucesso / erro |
| --- | --- | --- | --- |
| `/produtos` | ListaProdutos, FiltroProduto, Paginacao | Sem resultados; skeleton; lista; Repetir |
| `/produtos/novo` | FormularioProduto, SeletorCategoriaMarca, CampoMonetario | Sem referências orienta cadastro permitido; salvando; criado; erros por campo |
| `/produtos/{id}` | DetalheProduto, UploadImagem, ConfirmacaoStatus | Sem imagem usa placeholder; carregando; salvo; conflito pede reler |
| `/categorias`, `/marcas` | ListaReferencia, EditorReferencia | Nenhuma referência; enviando; salvo; duplicidade/em uso |

Inputs de preço usam pt-BR e enviam decimal JSON; botões de estoque não existem ainda. Imagem é transmitida pelo BFF autenticado, sem token em URL. Componentes em 360/1280 px com teclado/foco.

## 11. Erros e concorrência

Locks de cadastro: categorias antigas/novas ordenadas → marcas antigas/novas ordenadas → produto → saldo se necessário. Conferir versão do produto após locks; se vínculo mudou desde leitura preliminar, retornar 409/reler, não adquirir nova referência fora da ordem. Mesmo lock na inativação de referência impede produto ativo sob referência inativa. UNIQUE resolve SKU/nome concorrente. Arquivo falho não deixa referência quebrada; falha DB faz rollback de produto/saldo/auditoria.

## 12. Testes unitários e integração

Unidade: escala monetária/limites, normalização SKU, mínimo, referências e arquivo/caminho seguro. Integração PostgreSQL: produto/saldo atômicos, SKU/nome concorrentes, versão, inativação versus vínculo, 401/403, arquivo real/falso/grande, troca concorrente preservando arquivo anterior e leitura privada. Não registrar entrada fictícia para demonstrar produto; saldo fica zero.

## 13. Given/When/Then

| ID / rastreio | Given | When | Then |
| --- | --- | --- | --- |
| M04-CA01 / CA-05 | Categoria ativa | Criar produto preço 12.50 | 201, saldo 0, uma linha de saldo e nenhum movimento |
| M04-CA02 / RN-12 | SKU ABC cadastrado | Criar ` abc ` | 409 SKU_DUPLICADO |
| M04-CA03 / CA-05 | Referência com produto ativo | Inativar referência | 409 REFERENCIA_EM_USO, produto permanece válido |
| M04-CA04 / CA-17 | Usuário com Produtos.Gerenciar | Enviar SVG disfarçado ou arquivo >2 MiB | 415 ou 413; imagem anterior intacta |
| M04-CA05 / RN-01 | Formulário de preço | Enviar 12.505 ou saldo no corpo | 400; sem arredondamento silencioso ou criação parcial |

## 14. Checklist de execução

- [ ] Implementar cadastros/validações/permissões e volume privado; zero endpoint de movimento.
- [ ] Gerar/revisar migrations, testar do zero/upgrade 03→04 e aplicar explicitamente.
- [ ] Conferir locks/constraints e atomicidade produto+saldo+auditoria.
- [ ] Implementar DTOs/HTTP/UI/imagens e atualizar OpenAPI.
- [ ] Executar `dotnet restore`, `dotnet build --no-restore -c Release`, `dotnet test -c Release --no-build` em PostgreSQL isolado.
- [ ] Registrar cenários, arquivos, limpeza de órfãos e comandos no estado.

## 15. DoD e desbloqueio

DoD global, RF-07/08 e M04-CA01–05 demonstrados. Saldo zero íntegro e imagem segura; nenhuma operação de estoque antecipada. Desbloquear **05**. Testes não usam movimentações nem vendas futuras.
