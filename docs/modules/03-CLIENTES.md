# M03 — Clientes

## 1. Identificação, objetivo e valor

**Bloco 03, P1.** Entregar cadastro e pesquisa de clientes, habilitando seleção futura de cliente sem precisar antecipar vendas.

## 2. Pré-requisitos e dependências concluídas

Normas transversais: arquitetura05/09 e ADR-20–29, MongoDB.Driver oficial restrito à infraestrutura, transações sequenciais em topologia compatível e testes MongoDB reais isolados (Atlas ou perfil Testcontainers explícito) conforme ADR-28. Classes/serviços de módulo futuro não são pré-requisitos ocultos.

00–02 aprovados: sessões, policies, auditoria e CRUD seguro já demonstrados. Ler arquitetura 04–06, RN-08/12/17 e ADR-14. Venda não existe neste ponto e não pode ser criada como pré-requisito oculto.

## 3. Requisitos e prioridades

RF-05 P1 completo; RF-06 P1 apenas contrato/histórico vazio. RF-06 preenchido será concluído em 06, sem bloquear 03. RNF-01/03/07/08/11/12; CA-04 incremental. Não executar a parte do CA-04 que exige duas vendas antes do 06.

## 4. Escopo e exclusões

Dentro: PF/PJ, nome, documento opcional, email/telefone opcionais, pesquisar, editar, ativar/desativar e aba de histórico vazio tipado. Fora: crédito, cobrança, contas, endereço de entrega, importação, consulta externa de documento e implementação de venda.

## 5. Atores e permissões

Administrador/Vendedor: Clientes.Ler/Gerenciar/Historico. Estoquista: negar 403. Histórico no 06 respeita vendas próprias do vendedor; aqui todos os autorizados recebem vazio por inexistência do módulo, não por ausência comprovada de vendas externas.

## 6. Entidades e banco

Criar `customers`: _id UUID, nome 2–150, nomeNormalizado, tipo PF/PJ, documento normalizado string de até 14 dígitos ou null, contatos opcionais, ativo, timestamps UTC, versao Int64 e guardVersion. Índice unique parcial em documento com partialFilterExpression $type string, inclusive inativos; documento ausente é null e string vazia é rejeitada. Índice de nomeNormalizado/_id para prefixo. Cadastro e audit_events no mesmo commit. Histórico vazio tipado no 03 não consulta sales inexistente; substituir apenas no 06. Sem incorporar histórico de vendas no cliente.

## 7. Fluxos principais e alternativos

1. Pesquisar nome por prefixo ou documento por igualdade → paginar resultado → abrir ficha.
2. Criar PF/PJ → validar documento se informado → gravar ativo e auditar → detalhe.
3. Editar dados/status com versão → validar unicidade → atualizar/auditar.
4. Documento duplicado → 409, manter formulário; orientar localizar cadastro existente, inclusive inativo.
5. Desativar preserva ficha e auditoria; filtro padrão ativo esconde da seleção, `ativo=todos` permite consultar.
6. Abrir Histórico → página vazia estável; registrar a entrega parcial no estado/documentação. No06 substituir a consulta e atualizar a evidência, sem depender de implementação futura para responder agora.

## 8. Regras e validações

RN-08/12/15/17. Nome obrigatório; tipo enum; documento só dígitos, CPF 11/CNPJ 14 com dígitos verificadores e sem sequência inválida. Se ausente, null; não exigir sem resolver Q-01. Trocar PF/PJ exige documento compatível no mesmo comando. Contato com formato/limites do catálogo. Backend normaliza e valida, ignora nenhuma falha da UI. Sem DELETE físico.

## 9. Endpoints REST

Base `/api/v1`. `ClienteDetalhe={id,nome,tipo,documento,email,telefone,ativo,versao,createdAt,updatedAt}`. Erros ProblemDetails.

| Método / rota | Request | Response | Erros |
| --- | --- | --- | --- |
| GET `/clientes` | `search,tipo,ativo,page,pageSize` | 200 página de resumos | 400 filtro; 401/403 |
| GET `/clientes/{id}` | UUID | 200 detalhe | 401/403/404 |
| POST `/clientes` | `{nome,tipo,documento?,email?,telefone?}` | 201 detalhe + Location | 400 campos; 401/403; 409 DOCUMENTO_DUPLICADO |
| PUT `/clientes/{id}` | Mesmos campos + `{ativo,versao}` | 200 detalhe | 400; 401/403/404; 409 versão/documento |
| GET `/clientes/{id}/vendas` | `page,pageSize,inicio?,fim?` | 200 `{items:[],page,pageSize,totalItems:0}` neste bloco | 400 período/página; 401/403; 404 cliente |

Quando usado período opcional, informar as duas datas, início < fim, intervalo <=366 dias. Cliente inativo continua consultável. Campos desconhecidos ou tentativa de enviar venda/saldo/ator são rejeitados. Histórico verifica existência e policy antes de retornar vazio.

## 10. Páginas, componentes e estados

| Rota | Componentes propostos | Vazio / carregando / sucesso / erro |
| --- | --- | --- | --- |
| `/clientes` | ListaClientes, FiltroCliente, Paginacao | Nenhum cliente encontrado; carregando; página; erro com filtros preservados |
| `/clientes/novo` | FormularioCliente, DocumentoPfPj | Campos vazios; salvando; detalhe criado; erro por campo/duplicidade |
| `/clientes/{id}` | DetalheCliente, ConfirmacaoStatus, HistoricoCliente | Não localizado/histórico vazio; carregando; salvo; 409 pede reler |

Aba histórico mostra “Nenhuma venda registrada”; a limitação incremental fica no estado/documentação, sem detalhes de implementação na interface administrativa. Não usar erro 500 nem chamar endpoint inexistente. Sucesso não apaga filtros; erro não descarta formulário.

## 11. Erros e concorrência

Índice unique resolve documentos duplicados concorrentes em 409 DOCUMENTO_DUPLICADO. Update com filtro _id+versao e incremento efetivo, resultado sem correspondência distingue 404 de 409 por leitura autorizada. Auditoria falha implica abortar cadastro. Alteração de cliente escreve o mesmo documento usado como guarda pelas vendas futuras; testes de confirmação chegam somente no 06.

## 12. Testes unitários e integração

Unidade: documentos válidos/inválidos/ausentes, tipo, contatos, normalização e limites. Integração MongoDB: CRUD, busca/paginação/inativos, UNIQUE concorrente, versão, auditoria atômica, 401/403 e contrato vazio com 404 para cliente inexistente. Fixtures não criam coleções futuras. Teste de preenchimento do histórico fica explicitamente em 06.

## 13. Given/When/Then

| ID / rastreio | Given | When | Then |
| --- | --- | --- | --- |
| M03-CA01 / CA-04 parcial | Cliente sintético PF válido | Buscar documento exato | Uma ficha, versão e contatos esperados |
| M03-CA02 / RN-12 | Documento já cadastrado, inclusive inativo | POST com mesmo documento | 409 DOCUMENTO_DUPLICADO; uma ficha somente |
| M03-CA03 / RF-06 parcial | Cliente existente e policy válida, sem módulo Venda | Abrir histórico | 200 página vazia; nenhum acesso a coleção futura |
| M03-CA04 / RN-08 | Cliente ativo | PUT ativo=false e consultar com todos | Registro preservado; ausente do filtro ativo padrão |
| M03-CA05 / RN-12 | PF com documento preenchido | Trocar para PJ mantendo CPF | 400 e dados anteriores preservados |

## 14. Checklist de execução

- [ ] Implementar Cliente, validação e auditoria; criar adaptador vazio de histórico identificado para substituição.
- [ ] Gerar/revisar evolução de persistência; testar base limpa e upgrade 02→03; aplicar explicitamente no ambiente local.
- [ ] Implementar API/DTOs/UI/cliente HTTP e verificar OpenAPI.
- [ ] Executar `dotnet restore`, `dotnet build --no-restore -c Release`, `dotnet test -c Release --no-build` com MongoDB isolado.
- [ ] Registrar CA-04 parcial/RF-06 parcial, M03-CA01–05 e próximos passos no estado.

## 15. DoD e desbloqueio

DoD global, RF-05 e todos os testes do incremento aprovados. RF-06 parcial é condição planejada, não defeito oculto. Desbloquear **04**. Nenhuma dependência de software futuro: histórico vazio funciona agora e é substituído somente no 06.
