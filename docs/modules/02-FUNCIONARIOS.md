# M02 — Funcionários

## 1. Identificação, objetivo e valor

**Bloco 02, P1 para cadastro e P0 para acesso.** Registrar funcionários e concluir criação de contas vinculadas, preservando autoria e controle administrativo.

## 2. Pré-requisitos e dependências concluídas

Normas transversais: arquitetura05/09 e ADR-20–29, MongoDB.Driver oficial restrito à infraestrutura, transações sequenciais em topologia compatível e testes MongoDB reais isolados (Atlas ou perfil Testcontainers explícito) conforme ADR-28. Classes/serviços de módulo futuro não são pré-requisitos ocultos.

00–01 aprovados: adaptadores de persistência, Identity, bootstrap, sessões, policies, auditoria e testes. Ler arquitetura 04–07, matriz e ADR-14. Não depende de Cliente/Produto/Venda.

## 3. Requisitos e prioridades

RF-04 P1 completo; concluir RF-03 P0 com criação vinculada. RNF-01/07/08/11; RN-08/15/16/18; CA-03 e partes de CA-02/17. Cadastro de funcionário não cria acesso automaticamente.

## 4. Dentro e fora do escopo

Dentro: cadastrar, listar, pesquisar, editar, ativar/desativar, criar conta opcional, revogar acesso vinculado. Fora: folha, cargos organizacionais, comissões, transferência de vínculo, exclusão física e integrações financeiras.

## 5. Atores e permissões

Administrador: Funcionarios.Ler/Gerenciar. Só Admin: Usuarios.Gerenciar e criação de conta. Administrador não pode desativar funcionário ligado a usuário Admin; API exige permissão adicional. Vendedor/Estoquista recebem 403 em todas as rotas deste cadastro. Nenhum perfil ignora a regra do último Admin.

## 6. Entidades e banco

Criar `employees`: _id UUID, nome 2–150, nomeNormalizado, email opcional até 254, telefone normalizado opcional 10–15 dígitos, ativo, createdAt/updatedAt UTC, versao Int64 e guardVersion técnico. Em users, employeeId referencia funcionário existente e ativo, validado na mesma transação; índice unique parcial em employeeId com filtro $type binData (UUID Standard validado no mapeamento/backend) exclui null/ausente e vale inclusive para conta inativa. Bootstrap mantém null. Sem chave estrangeira nem cascade: desativação não apaga autoria. Auditoria no mesmo commit, sem senha.

## 7. Fluxos principais e alternativos

1. Listar → novo → validar contato → salvar ativo e auditar → detalhe com versão.
2. Editar → enviar versão → validar → atualizar e incrementar versão. Conflito pede reler; não sobrescrever.
3. Admin escolhe funcionário ativo sem conta → informa email de login/perfil/senha temporária → cria conta com troca obrigatória → novo usuário troca senha antes de operar.
4. Desativar funcionário → escrever efetivamente controle administrativo e revalidar usuário/funcionário conforme protocolo → validar perfil do operador/último Admin → inativar funcionário, conta vinculada e sessões em uma transação.
5. Reativar funcionário → conta permanece inativa; reativação de conta é ação separada do Admin no módulo já existente.
6. Sem resultados → oferecer cadastro somente a autorizado. Funcionário sem conta → informar “Sem acesso ao sistema”, sem erro.

## 8. Regras e validações de formulário/backend

RN-08/12/15/16/18. Nome obrigatório, trim/espaços normalizados; email/telefone opcionais, válidos se informados. Email de contato não é email de login nem precisa ser único. Login é único pelo Identity. FuncionárioId obrigatório na criação humana, existente, ativo e sem conta. Perfil permitido, senha 12–128 com política do 01. Backend repete validação e recebe ator da sessão. PUT exige versão; exclusão física não tem endpoint.

## 9. Contratos REST

Base `/api/v1`. `FuncionarioDetalhe={id,nome,email,telefone,ativo,versao,createdAt,updatedAt}`. Consulta do usuário vinculado fica em Usuarios.Gerenciar, sem expor credencial no cadastro de funcionário.

| Método / rota | Request | Response | Erros |
| --- | --- | --- | --- |
| GET `/funcionarios` | `search,ativo,page,pageSize` | 200 página de funcionários, limite 100 | 400 filtro; 401/403 |
| GET `/funcionarios/{id}` | UUID | 200 detalhe | 401/403/404 |
| POST `/funcionarios` | `{nome,email?,telefone?}` | 201 detalhe + Location | 400 campos; 401/403 |
| PUT `/funcionarios/{id}` | `{nome,email?,telefone?,ativo,versao}` | 200 detalhe | 400; 403 proteção Admin; 404; 409 versão/último Admin |
| POST `/usuarios` | `{funcionarioId,email,senhaTemporaria,perfil}` | 201 `{id,nome,funcionarioId,email,perfil,ativo,versao,deveAlterarSenha:true}` + Location | 400; 401/403; 404 vínculo ausente; 409 vínculo ocupado/email duplicado/inativo |

401/403 também se aplicam às escritas; 500/503 são seguros, sem senha. Conflitos de vínculo usam código proposto VINCULO_OCUPADO; email duplicado EMAIL_DUPLICADO, conforme registro de esclarecimentos. Ativação de conta exige funcionário ativo.

## 10. Páginas, componentes e estados

| Rota | Componentes propostos | Vazio / carregando / sucesso / erro |
| --- | --- | --- |
| `/funcionarios` | ListaFuncionarios, FiltrosStatus, Paginacao | Nenhum resultado; skeleton; lista; Repetir com filtros preservados |
| `/funcionarios/novo` | FormularioFuncionario, ResumoValidacao | Campos vazios; salvando; detalhe criado; erros por campo |
| `/funcionarios/{id}` | DetalheFuncionario, ConfirmacaoInativacao | Não localizado; carregando; alteração confirmada; 409 oferece reler |
| `/usuarios/novo?funcionarioId=...` | FormularioUsuario, SeletorFuncionario | Sem funcionário elegível; carregando; conta criada; vínculo/email duplicado |

Rotas `/usuarios` e `/usuarios/{id}` do 01 passam a exibir vínculo/nome/email. Sucesso limpa senha temporária da tela; nunca a retorna na resposta. Administrador não vê ação de criar acesso. Foco no primeiro erro e teclado em 360/1280 px.

## 11. Erros e concorrência

Índice único resolve duas contas para o mesmo employeeId; mapear duplicidade para 409 VINCULO_OCUPADO. Criação/ativação de conta e desativação de funcionário atualizam o mesmo administration_control antes de users/employees e revalidam vínculo/ativo no snapshot novo. Guardas efetivas e comparação de versão impedem write skew. Revogação, conta, funcionário e auditoria são atômicos na mesma sessão; qualquer falha aborta tudo. Reativar funcionário não reativa conta.

## 12. Testes unitários e integração

Unidade: nome/contatos, elegibilidade de vínculo, permissões de administrador, reativação sem acesso automático. Integração xUnit/MongoDB: CRUD/paginação, referências/índices únicos, duas criações concorrentes, inativação versus criação, revogação transacional e rollback, último Admin via funcionário, 401/403, versão obsoleta e login com troca obrigatória da conta criada.

## 13. Given/When/Then

| ID / rastreio | Given | When | Then |
| --- | --- | --- | --- |
| M02-CA01 / CA-03 | Funcionário ativo sem conta | Cadastrar contato | Registro ativo com versão e nenhum usuário criado |
| M02-CA02 / RF-03 | Admin e funcionário ativo | Criar acesso e entrar | Conta vinculada, troca obrigatória; depois da troca exige novo login |
| M02-CA03 / CA-03 | Conta vinculada com sessão ativa | Desativar funcionário | Conta/sessões inativadas juntas; próxima chamada 401; registro preservado |
| M02-CA04 / CA-17 | Funcionário ligado a Admin e operador Administrador | Tentar desativar por HTTP direto | 403 e nenhum efeito |
| M02-CA05 / RN-18 | Dois requests com mesmo funcionário | Criar contas simultaneamente | Uma conta, um 201 e um 409, sem órfão |

## 14. Checklist de execução

- [ ] Implementar entidade/casos de uso, vínculo e controles de transação sem antecipar outros cadastros.
- [ ] Gerar/revisar evolução de persistência, testar do zero e upgrade 01→02, aplicar no banco local por tarefa explícita.
- [ ] Implementar endpoints, DTOs, páginas e clientes HTTP; conferir OpenAPI.
- [ ] Executar `dotnet restore`, `dotnet build --no-restore -c Release`, `dotnet test -c Release --no-build` com MongoDB isolado.
- [ ] Demonstrar M02-CA01–05 e registrar testes/arquivos/comandos reais no estado.

## 15. DoD e desbloqueio

DoD global satisfeita, RF-03 e RF-04 completos; vínculo/revogação/permissões concorrentes comprovados. Desbloquear **03**. Nenhum aceite depende de venda ou autoria comercial futura: a preservação é testada na auditoria já existente.
