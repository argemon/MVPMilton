using GestaoComercial.Application.Acesso;
using GestaoComercial.Application.Funcionarios;
using GestaoComercial.Domain.Acesso;
using GestaoComercial.Domain.Cadastros;
using GestaoComercial.Infrastructure.Funcionarios;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.RegularExpressions;
namespace GestaoComercial.Infrastructure.Identity;

// Employee/access operations share the existing administrative transaction and Identity store.
public sealed partial class MongoAutenticacao : IFuncionarios
{
    private IMongoCollection<FuncionarioDocumento> Employees => Db.GetCollection<FuncionarioDocumento>("employees");
    private static Funcionario Employee(FuncionarioDocumento e) => new(e.Id, e.Nome, e.Email, e.Telefone, e.Ativo, e.Versao, new(e.CreatedAt, TimeSpan.Zero), new(e.UpdatedAt, TimeSpan.Zero));
    private static Contato Contact(DadosFuncionario d)
    { try { return Contato.Criar(d.Nome, d.Email, d.Telefone); } catch (ArgumentException) { throw Error(FalhaAcesso.Validacao); } }
    private async Task<FuncionarioDocumento> RequireActiveEmployeeAsync(Guid id, IClientSessionHandle s, CancellationToken ct)
    {
        var e = await Employees.FindOneAndUpdateAsync(s, Builders<FuncionarioDocumento>.Filter.Eq(x => x.Id, id), Builders<FuncionarioDocumento>.Update.Inc(x => x.GuardVersion, 1), new FindOneAndUpdateOptions<FuncionarioDocumento> { ReturnDocument = ReturnDocument.After }, ct)
            ?? throw Error(FalhaAcesso.FuncionarioNaoEncontrado);
        if (!e.Ativo) throw Error(FalhaAcesso.FuncionarioInativo);
        return e;
    }
    private Task AuditEmployeeAsync(IClientSessionHandle s, Guid ator, string acao, Guid id, CancellationToken ct)
        => Db.GetCollection<BsonDocument>("audit_events").InsertOneAsync(s, new BsonDocument
        {
            { "_id", new BsonBinaryData(Guid.NewGuid(), GuidRepresentation.Standard) }, { "schemaVersion", 1 },
            { "atorId", new BsonBinaryData(ator, GuidRepresentation.Standard) }, { "origem", "Autenticado" },
            { "acao", acao }, { "recursoTipo", "Funcionario" }, { "recursoId", new BsonBinaryData(id, GuidRepresentation.Standard) },
            { "ocorridoEm", Now }, { "traceId", request.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString("N") }
        }, cancellationToken: ct);
    public async Task<PaginaFuncionarios> ListarFuncionariosAsync(SessaoAtual ator, string? search, bool? ativo, int page, int pageSize, CancellationToken ct)
    {
        await RequireAdminAsync(ator, ct);
        if (page is < 1 or > 100000 || pageSize is < 1 or > 100 || search?.Length > 150) throw Error(FalhaAcesso.Validacao);
        var f = Builders<FuncionarioDocumento>.Filter.Empty;
        if (ativo is { } a) f &= Builders<FuncionarioDocumento>.Filter.Eq(x => x.Ativo, a);
        if (!string.IsNullOrWhiteSpace(search)) f &= Builders<FuncionarioDocumento>.Filter.Regex(x => x.NomeNormalizado, new BsonRegularExpression("^" + Regex.Escape(search.Trim().ToUpperInvariant())));
        var total = await Employees.CountDocumentsAsync(f, cancellationToken: ct);
        var rows = await Employees.Find(f).SortBy(x => x.NomeNormalizado).ThenBy(x => x.Id).Skip(checked((page - 1) * pageSize)).Limit(pageSize).ToListAsync(ct);
        return new(rows.Select(Employee).ToArray(), page, pageSize, total);
    }
    public async Task<Funcionario> ConsultarFuncionarioAsync(SessaoAtual ator, Guid id, CancellationToken ct)
    {
        await RequireAdminAsync(ator, ct);
        return Employee(await Employees.Find(x => x.Id == id).SingleOrDefaultAsync(ct) ?? throw Error(FalhaAcesso.FuncionarioNaoEncontrado));
    }
    public Task<Funcionario> CriarFuncionarioAsync(SessaoAtual ator, DadosFuncionario dados, CancellationToken ct)
    {
        var d = Contact(dados);
        return TransactionAsync(async (s, token) =>
        {
            await GuardAdministrationAsync(s, token); await ActorAsync(ator, true, s, token);
            var e = new FuncionarioDocumento { Nome = d.Nome, NomeNormalizado = d.Nome.ToUpperInvariant(), Email = d.Email, Telefone = d.Telefone, CreatedAt = Now, UpdatedAt = Now };
            await Employees.InsertOneAsync(s, e, cancellationToken: token); await AuditEmployeeAsync(s, ator.Usuario.Id, "FuncionarioCriado", e.Id, token); return Employee(e);
        }, ct);
    }
    public Task<Funcionario> AtualizarFuncionarioAsync(SessaoAtual ator, Guid id, DadosFuncionario dados, bool ativo, long versao, CancellationToken ct)
    {
        var d = Contact(dados); if (versao < 1) throw Error(FalhaAcesso.Validacao);
        return TransactionAsync(async (s, token) =>
        {
            await GuardAdministrationAsync(s, token); var acting = await ActorAsync(ator, true, s, token);
            var linked = await store.Users.Find(s, x => x.EmployeeId == id).SingleOrDefaultAsync(token);
            if (linked is not null) linked = linked.Id == acting.Id ? acting : await GuardUserAsync(linked.Id, s, token);
            var e = await Employees.Find(s, x => x.Id == id).SingleOrDefaultAsync(token) ?? throw Error(FalhaAcesso.FuncionarioNaoEncontrado);
            if (e.Versao != versao) throw Error(FalhaAcesso.Versao);
            if (!ativo && linked is { Ativo: true, Perfil: Perfis.Administrador } && await store.Users.CountDocumentsAsync(s, x => x.Ativo && x.Perfil == Perfis.Administrador, cancellationToken: token) <= 1) throw Error(FalhaAcesso.UltimoAdministrador);
            var update = Builders<FuncionarioDocumento>.Update.Set(x => x.Nome, d.Nome).Set(x => x.NomeNormalizado, d.Nome.ToUpperInvariant()).Set(x => x.Email, d.Email).Set(x => x.Telefone, d.Telefone).Set(x => x.Ativo, ativo).Set(x => x.UpdatedAt, Now).Inc(x => x.Versao, 1).Inc(x => x.GuardVersion, 1);
            var changed = await Employees.FindOneAndUpdateAsync(s, Builders<FuncionarioDocumento>.Filter.Where(x => x.Id == id && x.Versao == versao), update, new FindOneAndUpdateOptions<FuncionarioDocumento> { ReturnDocument = ReturnDocument.After }, token) ?? throw Error(FalhaAcesso.Versao);
            if (!ativo && linked is not null) { linked.Ativo = false; await RevokeAsync(linked, s, token); await AuditAsync(s, acting.Id, "ContaRevogadaFuncionario", linked.Id, token); }
            await AuditEmployeeAsync(s, acting.Id, "FuncionarioAlterado", id, token); return Employee(changed);
        }, ct);
    }
    public Task<UsuarioResumo> CriarContaAsync(SessaoAtual ator, Guid funcionarioId, string email, string senha, string perfil, CancellationToken ct)
    {
        PasswordBounds(senha);
        if (funcionarioId == Guid.Empty || !Perfis.Valido(perfil) || email.Length > 254 || !System.Net.Mail.MailAddress.TryCreate(email.Trim(), out var parsed) || parsed.Address != email.Trim()) throw Error(FalhaAcesso.Validacao);
        return TransactionAsync(async (s, token) =>
        {
            await GuardAdministrationAsync(s, token); await ActorAsync(ator, true, s, token);
            if (await store.Users.Find(s, x => x.EmployeeId == funcionarioId).AnyAsync(token)) throw Error(FalhaAcesso.VinculoOcupado);
            if (await store.FindByEmailAsync(manager.NormalizeEmail(email.Trim()), token) is not null) throw Error(FalhaAcesso.EmailDuplicado);
            var e = await RequireActiveEmployeeAsync(funcionarioId, s, token);
            var u = new UsuarioIdentity { EmployeeId = e.Id, Nome = e.Nome, NomeNormalizado = e.NomeNormalizado, Email = email.Trim(), Perfil = perfil, CreatedAt = Now, UpdatedAt = Now };
            var result = await manager.CreateAsync(u, senha);
            if (!result.Succeeded && result.Errors.Any(x => x.Code is "DuplicateEmail" or "DuplicateUserName")) throw Error(FalhaAcesso.EmailDuplicado);
            Ensure(result); await AuditAsync(s, ator.Usuario.Id, "ContaCriada", u.Id, token); return Summary(u);
        }, ct);
    }
}
