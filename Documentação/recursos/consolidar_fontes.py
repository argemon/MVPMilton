"""Consolida artefatos públicos sem executar a aplicação nem ler configuração privada."""
from pathlib import Path
import re, json, hashlib, subprocess

BASE = Path(__file__).resolve().parents[1]
ROOT = BASE.parent
F = BASE / 'fontes'
E = BASE / 'evidencias'
E.mkdir(exist_ok=True)
SHA = subprocess.check_output(['git','-c','safe.directory='+ROOT.as_posix(),'rev-parse','HEAD'],cwd=ROOT,text=True).strip()

def read(p): return (ROOT / p).read_text(encoding='utf-8-sig')
def table(headers, rows):
    def cell(v): return str(v).replace('|','/').replace('\n','; ')
    return '\n'.join(['| '+' | '.join(headers)+' |','| '+' | '.join(['---']*len(headers))+' |']+['| '+' | '.join(map(cell,row))+' |' for row in rows])+'\n'
def first_table(path):
    lines=read(path).splitlines(); rows=[]; active=False
    for line in lines:
        if line.startswith('|'):
            if not active: active=True
            cols=[c.strip() for c in line.strip('|').split('|')]
            if not all(re.fullmatch(r'[- :]+',c) for c in cols): rows.append(cols)
        elif active: break
    return rows
def split_params(value):
    result=[]; current=''; depth=0
    for ch in value:
        if ch in '<([{': depth+=1
        if ch in '>)]}': depth-=1
        if ch==',' and depth==0: result.append(current.strip()); current=''
        else: current+=ch
    if current.strip(): result.append(current.strip())
    return result

text=(F/'documentacao-tecnica-base.md').read_text(encoding='utf-8')
for marker,path,title in [
 ('functional','docs/requirements/02-REQUISITOS-FUNCIONAIS.md','Requisitos funcionais'),
 ('nonfunctional','docs/requirements/03-REQUISITOS-NAO-FUNCIONAIS.md','Requisitos não funcionais'),
 ('rules','docs/requirements/04-REGRAS-DE-NEGOCIO.md','Regras de negócio'),
 ('acceptance','docs/requirements/06-CRITERIOS-ACEITE.md','Critérios de aceite')]:
    rows=first_table(path); parts=[f'## 2.{["functional","nonfunctional","rules","acceptance"].index(marker)+1} {title}']
    for row in rows[1:]:
        parts.append('### '+row[0].replace('`',''))
        parts.extend(f'**{key}:** {val}' for key,val in zip(rows[0][1:],row[1:]))
        if row[0]=='RNF-05': parts.append('**Situação documental:** meta histórica não medida nesta entrega; ensaio massivo excluído pelo escopo acadêmico ADR-37.')
        if row[0]=='RNF-06' or row[0].startswith('CA-16'): parts.append('**Situação documental:** obrigação operacional histórica; não comprovada e fora do gate acadêmico vigente. Restaurar carga não é restaurar backup completo.')
        if row[0].startswith('CA-18'): parts.append('**Situação documental:** separar o resultado de interface/build dos p95 antigos; não se declara medição de desempenho.')
    if marker=='functional': parts.append('### RF-ADR39 Limpeza explícita\n\nAdministrador pode limpar dados da instalação acadêmica mediante confirmação APAGAR DADOS e UUID. Preserva executor, vínculo e sessão, conforme UC29; não gera nova carga automaticamente.')
    if marker=='rules': parts.append('### RN-ADR39 Exceção delimitada\n\nSomente a limpeza explícita confirmada pode remover histórico e dados manuais conforme ADR-39. Fluxos normais e restauração continuam preservando histórico e dependências. UUID concluído não apaga dados posteriores.')
    if marker=='acceptance': parts.append('### CA-ADR39 Limpeza isolada\n\n401 anônimo,403 perfis negados,400 confirmação inválida sem efeito; sucesso preserva executor/vínculo/sessão e revoga demais. Falha de auditoria reverte exclusões. Gerar depois e repetir UUID antigo preserva a nova carga. Teste somente em fixtures isoladas, nunca no banco de apresentação.')
    text=text.replace('<!-- '+marker+' -->','\n\n'.join(parts))

cases=json.loads((F/'casos-de-uso.json').read_text(encoding='utf-8'))
uc=[]
for c in cases:
    c['codigo']=c['codigo'].replace('Infrastructure/Funcionarios/MongoFuncionarios.cs','Infrastructure/Identity/MongoFuncionarios.cs')
    for p in c['codigo'].split('; '): assert (ROOT/p).is_file(),p
    uc += ['## '+c['id']+' '+c['titulo'],f'**Atores:** {c["atores"]}. **Rastreio:** {c["rastreio"]}.',f'**Precondições:** {c["pre"]}', '\n'.join(f'{i}. {s}' for i,s in enumerate(c['fluxo'],1)), '**Alternativas e erros:** '+c['alternativas'],'**Pós-condição:** '+c['pos'],'**Implementação:** '+c['codigo']+'.']
(F/'casos-de-uso.json').write_text(json.dumps(cases,ensure_ascii=False,indent=2),encoding='utf-8')
text=text.replace('<!-- usecases -->','\n\n'.join(uc))

mapping=[
('RF-01','UC01','Acesso/EndpointsAcesso; Identity/MongoAutenticacao','AuthenticationTests'),
('RF-02','UC02/03','Identity/MongoAutenticacao; Security/ContasHttp','AuthenticationTests'),
('RF-03','UC04','EndpointsFuncionarios; Identity/MongoFuncionarios','AdministrationTests'),
('RF-04','UC05','Identity/MongoFuncionarios','EmployeeTests'),
('RF-05','UC06','Clientes/MongoClientes','CustomerTests'),
('RF-06','UC07','Clientes/MongoClientes; Vendas/MongoVendas','SalesTests'),
('RF-07','UC08/09','Produtos/MongoProdutos','ProductTests'),
('RF-08','UC10/11/12','Produtos/MongoProdutos; DadosProduto','ProductTests'),
('RF-09','UC15','Estoque/MongoEstoque; Movimentacao','StockTests'),
('RF-10','UC16','Estoque/MongoEstoque; Movimentacao','StockTests'),
('RF-11','UC17','Estoque/MongoEstoque; Movimentacao','StockTests'),
('RF-12','UC13/14','Estoque/MongoEstoque','StockQueryTests'),
('RF-13','UC18/21','Vendas/MongoVendas; RegrasVenda','SalesTests'),
('RF-14','UC19','Vendas/MongoVendas; OperacoesComerciais','SalesTests'),
('RF-15','UC20','Vendas/MongoVendas','SalesTests'),
('RF-16','UC22','Vendas/MongoVendas; MongoEstoque','SalesTests'),
('RF-17','UC23','Dashboard/MongoDashboard; Indicadores','DashboardTests'),
('RF-18','UC23','Dashboard/MongoDashboard','DashboardTests'),
('RF-19','UC24/26','Relatorios/MongoRelatorios; EndpointsRelatorios','ReportsTests'),
('RF-20','UC25/26','Relatorios/MongoRelatorios','ReportsTests'),
('RF-ADR39','UC29','Infrastructure/DadosDemonstracao','DemonstrationCleanupTests'),
('M09 / ADR-37','UC27/28','Infrastructure/DadosDemonstracao','DemonstrationTests')]
for r in mapping: assert (ROOT/'tests/GestaoComercial.IntegrationTests'/f'{r[3]}.cs').exists(),r[3]
text=text.replace('<!-- traceability -->',table(['Requisito / casos','Implementação resumida','Teste IntegrationTests'],[(a+' / '+b,c,d+'.cs') for a,b,c,d in mapping]))
versions=[('SDK .NET','10.0.401')]+re.findall(r'PackageVersion Include="([^"]+)" Version="([^"]+)"',read('Directory.Packages.props'))
text=text.replace('<!-- versions -->',table(['Componente','Versão declarada'],versions))

COMMON=[('_id','binData UUID','Identificador Standard'),('schemaVersion','Int32 = 1','Formato do documento'),('nome / nomeNormalizado','string 2–150','Exibição e chave de busca'),('ativo','bool','Inativação lógica'),('versao','Int64 >= 1','Concorrência de cadastro'),('guardVersion','Int64 >= 0','Coordenação de referências'),('createdAt / updatedAt','Date UTC','Criação e alteração')]
collections=[]
def coll(name,source,fields,note=''): collections.append((name,source,fields,note))
coll('employees','Funcionarios/FuncionariosSchema.cs',COMMON+[('email','string 3–254 ou null','Contato opcional'),('telefone','string 10–15 dígitos ou null','Contato opcional')])
coll('users','Identity/AuthSchema.cs',COMMON+[
 ('email / emailNormalizado','string 3–254','Login e unicidade'),('passwordHash','string 1–1024','Hash Identity, nunca retornar'),('perfil','string enum','Administrador, Vendedor ou Estoquista'),('deveAlterarSenha','bool','Troca obrigatória'),('securityStamp','string 32–64','Revogação'),('failedAccessCount','Int32 0–5','Falhas de login'),('lockoutEnd','Date UTC ou null','Bloqueio'),('employeeId','UUID ou null','Vínculo único; bootstrap pode ser null')])
coll('customers','Clientes/ClientesSchema.cs',COMMON+[
 ('tipo','string PF/PJ','Tipo de pessoa'),('documento','string ou null','CPF11/CNPJ14 válido e normalizado'),('email','string ou null','Contato'),('telefone','string ou null','10–15 dígitos')])
for n in ['categories','brands']:
    coll(n,'Produtos/ProdutosSchema.cs',[(a,b.replace('2–150','2–80'),c) for a,b,c in COMMON], 'Nomes normalizados únicos por coleção, inclusive inativos.')
coll('products','Produtos/ProdutosSchema.cs',COMMON+[
 ('sku / skuNormalizado','string 1–40','SKU normalizado único; padrão A–Z/0–9/._-'),('descricao','string até 2000 ou null','Sem HTML'),('categoriaId','UUID','Categoria obrigatória'),('marcaId','UUID ou null','Marca opcional'),('precoCentavos','Int64 1–999.999.999','Preço BRL'),('moeda','string BRL','Moeda fixa'),('estoqueMinimo','Int32 0–1.000.000','Referência de reposição'),('saldo.quantidade','Int32 >= 0','Saldo incorporado obrigatório'),('saldo.versao','Int64 >= 1','Versão do saldo'),('imagem','object ou null','Metadados de arquivo privado'),('imagem.chave','string','32 caracteres hexadecimais e .png'),('imagem.mime','string image/png','Arquivo convertido'),('imagem.tamanhoBytes','Int32 1–2.097.152','Tamanho do PNG')])
coll('sales','Vendas/VendasSchema.cs',[
 ('_id / clienteId / criadorId','UUID','Venda e referências'),('schemaVersion','Int32 = 1','Formato'),('criadorNomeSnapshot','string 2–150','Nome do criador na época'),('status','enum string','Rascunho, Confirmada, Cancelada, Descartada'),('itens','array até 100','Incorporado; produto e ID distintos'),('itens.id / itens.produtoId','UUID','Item estável e produto'),('itens.skuSnapshot / nomeSnapshot','string 1–40 / 2–150','Cópia histórica'),('itens.quantidade','Int32 1–1.000.000','Quantidade UN'),('itens.precoUnitarioCentavos','Int64 1–999.999.999','Preço copiado'),('itens.totalCentavos','Int64 positivo limitado','Quantidade × preço'),('totalCentavos','Int64 0–99.999.999.999.999','Soma exata de itens'),('moeda','string BRL','Moeda fixa'),('versao / guardVersion','Int64 >= 1 / >= 0','Edição e coordenação'),('createdAt / updatedAt / criadoEm','Date UTC','createdAt igual criadoEm'),('confirmadoEm / canceladoEm / descartadoEm','Date UTC ou null','Conforme estado'),('clienteSnapshot','object ou null','nome e documento nullable; confirmação'),('confirmacao / cancelamento / descarte','object ou null','atorId, nomeSnapshot e motivo nullable'),('motivo nos eventos','string 10–500 ou null','Null na confirmação; obrigatório no encerramento')], 'Confirmada/Cancelada exigem itens e confirmação; Cancelada também estorno/evento. Não contém ativo nem nomeNormalizado.')
coll('stock_movements','Estoque/EstoqueSchema.cs e Vendas/VendasSchema.cs',[
 ('_id / produtoId / atorId','UUID','Identidade e referências'),('schemaVersion','Int32 = 1','Formato'),('tipo','string enum','Entrada, SaidaManual, Ajuste, Venda, EstornoVenda'),('delta','Int32 não zero ±1.000.000','Sinal conforme tipo'),('saldoAntes / saldoDepois','Int32 >= 0','Depois = antes + delta'),('motivo','string 10–500','Justificativa'),('atorNomeSnapshot','string 2–150','Autoria histórica'),('ocorridoEm','Date UTC','Data do movimento'),('operacaoId','UUID no manual; null/ausente comercial','Identifica intenção manual'),('vendaId / itemId','UUID no comercial; null/ausente manual','Baixa/estorno por item')], 'Imutável no fluxo normal. A definição é expandida pela evolução06 para incluir vínculos comerciais.')
coll('idempotency_receipts','Estoque/EstoqueSchema.cs e Vendas/VendasSchema.cs',[
 ('_id / atorId / chave / recursoId','UUID','Recibo, ator, intenção e recurso'),('schemaVersion','Int32','Formato'),('escopo','string 1–100','Identificador interno do comando'),('hashRequest','binData 32 bytes','Hash da intenção normalizada'),('estado','string enum','Preparando ou Concluido'),('statusHttp','Int32 enum 200/201','Sucesso do comando'),('resposta','object','Vazio na preparação invisível; documento do resultado na conclusão'),('criadoEm','Date UTC','Registro da intenção')], 'Unique ator/escopo/chave; sem TTL comercial. Schema físico exige campos, inclusive durante preparação.')
coll('audit_events','Identity/AuthSchema.cs',[
 ('_id','UUID','Evento'),('schemaVersion','enum 1','Formato'),('atorId / recursoId','UUID ou null','Anonimato/recurso conforme evento'),('origem','string enum','Autenticado, Anonimo ou Bootstrap'),('traceId','string 1–150','Correlação e proveniência demonstrativa'),('acao','string 1–50','Ação registrada'),('recursoTipo','string 1–30','Tipo de recurso'),('ocorridoEm','Date UTC','Instante')], 'Schema atual não define um payload genérico detalhes. Proveniência da carga e recibos DemoLimpa usam campos existentes.')
coll('api_sessions','Identity/AuthSchema.cs',[
 ('_id / usuarioId','UUID','Sessão e conta'),('schemaVersion','enum 1','Formato'),('tokenHash','binData','SHA-256 do bearer, não token bruto'),('securityStamp','string 32–64','Stamp da conta'),('criadoEm / expiraEm / ultimoUsoEm','Date UTC','Vida da sessão'),('revogadaEm','Date UTC ou null','Revogação')], 'Expiração verificada em código; índice expiraEm não é declaração de TTL configurado.')
coll('administration_control','Identity/AuthSchema.cs',[
 ('_id','string administration','Documento técnico fixo'),('guardVersion','Int64 >= 0','Escrita de coordenação administrativa')], 'Exceção à convenção genérica: schema físico não possui schemaVersion.')
coll('tickets','frontend/GestaoComercial.Web/Security/MongoTickets.cs',[
 ('_id','UUID','Ticket técnico'),('schemaVersion','enum 1','Formato'),('handleHash','binData','Hash SHA-256 da referência'),('ticketProtegido','binData','Ticket Data Protection'),('tokenApiProtegido','binData','Token protegido'),('expiraEm / createdAt','Date UTC','Validade e criação')], 'Mesmo banco, finalidade técnica Web. Sem conteúdo comercial ou token bruto.')
coll('persistence_versions','Persistence/MongoFoundation.cs',[
 ('_id','string','ID da evolução'),('schemaVersion','Int32 = 1','Formato do registro'),('version','Int32','Posição aplicada'),('checksum','string','Conferência da definição'),('appliedAt','Date UTC','Aplicação explícita')])
dictionary=[]
for i,(name,source,fields,note) in enumerate(collections,1):
    path=source if source.startswith('frontend/') else 'backend/GestaoComercial.Infrastructure/'+source.split(' e ')[0]
    assert (ROOT/path).is_file(),path
    dictionary += [f'### Coleção {name}',(f'Fonte de definição: {source}. '+note).rstrip(),table(['Campo','Tipo BSON e limite','Finalidade'],fields)]
text=text.replace('<!-- dictionary -->','\n\n'.join(dictionary))

indexes=[
 ('employees','ix_employees_nome_id','nomeNormalizado, _id'),('users','ux_users_email / ix_users_nome_id','emailNormalizado único; nomeNormalizado,_id'),('users','ux_users_employee','employeeId único parcial binData'),('customers','ux_customers_documento / ix_customers_nome_id','documento único parcial string; nomeNormalizado,_id'),('categories / brands','ux_*_nome / ix_*_nome_id','nomeNormalizado único; nomeNormalizado,_id'),('products','ux_products_sku / ix_products_nome_id','skuNormalizado único; nomeNormalizado,_id'),('products','ix_products_categoria_ativo_id / ix_products_marca_ativo_id','referência, ativo, _id'),('sales','ix_sales_criado_id','criadoEm desc, _id desc'),('sales','ix_sales_cliente_criado_id / ix_sales_criador_criado_id','cliente/criador, criadoEm desc, _id desc'),('sales','ix_sales_confirmado_status_id','confirmadoEm, status, _id'),('stock_movements','ix_stock_data_id / ix_stock_produto_data_id','ocorridoEm desc,_id desc; produtoId + data/id'),('stock_movements','ux_stock_operacao','operacaoId único parcial UUID'),('stock_movements','ux_stock_venda_item_tipo','vendaId,itemId,tipo único parcial UUID'),('idempotency_receipts','ux_receipts_ator_escopo_chave / ix_receipts_data','atorId,escopo,chave único; criadoEm'),('api_sessions','ux_sessions_token / ix_sessions_usuario_expira / ix_sessions_expira','tokenHash único; usuarioId,expiraEm; expiraEm'),('audit_events','ix_audit_recurso_data_id','recursoTipo,recursoId,ocorridoEm,_id'),('tickets','ux_tickets_handle / ix_tickets_expira','handleHash único; expiraEm')]
indexes.append(('persistence_versions','ux_persistence_version','version único; _id possui unicidade nativa'))
# Garantia de que nomes literais, fora dos padrões de geração, existem no código.
schema_text='\n'.join(p.read_text(encoding='utf-8-sig') for folder in [ROOT/'backend',ROOT/'frontend'] for p in folder.rglob('*.cs') if 'obj' not in p.parts and 'bin' not in p.parts)
for _,names,_ in indexes:
    for name in names.split(' / '):
        if '*' not in name: assert name in schema_text,name
text=text.replace('<!-- indexes -->',table(['Coleção','Índice','Chaves e observação'],indexes))

routes=[]
def route(method,path,body,policy): routes.append((method+' '+path,body,policy))
for meth,path,body,pol in [
 ('POST','/auth/login','LoginInput; 200 token interno e usuário','Anônimo; rate limit'),('GET','/auth/me','200 SessaoResponse','Conta'),('POST','/auth/logout','204','Conta'),('POST','/auth/alterar-senha','PasswordInput;204','Conta'),('GET','/usuarios','200 UsuariosResponse; ativo bool?','Usuarios.Gerenciar'),('GET','/usuarios/{id}','200 UsuarioResponse','Usuarios.Gerenciar'),('POST','/usuarios','CriarContaInput privado;201 UsuarioResponse','Usuarios.Gerenciar'),('PUT','/usuarios/{id}','AtualizarUsuarioRequest;200','Usuarios.Gerenciar'),('POST','/usuarios/{id}/redefinir-senha','ResetInput privado;204','Usuarios.Gerenciar')]: route(meth,path,body,pol)
for path,singular,plural,policy in [('funcionarios','Funcionario','Funcionarios','Funcionarios.Gerenciar'),('clientes','Cliente','Clientes','Clientes.Gerenciar'),('categorias','Referencia','Referencias','Produtos'),('marcas','Referencia','Referencias','Produtos'),('produtos','Produto','Produtos','Produtos')]:
    readpolicy=policy+'.Ler' if policy=='Produtos' else policy
    writepolicy=policy+'.Gerenciar' if policy=='Produtos' else policy
    route('GET','/'+path,'200 '+plural+'Response',readpolicy)
    route('GET','/'+path+'/{id}','200 '+singular+'Response',readpolicy)
    route('POST','/'+path,'Criar'+singular+'Request;201 '+singular+'Response',writepolicy)
    route('PUT','/'+path+'/{id}','Atualizar'+singular+'Request;200 '+singular+'Response',writepolicy)
route('GET','/clientes/{id}/vendas','200 HistoricoClienteResponse','Clientes.Gerenciar + propriedade')
for m,b in [('GET','200 bytes PNG'),('POST','multipart arquivo e versao;200 ProdutoResponse'),('DELETE','query versao;204')]: route(m,'/produtos/{id}/imagem',b,'Produtos.Ler' if m=='GET' else 'Produtos.Gerenciar')
route('GET','/estoque/saldos','200 SaldosResponse','Estoque.Ler')
route('GET','/estoque/movimentos','200 MovimentosResponse','Estoque.Historico')
route('GET','/estoque/movimentos/{id}','200 MovimentoResponse','Estoque.Historico')
for p in ['entradas','saidas','ajustes']: route('POST','/estoque/'+p,('AjusteRequest' if p=='ajustes' else 'MovimentoRequest')+'; chave;201 MovimentoResponse','Estoque.Ajustar' if p=='ajustes' else 'Estoque.Movimentar')
route('GET','/vendas','200 VendasResponse','Vendas.Ler + propriedade')
route('GET','/vendas/{id}','200 VendaResponse','Vendas.Ler + propriedade')
route('POST','/vendas','CriarVendaRequest; chave;201 VendaResponse','Vendas.Criar')
route('PUT','/vendas/{id}','AtualizarVendaRequest;200 VendaResponse','Vendas.Editar + propriedade')
for p in ['confirmacao','cancelamento','descarte']: route('POST','/vendas/{id}/'+p,('ConfirmarVendaRequest' if p=='confirmacao' else 'EncerrarVendaRequest')+'; chave;200 VendaResponse','Vendas.Cancelar' if p=='cancelamento' else 'Vendas.Confirmar' if p=='confirmacao' else 'Vendas.Editar')
route('GET','/dashboard','200 DashboardResponse','Dashboard.Ler')
for p in ['vendas','operadores','produtos','estoque','movimentos']:
    pol='Relatorios.'+({'estoque':'Estoque','movimentos':'Movimentos'}.get(p,'Vendas'))
    route('GET','/relatorios/'+p,'200 Relatorio'+p.capitalize()+'Response',pol)
    route('GET','/relatorios/'+p+'/exportacao','200 CSV UTF-8',pol)
for m,p,b in [('GET','','200 EstadoCargaResponse'),('POST','/gerar','200 CargaDemonstracaoResponse'),('POST','/restaurar','RestaurarCargaRequest;200 CargaDemonstracaoResponse'),('POST','/limpar','LimparDadosRequest;204')]: route(m,'/administracao/dados-demonstracao'+p,b,'Funcionarios.Gerenciar')
text=text.replace('<!-- endpoints -->','## 9.2 Catálogo de rotas API\n\nCaminhos relativos a /api/v1; {id} exige UUID. Chave indica Idempotency-Key; limpeza usa OperacaoId no corpo. POST/PUT de cadastros não recebem chave comercial.\n\n'+table(['Método e caminho','Entrada e sucesso','Policy'],routes)+'\n\nRotas Web de conta: POST /conta/entrar, /conta/sair e /conta/senha, com formulário e antiforgery; criação/redefinição de conta também atravessam formulários próprios. GET /produtos/{id}/imagem transmite imagem autenticada e downloads /downloads/relatorios/{tipo} passam pelo BFF. Rotas técnicas /health/live e /health/ready estão nos hosts; não integram API comercial autenticada.')

dto=[]
for p in sorted((ROOT/'shared/GestaoComercial.Contracts').glob('*.cs')):
    content=p.read_text(encoding='utf-8-sig')
    for m in re.finditer(r'public\s+(?:(?:sealed|readonly)\s+)?record\s+(\w+)\s*\(',content):
        pos=m.end();start=pos;depth=1
        while depth and pos<len(content):
            if content[pos]=='(':depth+=1
            if content[pos]==')':depth-=1
            pos+=1
        fields=split_params(content[start:pos-1])
        dto.append({'tipo':m.group(1),'arquivo':p.relative_to(ROOT).as_posix(),'campos':fields,'linha':content[:m.start()].count('\n')+1})
dto_parts=['## 9.5 Inventário dos contratos compartilhados','Propriedades listadas com nome C# e tipo; no JSON, nomes camelCase. ? indica nulabilidade declarada; não prova que o caso de uso aceita null. IReadOnlyList indica coleção. Requests são entradas e Response/resultados são saídas; não contêm documentos de persistência. Credenciais de login/senha são tipos privados dos hosts, fora de Contracts.']
for filename in sorted({d['arquivo'] for d in dto}):
    dto_parts.append('### '+Path(filename).name)
    dto_parts.append(table(['Tipo','Campos e tipos C#'],[(d['tipo'],'; '.join(d['campos'])) for d in dto if d['arquivo']==filename]))
text=text.replace('<!-- dto -->','\n\n'.join(dto_parts))
(E/'contratos.json').write_text(json.dumps(dto,ensure_ascii=False,indent=2),encoding='utf-8')

tests=[]
for p in sorted((ROOT/'tests').rglob('*.cs')):
    if 'obj' in p.parts or 'bin' in p.parts: continue
    content=p.read_text(encoding='utf-8-sig')
    for m in re.finditer(r'\[(Fact|Theory)(?:\([^\]]*\))?\]',content):
        method=re.search(r'public\s+(?:async\s+)?(?:Task(?:<[^>]+>)?|void)\s+(\w+)\s*\(',content[m.end():m.end()+1700])
        if method: tests.append({'arquivo':p.relative_to(ROOT).as_posix(),'metodo':method.group(1),'categoria':m.group(1),'linha':content[:m.end()+method.start()].count('\n')+1})
critical=[('Venda com snapshots e estorno único','FullLifecycleUsesExactServerPricesSnapshotsAndOneRestock'),('Insuficiência e falha de auditoria','MultipleItemsInsufficientOrAuditFailureRollBackAllDocuments'),('Última unidade entre sessões','TwoSalesCompeteForLastUnitWithIndependentSessions'),('Idempotência e corpo divergente','CreateAndConfirmIdempotencyAreConcurrentAndBoundToRequest'),('Propriedade antes da paginação','SellerOwnershipAppliesBeforePaginationCountsAndClientHistory'),('Cancelamento com overflow e rollback','CancellationOverflowAndAuditFailurePreserveConfirmedSaleUntilSuccessfulRetry'),('Conciliação de estoque','EntriesExitsAdjustmentsReconcileAndHistoryIsImmutable'),('Cinco relatórios e CSV','AllFiveReportsUseSnapshotsFiltersTotalsDeterministicPagesAndSafeCsv'),('Limpeza e nova carga','CleanupPreservesActorAndEmployeeRevokesOthersRollsBackAndAllowsFreshLoad')]
criticalrows=[]
for label,method in critical:
    found=next(t for t in tests if t['metodo']==method)
    criticalrows.append((label,Path(found['arquivo']).stem+'.'+method,str(found['linha'])))
text=text.replace('<!-- criticaltests -->',table(['Cenário','Método existente','Linha'],criticalrows))
testparts=[]
for file in sorted({t['arquivo'] for t in tests}):
    testparts.append('### '+Path(file).stem)
    testparts.append('Localização: '+file+'.')
    testparts.append(table(['Método','Tipo / linha'],[(t['metodo'],t['categoria']+' / '+str(t['linha'])) for t in tests if t['arquivo']==file]))
text=text.replace('<!-- testinventory -->','\n\n'.join(testparts))
(E/'testes-inventario.json').write_text(json.dumps(tests,ensure_ascii=False,indent=2),encoding='utf-8')

diagrams=json.loads((BASE/'diagramas/indice.json').read_text(encoding='utf-8'))
diaparts=[]
for n,d in diagrams.items():
    diaparts+=['## '+d['titulo'],f'![Figura - {d["titulo"]}](../diagramas/{n}.png)','Fonte: elaboração própria a partir de '+d['fonte_codigo']+'.']
text=text.replace('<!-- diagrams -->','\n\n'.join(diaparts))
text=text.replace('## 9.2 Exemplos','## 9.3 Exemplos').replace('## 9.3 Erros','## 9.4 Erros')
(F/'documentacao-tecnica.md').write_text(text,encoding='utf-8')

manifest=[]
for folder in ['backend','frontend','shared','tests','docs','.github','build','scripts']:
    for p in sorted((ROOT/folder).rglob('*')):
        if not p.is_file() or any(x in p.parts for x in ['obj','bin','node_modules','.local']):continue
        if p.suffix.lower() not in ['.cs','.csproj','.razor','.md','.json','.yml','.yaml','.ps1','.css','.js']:continue
        if p.name.startswith('.env'):continue
        data=p.read_bytes();manifest.append({'arquivo':p.relative_to(ROOT).as_posix(),'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest()})
for name in ['global.json','Directory.Packages.props','GestaoComercial.slnx','README.md','AGENTS.md']:
    p=ROOT/name;manifest.append({'arquivo':name,'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
(E/'inventario-fontes.json').write_text(json.dumps({'commit':SHA,'data':'2026-11-24','natureza':'inspeção estática; sem execução da aplicação','arquivos':manifest},ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'commit':SHA,'casos_uso':len(cases),'rotas_api':len(routes),'contratos':len(dto),'metodos_teste':len(tests),'colecoes':len(collections),'fontes_publicas':len(manifest)},ensure_ascii=False))
