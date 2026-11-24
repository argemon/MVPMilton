"""Fontes UML da versão documentada. Execução sem acesso à aplicação ou ao banco."""
from pathlib import Path
import json
import re
import textwrap

BASE = Path(__file__).resolve().parents[1]
OUT = BASE / 'diagramas'
OUT.mkdir(exist_ok=True)
DIAGRAMS = {}

def add(name, title, body, source):
    DIAGRAMS[name] = {'titulo': title, 'fonte_codigo': source}
    body = re.sub(r'"[^"\\]*(?:\\.[^"\\]*)*"', lambda m: m.group(0).replace('\n', r'\n'), body)
    body = re.sub(r'^(class .+?)\{ ([^{}\n]+) \}', r'\1{\n \2\n}', body, flags=re.M)
    body = body.replace('\n/ ', r'\n/ ')
    body = body.replace('endif (não)', 'else (não)\nendif')
    def wrap_label(value,width):
        return r'\n'.join(piece for part in value.split(r'\n') for piece in textwrap.wrap(part,width=width,break_long_words=False,break_on_hyphens=False))
    body=re.sub(r'"[^"\n]*"',lambda m:'"'+wrap_label(m.group(0)[1:-1],26)+'"',body)
    lines=[];note=False
    for line in body.splitlines():
        if line.startswith('note '):note=True;lines.append(line);continue
        if line=='end note':note=False;lines.append(line);continue
        if note:
            lines.extend(textwrap.wrap(line,42,break_long_words=False,break_on_hyphens=False) or ['']);continue
        if re.search(r'[-=]+>.* : ',line):
            left,right=line.split(' : ',1);line=left+' : '+wrap_label(right,30)
        if line.lstrip().startswith(':'):
            indent=line[:len(line)-len(line.lstrip())];value=line.strip()[1:]
            if value.endswith(';'):line=indent+':'+wrap_label(value[:-1],34)+';'
        lines.append(line)
    body='\n'.join(lines)
    if name=='08-classes-conceituais-vendas':body+='\nItemVenda -[hidden]down-> MovimentoEstoque\n'
    if name=='09-classes-implementadas-vendas':
        body=body.replace('note bottom\nMétodos','note bottom of MongoEstoque\nMétodos')
        body+='\nRegrasVenda -[hidden]down-> OperacoesComerciais\nOperacoesComerciais -[hidden]down-> MongoEstoque\n'
    if name=='10-classes-implementadas-estoque':
        body=body.replace('note bottom of DadosProduto','note bottom of EstoqueSchema')
        body+='\nMovimentacao -[hidden]down-> OperacoesCadastro\nOperacoesCadastro -[hidden]down-> OperacoesComerciais\nOperacoesComerciais -[hidden]down-> EstoqueSchema\n'
    if name=='14-modelo-documental-seguranca':body+='\nidempotency_receipts -[hidden]down-> tickets\ntickets -[hidden]down-> administration_control\nadministration_control -[hidden]down-> persistence_versions\n'
    text = '@startuml\n!pragma layout smetana\nskinparam backgroundColor white\nskinparam defaultFontName Arial\nskinparam defaultFontSize 17\nskinparam maxMessageSize 200\nskinparam shadowing false\nskinparam roundcorner 0\nskinparam monochrome true\nhide empty members\n' + body.strip() + '\n@enduml\n'
    (OUT / (name + '.puml')).write_text(text, encoding='utf-8')

add('01-casos-visao-geral','Casos de uso gerais', '''
left to right direction
actor Administrador as A
actor Vendedor as V
actor Estoquista as E
rectangle "Gestão Comercial" {
 usecase "Acessar conta" as U1
 usecase "Administrar equipe\ne contas" as U2
 usecase "Manter clientes" as U3
 usecase "Consultar produtos\ne saldo" as U4
 usecase "Manter produtos\ne referências" as U5
 usecase "Movimentar estoque" as U6
 usecase "Operar vendas" as U7
 usecase "Analisar indicadores\ne relatórios comerciais" as U8
 usecase "Relatórios de\nestoque e movimentos" as U9
 usecase "Administrar demonstração" as U10
}
A --> U1
V --> U1
E --> U1
A --> U2
A --> U3
V --> U3
A --> U4
V --> U4
E --> U4
A --> U5
E --> U5
A --> U6
E --> U6
A --> U7
V --> U7
A --> U8
A --> U9
E --> U9
A --> U10
note bottom of U7
Vendedor: somente vendas próprias.
Cancelamento: somente Administrador.
end note
''','docs/requirements/05-MATRIZ-PERMISSOES.md; backend/GestaoComercial.Api/Acesso')

def usecases(name,title,actors,cases,links,note=''):
    lines=['left to right direction']
    for key,value in actors.items(): lines.append(f'actor "{value}" as {key}')
    lines.append('rectangle "Gestão Comercial" {')
    for key,value in cases.items(): lines.append(f'usecase "{value}" as {key}')
    lines.append('}')
    for actor,items in links.items():
        for item in items: lines.append(f'{actor} --> {item}')
    if note: lines.extend(['note bottom',note,'end note'])
    add(name,title,'\n'.join(lines),'backend/GestaoComercial.Api/Acesso; docs/requirements/05-MATRIZ-PERMISSOES.md')

usecases('02-casos-acesso-equipe','Casos de uso de acesso e equipe',{'A':'Administrador','O':'Operador autenticado'},
 {'U1':'UC01 Entrar','U2':'UC02 Sair','U3':'UC03 Alterar senha','U4':'UC04 Manter contas','U5':'UC05 Manter funcionários'},
 {'O':['U1','U2','U3'],'A':['U1','U2','U3','U4','U5']},'Operador representa os três perfis.\nEntrar é iniciado antes de haver sessão.\nNenhuma conta é criada publicamente.')
usecases('03-casos-cadastros','Casos de uso de cadastros',{'A':'Administrador','V':'Vendedor','E':'Estoquista'},
 {'U6':'UC06 Manter clientes','U7':'UC07 Consultar histórico\ndo cliente','U8':'UC08 Manter categorias','U9':'UC09 Manter marcas','U10':'UC10 Manter produtos','U11':'UC11 Gerenciar imagem','U12':'UC12 Consultar produtos'},
 {'A':['U6','U7','U8','U9','U10','U11','U12'],'V':['U6','U7','U12'],'E':['U8','U9','U10','U11','U12']},'Histórico para Vendedor contém apenas suas vendas.\nInativação preserva registros e referências.')
usecases('04-casos-estoque','Casos de uso de estoque',{'A':'Administrador','V':'Vendedor','E':'Estoquista'},
 {'U13':'UC13 Consultar saldos','U14':'UC14 Consultar movimentos','U15':'UC15 Registrar entrada','U16':'UC16 Registrar saída manual','U17':'UC17 Ajustar por delta'},
 {'A':['U13','U14','U15','U16','U17'],'V':['U13'],'E':['U13','U14','U15','U16']})
usecases('05-casos-vendas','Casos de uso de vendas',{'A':'Administrador','V':'Vendedor'},
 {'U18':'UC18 Criar e editar rascunho','U19':'UC19 Confirmar venda','U20':'UC20 Consultar venda','U21':'UC21 Descartar rascunho','U22':'UC22 Cancelar venda'},
 {'A':['U18','U19','U20','U21','U22'],'V':['U18','U19','U20','U21']},'Vendedor atua somente nas próprias vendas.\nRascunho não reserva saldo.\nCancelamento estorna integralmente.')
usecases('06-casos-analise-demonstracao','Casos de uso de análise e demonstração',{'A':'Administrador','E':'Estoquista'},
 {'U23':'UC23 Consultar dashboard','U24':'UC24 Relatórios comerciais','U25':'UC25 Relatórios de estoque\ne movimentos','U26':'UC26 Exportar CSV','U27':'UC27 Gerar ou retomar carga','U28':'UC28 Restaurar carga','U29':'UC29 Limpar demonstração'},
 {'A':['U23','U24','U25','U26','U27','U28','U29'],'E':['U25','U26']},'CSV respeita o perfil do relatório.\nRestaurar protege dependências manuais.\nLimpar exige APAGAR DADOS e preserva executor.')

add('07-classes-conceituais-cadastros','Classes conceituais de cadastros', '''
class Funcionario {
 id: UUID
 nome: texto
 ativo: booleano
}
class Usuario {
 id: UUID
 perfil: Perfil
 ativo: booleano
}
enum Perfil {
 Administrador
 Vendedor
 Estoquista
}
class Cliente {
 id: UUID
 tipo: PF ou PJ
 documento: texto opcional
}
class Categoria { nome: texto }
class Marca { nome: texto }
class Produto {
 sku: texto
 precoCentavos: inteiro longo
 ativo: booleano
}
class SaldoEstoque { quantidade: inteiro }
Funcionario "0..1" -- "0..1" Usuario : vínculo único
Usuario --> Perfil
Categoria "1" -- "0..*" Produto
Marca "0..1" -- "0..*" Produto
Produto "1" *-- "1" SaldoEstoque
note bottom of Usuario
Conceitos de negócio, não classes C#.
Bootstrap pode não ter funcionário.
end note
''','docs/architecture/04-MODELO-DE-DOMINIO.md; schemas de cadastros')
add('08-classes-conceituais-vendas','Classes conceituais de vendas e estoque', '''
class Cliente { id: UUID }
class Usuario { id: UUID }
class Venda {
 status: EstadoVenda
 totalCentavos: inteiro longo
 versao: inteiro longo
}
class ItemVenda {
 quantidade: inteiro
 precoUnitarioCentavos: inteiro longo
 nomeSnapshot: texto
}
class Produto { id: UUID }
class MovimentoEstoque {
 delta: inteiro
 saldoAntes: inteiro
 saldoDepois: inteiro
 tipo: texto
}
Cliente "1" -- "0..*" Venda
Usuario "1" -- "0..*" Venda : criador
Venda "1" *-- "0..100" ItemVenda
Produto "1" -- "0..*" ItemVenda
Produto "1" -- "0..*" MovimentoEstoque
Venda "0..1" -- "0..*" MovimentoEstoque
Usuario "1" -- "0..*" MovimentoEstoque : ator
note bottom of Venda
Confirmada ou Cancelada: 1 a 100 itens.
Rascunho pode estar vazio.
Conceitos não impõem coleção por classe.
end note
''','IVendas.cs; VendasSchema.cs; EstoqueSchema.cs')
add('09-classes-implementadas-vendas','Classes e interfaces implementadas em vendas', '''
interface IVendas {
 ListarAsync(...): Task<PaginaVendas>
 ConsultarAsync(...): Task<Venda>
 SalvarAsync(...): Task<Venda>
 TransicionarAsync(...): Task<Venda>
}
class MongoVendas {
 SalvarAsync(...)
 TransicionarAsync(...)
}
class RegrasVenda <<static>> {
 Total(itens): long
 Itens(itens): void
 Transicao(atual, acao, itens): string
}
class Venda <<record Application>> {
 Id: Guid
 Status: string
 Itens: IReadOnlyList<ItemVenda>
 TotalCentavos: long
 Versao: long
}
class ItemVenda <<record Application>> {
 ProdutoId: Guid
 Quantidade: int
 PrecoUnitarioCentavos: long
}
class OperacoesComerciais
class MongoEstoque
class EndpointsVendas <<static Api>>
IVendas <|.. MongoVendas
IVendas ..> Venda
Venda *-- ItemVenda
MongoVendas ..> RegrasVenda
MongoVendas ..> MongoEstoque
MongoVendas ..> OperacoesComerciais
EndpointsVendas ..> IVendas
note bottom
Métodos abreviados por legibilidade.
Tipos de resultado não são entidades Domain.
end note
''','backend/GestaoComercial.Application/Vendas/IVendas.cs; backend/GestaoComercial.Infrastructure/Vendas/MongoVendas.cs')
add('10-classes-implementadas-estoque','Classes e interfaces implementadas em estoque', '''
interface IEstoque {
 SaldosAsync(...)
 HistoricoAsync(...)
 ConsultarAsync(...)
 MovimentarAsync(...)
}
class MongoEstoque
class Movimentacao <<static Domain>> {
 Delta(tipo, quantidade): int
 Motivo(motivo): string
 SaldoFinal(saldo, delta): int
}
class Movimento <<record Application>> {
 Id: Guid
 ProdutoId: Guid
 Delta: int
 SaldoAntes: int
 SaldoDepois: int
}
class DadosProduto <<record Domain>> {
 Sku: string
 PrecoCentavos: long
 Criar(...): DadosProduto
}
class OperacoesCadastro
class OperacoesComerciais
class EstoqueSchema
IEstoque <|.. MongoEstoque
IEstoque ..> Movimento
MongoEstoque ..> Movimentacao
MongoEstoque ..> OperacoesCadastro
MongoEstoque ..> OperacoesComerciais
MongoEstoque ..> EstoqueSchema
note bottom of DadosProduto
Recorte de tipos reais.
DadosProduto valida cadastro;
não representa um documento BSON.
end note
''','backend/GestaoComercial.Application/Estoque/IEstoque.cs; backend/GestaoComercial.Infrastructure/Estoque/MongoEstoque.cs')
add('11-pacotes','Dependências entre projetos', '''
package "GestaoComercial.Api" as Api
package "GestaoComercial.Application" as App
package "GestaoComercial.Domain" as Dom
package "GestaoComercial.Infrastructure" as Infra
package "GestaoComercial.Web" as Web
package "GestaoComercial.Contracts" as Contracts
Api ..> App
Api ..> Infra
Api ..> Contracts
Infra ..> App
Infra ..> Dom
App ..> Dom
Web ..> Contracts
note bottom
Setas: referência entre projetos.
Web contém adaptador próprio de tickets.
Nenhuma referência Web para backend.
end note
''','arquivos .csproj dos seis projetos de produção')
add('12-componentes','Componentes e comunicação', '''
actor Operador
component "Navegador" as Browser
component "Blazor Web\nInteractive Server / BFF" as Web
component "API ASP.NET Core\nAutorização e mapeamento" as Api
component "Portas Application\ne regras Domain" as App
component "Adaptadores Infrastructure\nMongoDB.Driver" as Infra
database "MongoDB Atlas\nbanco único" as Db
folder "Arquivos de imagens\ne chaves Data Protection" as Files
Operador --> Browser
Browser --> Web : HTTPS / circuito
Web --> Api : HTTPS / bearer no servidor
Api --> App
Api --> Infra : composição DI
Infra --> Db : dados de negócio
Web --> Db : somente tickets técnicos
Api --> Files : imagens privadas
Web --> Files : chaves da sessão
''','Program.cs de Api/Web; arquitetura02')
add('13-modelo-documental-comercial','Modelo documental comercial MongoDB', '''
class customers <<collection>> { _id: UUID }
class categories <<collection>> { _id: UUID }
class brands <<collection>> { _id: UUID }
class products <<collection>> {
 _id: UUID
 categoriaId: UUID
 marcaId: UUID ou null
 precoCentavos: Int64
 saldo: objeto incorporado
 imagem: objeto ou null
}
class sales <<collection>> {
 _id: UUID
 clienteId: UUID
 criadorId: UUID
 itens: array incorporado até 100
 clienteSnapshot: objeto ou null
 totalCentavos: Int64
}
class stock_movements <<collection>> {
 produtoId: UUID
 vendaId: UUID ou null
 itemId: UUID ou null
 delta: Int32
}
products ..> categories : categoriaId
products ..> brands : marcaId
sales ..> customers : clienteId
sales ..> products : itens.produtoId
stock_movements ..> products : produtoId
stock_movements ..> sales : vendaId
note bottom
Representação documental, não diagrama relacional.
Referências são UUID; integridade no backend.
Movimentos não são arrays ilimitados em products.
end note
''','ClientesSchema; ProdutosSchema; EstoqueSchema; VendasSchema')
add('14-modelo-documental-seguranca','Modelo documental de segurança e coordenação', '''
class employees <<collection>> { _id: UUID }
class users <<collection>> {
 _id: UUID
 employeeId: UUID ou null
 passwordHash: string
 perfil: string
 securityStamp: string
}
class api_sessions <<collection>> {
 usuarioId: UUID
 tokenHash: binData
 expiraEm: date
}
class tickets <<collection técnica Web>> {
 handleHash: binData
 ticketProtegido: binData
 tokenApiProtegido: binData
}
class audit_events <<collection>> {
 acao: string
 recursoId: UUID ou null
 atorId: UUID ou null
}
class idempotency_receipts <<collection>> {
 atorId: UUID
 escopo: string
 chave: UUID
 hashRequest: binData
 resposta: object
}
class administration_control <<collection>> { guardVersion: Int64 }
class persistence_versions <<collection>> { checksum: string }
users ..> employees : employeeId
api_sessions ..> users : usuarioId
audit_events ..> users : atorId
idempotency_receipts ..> users : atorId
note bottom
Todos no mesmo banco da apresentação.
Tickets não contêm dados comerciais.
Não há token bruto em api_sessions.
end note
''','AuthSchema.cs; MongoTickets.cs; EstoqueSchema.cs; MongoFoundation.cs')

add('15-sequencia-login','Sequência de autenticação', '''
actor Operador as O
participant "Navegador" as N
participant "Web / conta HTTP" as W
participant "API / autenticação" as A
database "Atlas" as D
O -> N : informa email e senha
N -> W : POST formulário + antiforgery
W -> W : validar antiforgery e retorno local
W -> A : POST /auth/login por HTTPS
A -> D : consultar conta / verificar bloqueio
A -> A : validar hash Identity e estado
alt acesso permitido
 A -> D : gravar sessão com hash do token
 A --> W : token opaco e usuário
 W -> D : gravar ticket e token protegidos
 W --> N : cookie de referência seguro
 N --> O : home ou troca obrigatória
else credenciais inválidas
 A --> W : falha genérica
 W --> N : documento de login com erro seguro
end
''','EndpointsAcesso.cs; Security/ContasHttp.cs; MongoAutenticacao.cs; MongoTickets.cs')
add('16-sequencia-confirmacao','Sequência de confirmação de venda', '''
actor Operador as O
participant "Web / BFF" as W
participant "API" as A
participant "MongoVendas" as V
participant "OperacoesComerciais" as C
database "Atlas" as D
O -> W : confirmar rascunho revisado
W -> A : versão esperada + Idempotency-Key
A -> A : autenticar e autorizar
A -> V : TransicionarAsync(confirmacao)
V -> V : verificar propriedade
V -> C : executar com mesma intenção
C -> D : iniciar transação / consultar recibo
alt recibo concluído compatível
 C --> V : resposta original
else nova operação
 C -> D : guarda efetiva do ator
 V -> D : guarda de venda e cliente
 loop itens em ordem determinística
  V -> D : guarda de produto / validar preço
  V -> D : baixa condicional + movimento
 end
 V -> D : venda confirmada + snapshots + auditoria
 C -> D : recibo + commit majority
 C --> V : resultado
end
V --> A : Venda
A --> W : DTO em reais ou erro
W --> O : resultado; reconsultar se necessário
note over C,D
Falha antes do commit reverte todos os efeitos.
Snapshot / majority / primary; chamadas sequenciais.
end note
''','MongoVendas.cs; OperacoesComerciais.cs; MongoEstoque.cs')
add('17-sequencia-cancelamento','Sequência de cancelamento', '''
actor Administrador as O
participant "Web / API" as A
participant "MongoVendas" as V
participant "OperacoesComerciais" as C
database "Atlas" as D
O -> A : confirmar cancelamento com motivo
A -> A : exigir Vendas.Cancelar
A -> V : versão + motivo + chave
V -> C : executar intenção idempotente
C -> D : transação e recibo / guarda do ator
V -> D : guardar venda Confirmada e versão
loop itens da venda
 V -> D : guardar produto sem exigir ativo
 V -> D : somar quantidade e gravar EstornoVenda
end
V -> D : estado Cancelada + evento + auditoria
C -> D : recibo e commit
C --> V : resposta
V --> A : venda cancelada
A --> O : resultado
note over C,D
Mesma chave: recibo original, sem novo estorno.
Chave nova após cancelamento: estado inválido.
Qualquer erro de capacidade provoca rollback integral.
end note
''','MongoVendas.TransicionarAsync; MongoEstoque.AplicarAsync')
add('18-sequencia-estoque','Sequência de movimentação manual', '''
actor "Operador autorizado" as O
participant "Web / API" as A
participant "MongoEstoque" as E
participant "OperacoesComerciais" as C
database "Atlas" as D
O -> A : produto, quantidade ou delta, motivo
A -> A : policy e chave UUID
A -> E : MovimentarAsync
E -> E : validar tipo, delta e motivo
E -> C : executar intenção
C -> D : transação / recibo / guarda do ator
E -> D : consultar e guardar produto ativo
E -> D : update condicional do saldo
E -> D : movimento com saldo antes/depois
E -> D : auditoria
C -> D : recibo e commit
C --> A : resultado
A --> O : saldo e movimento
note over E,D
Sem saldo suficiente: nenhum movimento gravado.
Ajuste é delta, nunca substituição do histórico.
end note
''','EndpointsEstoque.cs; MongoEstoque.cs; Movimentacao.cs')
add('19-atividade-venda','Atividade de venda', '''
start
:Selecionar cliente ativo;
:Localizar produto e informar quantidade;
:Salvar rascunho e revisar preços do servidor;
while (Adicionar ou alterar itens?) is (sim)
 :Alterar cupom e salvar rascunho;
endwhile (não)
if (Descartar?) then (sim)
 :Informar motivo e confirmar descarte;
 stop
endif (não)
:Solicitar confirmação;
if (Versão, referências e preços válidos?) then (sim)
 if (Todos os saldos suficientes?) then (sim)
  :Confirmar atomicamente venda, baixas, histórico e recibo;
 else (não)
  :Rollback e mensagem de estoque insuficiente;
 endif
else (não)
 :Rejeitar sem efeitos; reler e revisar rascunho;
endif
stop
''','VendaDetalhe.razor; MongoVendas.cs')
add('20-atividade-estoque','Atividade de manutenção de estoque', '''
start
:Consultar produto e saldo;
:Escolher entrada, saída manual ou ajuste;
if (Ajuste?) then (sim)
 :Exigir Administrador e delta assinado;
else (não)
 :Exigir Administrador ou Estoquista;
 :Informar quantidade positiva;
endif
:Informar motivo de 10 a 500 caracteres;
:Validar intenção e produto ativo;
if (Saldo final no intervalo permitido?) then (sim)
 :Atualizar saldo e inserir movimento, auditoria e recibo;
 :Concluir transação;
else (não)
 :Rejeitar sem efeito parcial;
endif
:Consultar histórico;
stop
''','Estoque.razor; Movimentacao.cs; MongoEstoque.cs')
add('21-estados-venda','Estados da venda', '''
state Rascunho
state Confirmada
state Cancelada
state Descartada
[*] --> Rascunho : criar
Rascunho --> Rascunho : editar / versão esperada
Rascunho --> Confirmada : confirmar [cliente e itens válidos]\n/ baixa atômica e snapshots
Rascunho --> Descartada : descartar / motivo
Confirmada --> Cancelada : cancelar [Administrador]\n/ estorno integral e motivo
note bottom of Rascunho
Sem reserva de estoque.
0 a 100 itens; confirmação requer ao menos 1.
end note
note bottom of Cancelada
Histórico preservado.
Sem transição para Confirmada.
end note
''','backend/GestaoComercial.Domain/Vendas/RegrasVenda.cs')
add('22-execucao-local','Topologia da execução local', '''
node "Máquina do apresentador" {
 artifact "Navegador" as N
 node "Processo Web .NET 10\nHTTPS 7243" as W
 node "Processo API .NET 10\nHTTPS 7143" as A
 folder "Chaves Data Protection" as K
 folder "Imagens privadas" as I
 W --> K
 A --> I
 N --> W : HTTPS / SignalR
 W --> A : HTTPS / bearer servidor
}
cloud "Atlas existente" {
 database "Banco único\ncoleções comerciais e técnicas" as D
}
A --> D : TLS / dados de negócio
W --> D : TLS / tickets
note bottom of D
Conexão à internet necessária.
Sem servidor público ou deploy no escopo.
Diagrama não revela credenciais nem hostname privado.
end note
''','launchSettings.json de Api/Web; Program.cs; ADR-37')

(OUT / 'indice.json').write_text(json.dumps(DIAGRAMS, ensure_ascii=False, indent=2), encoding='utf-8')
print(f'{len(DIAGRAMS)} diagramas PlantUML criados')
