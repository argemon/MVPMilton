using System.Text.RegularExpressions;
using GestaoComercial.Application.Acesso;
using GestaoComercial.Application.Clientes;
using GestaoComercial.Domain.Cadastros;
using GestaoComercial.Infrastructure.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;
namespace GestaoComercial.Infrastructure.Clientes;

public sealed class MongoClientes(OperacoesCadastro operations) : IClientes
{
    private static readonly string[] Roles = ["Administrador", "Vendedor"];
    private IMongoCollection<ClienteDocumento> Customers => operations.Db.GetCollection<ClienteDocumento>("customers");
    private static Cliente Public(ClienteDocumento c) => new(c.Id, c.Nome, c.Tipo, c.Documento, c.Email, c.Telefone, c.Ativo, c.Versao, new(c.CreatedAt, TimeSpan.Zero), new(c.UpdatedAt, TimeSpan.Zero));
    public async Task<PaginaClientes> ListarAsync(SessaoAtual ator, string? search, string? tipo, bool? ativo, int page, int pageSize, CancellationToken ct)
    {
        await operations.ExigirAsync(ator, Roles, ct); OperacoesCadastro.Pagina(page, pageSize, search);
        if (tipo is not null and not "PF" and not "PJ") throw new CadastroException("VALIDACAO");
        var f = Builders<ClienteDocumento>.Filter.Empty;
        if (ativo is { } a) f &= Builders<ClienteDocumento>.Filter.Eq(x => x.Ativo, a);
        if (tipo is not null) f &= Builders<ClienteDocumento>.Filter.Eq(x => x.Tipo, tipo);
        if (!string.IsNullOrWhiteSpace(search)) {
            var q = search.Trim();
            f &= Builders<ClienteDocumento>.Filter.Regex(x => x.NomeNormalizado, new BsonRegularExpression("^" + Regex.Escape(q.ToUpperInvariant()))) | Builders<ClienteDocumento>.Filter.Eq(x => x.Documento, q) | Builders<ClienteDocumento>.Filter.Eq(x => x.Email, q);
        }
        var total = await Customers.CountDocumentsAsync(f, cancellationToken: ct);
        var rows = await Customers.Find(f).SortBy(x => x.NomeNormalizado).ThenBy(x => x.Id).Skip(checked((page - 1) * pageSize)).Limit(pageSize).ToListAsync(ct);
        return new(rows.Select(Public).ToArray(), page, pageSize, total);
    }
    public async Task<Cliente> ConsultarAsync(SessaoAtual ator, Guid id, CancellationToken ct)
    {
        await operations.ExigirAsync(ator, Roles, ct);
        return Public(await Customers.Find(x => x.Id == id).SingleOrDefaultAsync(ct) ?? throw new CadastroException("CLIENTE_NAO_ENCONTRADO", 404));
    }
    public async Task<Cliente> SalvarAsync(SessaoAtual ator, Guid? id, DadosCliente dados, bool ativo, long versao, CancellationToken ct)
    {
        Contato c; string? documento;
        try { c = Contato.Criar(dados.Nome, dados.Email, dados.Telefone); documento = DocumentoCliente.Validar(dados.Tipo, dados.Documento); }
        catch (ArgumentException) { throw new CadastroException("VALIDACAO"); }
        if (id is not null && versao < 1) throw new CadastroException("VALIDACAO");
        try {
            return await operations.ExecutarAsync(ator, Roles, async (s, token) => {
                ClienteDocumento customer;
                if (id is null) {
                    customer = new ClienteDocumento { Nome = c.Nome, NomeNormalizado = c.Nome.ToUpperInvariant(), Tipo = dados.Tipo!, Documento = documento, Email = c.Email, Telefone = c.Telefone, CreatedAt = operations.Now, UpdatedAt = operations.Now };
                    await Customers.InsertOneAsync(s, customer, cancellationToken: token);
                } else {
                    var old = await Customers.Find(s, x => x.Id == id.Value).SingleOrDefaultAsync(token) ?? throw new CadastroException("CLIENTE_NAO_ENCONTRADO", 404);
                    if (old.Versao != versao) throw new CadastroException("VERSAO_DESATUALIZADA", 409);
                    var update = Builders<ClienteDocumento>.Update.Set(x => x.Nome,c.Nome).Set(x => x.NomeNormalizado,c.Nome.ToUpperInvariant()).Set(x => x.Tipo,dados.Tipo!).Set(x => x.Documento,documento).Set(x => x.Email,c.Email).Set(x => x.Telefone,c.Telefone).Set(x => x.Ativo,ativo).Set(x => x.UpdatedAt,operations.Now).Inc(x => x.Versao,1).Inc(x => x.GuardVersion,1);
                    customer = await Customers.FindOneAndUpdateAsync(s, Builders<ClienteDocumento>.Filter.Where(x => x.Id == id.Value && x.Versao == versao), update, new FindOneAndUpdateOptions<ClienteDocumento> { ReturnDocument = ReturnDocument.After }, token) ?? throw new CadastroException("VERSAO_DESATUALIZADA",409);
                }
                await operations.AuditarAsync(s, ator, id is null ? "ClienteCriado" : "ClienteAlterado", "Cliente", customer.Id, token); return Public(customer);
            }, ct);
        } catch (MongoWriteException e) when (e.WriteError.Category == ServerErrorCategory.DuplicateKey) { throw new CadastroException("DOCUMENTO_DUPLICADO",409); }
    }
    public async Task<HistoricoCliente> HistoricoAsync(SessaoAtual ator, Guid id, int page, int pageSize, DateOnly? inicio, DateOnly? fim, CancellationToken ct)
    {
        OperacoesCadastro.Pagina(page,pageSize);
        if (inicio.HasValue != fim.HasValue || inicio is { } a && fim is { } b && (a >= b || b.DayNumber - a.DayNumber > 366)) throw new CadastroException("VALIDACAO");
        await ConsultarAsync(ator,id,ct);
        // Incremental contract03. Replace query in06; never access/create a future collection.
        return new([],page,pageSize,0);
    }
}
