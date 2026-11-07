using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using GestaoComercial.Application.Acesso;
using GestaoComercial.Domain.Acesso;
using GestaoComercial.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using MongoDB.Bson;
using MongoDB.Driver;
namespace GestaoComercial.Infrastructure.Identity;

public sealed class MongoAutenticacao(IMongoClient client, MongoConfiguration configuration, MongoUserStore store,
    UserManager<UsuarioIdentity> manager, TimeProvider clock, Microsoft.AspNetCore.Http.IHttpContextAccessor request) : IAutenticacao
{
    private IMongoDatabase Db => client.GetDatabase(configuration.DatabaseName);
    private IMongoCollection<SessaoDocumento> Sessions => Db.GetCollection<SessaoDocumento>("api_sessions");
    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    private static readonly Lazy<(UsuarioIdentity User, string Hash)> Dummy = new(() =>
    { var u = new UsuarioIdentity(); return (u, new PasswordHasher<UsuarioIdentity>().HashPassword(u, Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)))); });
    private static byte[] Hash(string token) => SHA256.HashData(Encoding.UTF8.GetBytes(token));
    public static bool SessaoValida(SessaoDocumento s, DateTime now) => s.RevogadaEm is null && s.ExpiraEm > now && s.UltimoUsoEm.AddMinutes(30) > now;
    private static UsuarioAcesso Access(UsuarioIdentity u) => new(u.Id, u.Nome, u.Perfil, u.DeveAlterarSenha);
    private static UsuarioResumo Summary(UsuarioIdentity u) => new(u.Id, u.Nome, u.Email, u.Perfil, u.Ativo, u.Versao, u.DeveAlterarSenha, u.EmployeeId);
    private static AcessoException Error(FalhaAcesso f) => new(f);
    private static void Ensure(IdentityResult result) { if (!result.Succeeded) throw Error(result.Errors.Any(x => x.Code == "ConcurrencyFailure") ? FalhaAcesso.Versao : FalhaAcesso.Validacao); }
    private static void PasswordBounds(string? p) { if (p is null || p.Length is < 12 or > 128) throw Error(FalhaAcesso.Validacao); }
    private async Task<T> TransactionAsync<T>(Func<IClientSessionHandle, CancellationToken, Task<T>> body, CancellationToken ct)
    {
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(ct); budget.CancelAfter(TimeSpan.FromSeconds(5)); var token = budget.Token;
        using var session = await client.StartSessionAsync(cancellationToken: token);
        try
        {
            for (var attempt = 0; ; attempt++)
            {
                session.StartTransaction(new TransactionOptions(ReadConcern.Snapshot, ReadPreference.Primary, WriteConcern.WMajority)); store.Session = session;
                try
                {
                    var result = await body(session, token);
                    while (true)
                    {
                        try { await session.CommitTransactionAsync(token); return result; }
                        catch (MongoException e) when (e.HasErrorLabel("UnknownTransactionCommitResult") && !token.IsCancellationRequested) { await Task.Delay(20, token); }
                    }
                }
                catch (MongoException e) when (e.HasErrorLabel("TransientTransactionError") && attempt < 2 && !token.IsCancellationRequested)
                {
                    if (session.IsInTransaction) await session.AbortTransactionAsync(token);
                    await Task.Delay(250 * (attempt + 1) + Random.Shared.Next(20, 70), token);
                }
                catch { if (session.IsInTransaction) { try { await session.AbortTransactionAsync(CancellationToken.None); } catch (MongoException) { } } throw; }
            }
        }
        finally { store.Session = null; }
    }
    private async Task GuardAdministrationAsync(IClientSessionHandle s, CancellationToken ct)
    {
        var result = await Db.GetCollection<BsonDocument>("administration_control").UpdateOneAsync(s, new BsonDocument("_id", "administration"), new BsonDocument("$inc", new BsonDocument("guardVersion", 1L)), cancellationToken: ct);
        if (result.ModifiedCount != 1) throw new InvalidOperationException("Evolução01 necessária.");
    }
    private async Task<UsuarioIdentity?> GuardUserAsync(Guid id, IClientSessionHandle s, CancellationToken ct)
        => await store.Users.FindOneAndUpdateAsync(s, Builders<UsuarioIdentity>.Filter.Eq(x => x.Id, id), Builders<UsuarioIdentity>.Update.Inc(x => x.GuardVersion, 1), new FindOneAndUpdateOptions<UsuarioIdentity> { ReturnDocument = ReturnDocument.After }, ct);
    private async Task<UsuarioIdentity> ActorAsync(SessaoAtual actor, bool admin, IClientSessionHandle s, CancellationToken ct)
    {
        var u = await GuardUserAsync(actor.Usuario.Id, s, ct); var current = await Sessions.Find(s, x => x.Id == actor.Id).SingleOrDefaultAsync(ct);
        if (u is null || current is null || !u.Ativo || !Perfis.Valido(u.Perfil) || u.SecurityStamp != actor.Stamp || current.Stamp != u.SecurityStamp || !SessaoValida(current, Now)) throw Error(FalhaAcesso.Credenciais);
        if (admin && u.DeveAlterarSenha) throw Error(FalhaAcesso.TrocaObrigatoria);
        if (admin && u.Perfil != Perfis.Administrador) throw Error(FalhaAcesso.Proibido);
        return u;
    }
    private Task AuditAsync(IClientSessionHandle s, Guid? actor, string action, Guid? resource, CancellationToken ct, bool bootstrap = false)
        => Db.GetCollection<BsonDocument>("audit_events").InsertOneAsync(s, new BsonDocument
        {
            { "_id", new BsonBinaryData(Guid.NewGuid(), GuidRepresentation.Standard) }, { "schemaVersion", 1 },
            { "atorId", actor is { } id ? new BsonBinaryData(id, GuidRepresentation.Standard) : BsonNull.Value },
            { "origem", bootstrap ? "Bootstrap" : actor is null ? "Anonimo" : "Autenticado" }, { "acao", action }, { "recursoTipo", "Usuario" },
            { "recursoId", resource is { } rid ? new BsonBinaryData(rid, GuidRepresentation.Standard) : BsonNull.Value }, { "ocorridoEm", Now }, { "traceId", request.HttpContext?.TraceIdentifier ?? System.Diagnostics.Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N") }
        }, cancellationToken: ct);
    public async Task<bool> ProvisionarAsync(string nome, string email, string senha, CancellationToken ct)
    {
        PasswordBounds(senha); nome = Regex.Replace(nome.Trim(), @"\s+", " ");
        if (nome.Length is < 2 or > 150 || email.Length > 254) throw Error(FalhaAcesso.Validacao);
        return await TransactionAsync(async (s, token) =>
        {
            await GuardAdministrationAsync(s, token);
            if (await store.Users.Find(s, x => x.Perfil == Perfis.Administrador).AnyAsync(token)) return false;
            var u = new UsuarioIdentity { Nome = nome, NomeNormalizado = nome.ToUpperInvariant(), Email = email.Trim(), Perfil = Perfis.Administrador, CreatedAt = Now, UpdatedAt = Now };
            Ensure(await manager.CreateAsync(u, senha)); await AuditAsync(s, u.Id, "Bootstrap", u.Id, token, true); return true;
        }, ct);
    }
    public async Task<EntradaSessao?> EntrarAsync(string email, string senha, CancellationToken ct)
    {
        if (email.Length is < 3 or > 254 || senha.Length is < 1 or > 128) throw Error(FalhaAcesso.Validacao);
        var normalized = manager.NormalizeEmail(email.Trim()); var raw = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        return await TransactionAsync<EntradaSessao?>(async (s, token) =>
        {
            var u = await store.FindByEmailAsync(normalized, token);
            if (u is not null) u = await GuardUserAsync(u.Id, s, token);
            if (u is null || !u.Ativo || !Perfis.Valido(u.Perfil) || u.LockoutEnd > Now)
            {
                _ = manager.PasswordHasher.VerifyHashedPassword(Dummy.Value.User, Dummy.Value.Hash, senha);
                await AuditAsync(s, null, "LoginFalhou", null, token); return null;
            }
            var valid = await manager.CheckPasswordAsync(u, senha);
            if (!valid)
            {
                u.FailedAccessCount++;
                if (u.FailedAccessCount >= manager.Options.Lockout.MaxFailedAccessAttempts) { u.LockoutEnd = Now.Add(manager.Options.Lockout.DefaultLockoutTimeSpan); u.FailedAccessCount = 0; }
                u.UpdatedAt = Now; Ensure(await store.UpdateAsync(u, token)); await AuditAsync(s, null, "LoginFalhou", null, token); return null;
            }
            u.FailedAccessCount = 0; u.LockoutEnd = null; u.UpdatedAt = Now; Ensure(await store.UpdateAsync(u, token));
            var session = new SessaoDocumento { UsuarioId = u.Id, TokenHash = Hash(raw), Stamp = u.SecurityStamp, CriadoEm = Now, UltimoUsoEm = Now, ExpiraEm = Now.AddHours(8) };
            await Sessions.InsertOneAsync(s, session, cancellationToken: token); await AuditAsync(s, u.Id, "Login", u.Id, token);
            return new EntradaSessao(raw, new DateTimeOffset(session.ExpiraEm, TimeSpan.Zero), Access(u));
        }, ct);
    }
    public async Task<SessaoAtual?> ValidarAsync(string token, bool registrarUso, CancellationToken ct)
    {
        if (token.Length != 43 || token.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '-' and not '_')) return null;
        using var s = await client.StartSessionAsync(new ClientSessionOptions { CausalConsistency = false }, ct);
        var sessions = Sessions.WithReadConcern(ReadConcern.Linearizable).WithReadPreference(ReadPreference.Primary);
        var session = await sessions.Find(s, Builders<SessaoDocumento>.Filter.Eq(x => x.TokenHash, Hash(token)), new FindOptions { MaxTime = TimeSpan.FromSeconds(3) }).SingleOrDefaultAsync(ct);
        if (session is null || !SessaoValida(session, Now)) return null;
        var u = await store.Users.WithReadConcern(ReadConcern.Linearizable).WithReadPreference(ReadPreference.Primary).Find(s, x => x.Id == session.UsuarioId, new FindOptions { MaxTime = TimeSpan.FromSeconds(3) }).SingleOrDefaultAsync(ct);
        if (u is null || !u.Ativo || !Perfis.Valido(u.Perfil) || u.SecurityStamp != session.Stamp) return null;
        if (registrarUso)
        {
            var now = Now;
            var changed = await Sessions.UpdateOneAsync(s, x => x.Id == session.Id && x.RevogadaEm == null && x.ExpiraEm > now && x.UltimoUsoEm > now.AddMinutes(-30), Builders<SessaoDocumento>.Update.Max(x => x.UltimoUsoEm, now), cancellationToken: ct);
            if (changed.MatchedCount != 1) return null;
        }
        return new SessaoAtual(session.Id, Access(u), u.SecurityStamp, new DateTimeOffset(session.ExpiraEm, TimeSpan.Zero));
    }
    public Task SairAsync(SessaoAtual actor, CancellationToken ct) => TransactionAsync(async (s, token) =>
    {
        var u = await ActorAsync(actor, false, s, token);
        await Sessions.UpdateOneAsync(s, x => x.Id == actor.Id && x.RevogadaEm == null, Builders<SessaoDocumento>.Update.Set(x => x.RevogadaEm, Now), cancellationToken: token);
        await AuditAsync(s, u.Id, "Logout", u.Id, token); return true;
    }, ct);
    private async Task RevokeAsync(UsuarioIdentity u, IClientSessionHandle s, CancellationToken ct)
    {
        u.SecurityStamp = Guid.NewGuid().ToString("N"); u.UpdatedAt = Now; Ensure(await store.UpdateAsync(u, ct));
        await Sessions.UpdateManyAsync(s, x => x.UsuarioId == u.Id && x.RevogadaEm == null, Builders<SessaoDocumento>.Update.Set(x => x.RevogadaEm, Now), cancellationToken: ct);
    }
    public async Task AlterarSenhaAsync(SessaoAtual actor, string atual, string nova, CancellationToken ct)
    {
        PasswordBounds(nova); if (atual.Length > 128) throw Error(FalhaAcesso.Validacao);
        await TransactionAsync(async (s, token) =>
        {
            await GuardAdministrationAsync(s, token); var u = await ActorAsync(actor, false, s, token);
            if (atual == nova) throw Error(FalhaAcesso.Validacao);
            Ensure(await manager.ChangePasswordAsync(u, atual, nova)); u.DeveAlterarSenha = false; u.FailedAccessCount = 0; u.LockoutEnd = null;
            await RevokeAsync(u, s, token); await AuditAsync(s, u.Id, "SenhaAlterada", u.Id, token); return true;
        }, ct);
    }
    private async Task RequireAdminAsync(SessaoAtual actor, CancellationToken ct)
    {
        var u = await store.FindByIdAsync(actor.Usuario.Id.ToString(), ct);
        if (u is null || !u.Ativo || u.SecurityStamp != actor.Stamp) throw Error(FalhaAcesso.Credenciais);
        if (u.DeveAlterarSenha) throw Error(FalhaAcesso.TrocaObrigatoria);
        if (u.Perfil != Perfis.Administrador) throw Error(FalhaAcesso.Proibido);
    }
    public async Task<PaginaUsuarios> ListarAsync(SessaoAtual actor, int page, int pageSize, string? search, bool? ativo, CancellationToken ct)
    {
        await RequireAdminAsync(actor, ct);
        if (page is < 1 or > 100000 || pageSize is < 1 or > 100 || search?.Length > 150) throw Error(FalhaAcesso.Validacao);
        var f = Builders<UsuarioIdentity>.Filter.Empty;
        if (ativo is { } a) f &= Builders<UsuarioIdentity>.Filter.Eq(x => x.Ativo, a);
        if (!string.IsNullOrWhiteSpace(search)) f &= Builders<UsuarioIdentity>.Filter.Regex(x => x.NomeNormalizado, new BsonRegularExpression("^" + Regex.Escape(search.Trim().ToUpperInvariant())));
        var total = await store.Users.CountDocumentsAsync(f, cancellationToken: ct);
        var rows = await store.Users.Find(f).SortBy(x => x.NomeNormalizado).ThenBy(x => x.Id).Skip(checked((page - 1) * pageSize)).Limit(pageSize).ToListAsync(ct);
        return new PaginaUsuarios(rows.Select(Summary).ToArray(), page, pageSize, total);
    }
    public async Task<UsuarioResumo> ConsultarAsync(SessaoAtual actor, Guid id, CancellationToken ct)
    {
        await RequireAdminAsync(actor, ct); var u = await store.FindByIdAsync(id.ToString(), ct); return u is null ? throw Error(FalhaAcesso.NaoEncontrado) : Summary(u);
    }
    public async Task<UsuarioResumo> AtualizarAsync(SessaoAtual actor, Guid id, string perfil, bool ativo, long versao, CancellationToken ct)
    {
        if (!Perfis.Valido(perfil) || versao < 1) throw Error(FalhaAcesso.Validacao);
        return await TransactionAsync(async (s, token) =>
        {
            await GuardAdministrationAsync(s, token); var acting = await ActorAsync(actor, true, s, token);
            var u = acting.Id == id ? acting : await GuardUserAsync(id, s, token) ?? throw Error(FalhaAcesso.NaoEncontrado);
            if (u.Versao != versao) throw Error(FalhaAcesso.Versao);
            if (u.Perfil == perfil && u.Ativo == ativo) return Summary(u);
            if (u.Ativo && u.Perfil == Perfis.Administrador && (!ativo || perfil != Perfis.Administrador)
                && await store.Users.CountDocumentsAsync(s, x => x.Ativo && x.Perfil == Perfis.Administrador, cancellationToken: token) <= 1) throw Error(FalhaAcesso.UltimoAdministrador);
            u.Perfil = perfil; u.Ativo = ativo; await RevokeAsync(u, s, token); await AuditAsync(s, acting.Id, "ContaAlterada", id, token); return Summary(u);
        }, ct);
    }
    public async Task RedefinirAsync(SessaoAtual actor, Guid id, string senha, long versao, CancellationToken ct)
    {
        PasswordBounds(senha); if (versao < 1) throw Error(FalhaAcesso.Validacao);
        await TransactionAsync(async (s, token) =>
        {
            await GuardAdministrationAsync(s, token); var acting = await ActorAsync(actor, true, s, token);
            var u = acting.Id == id ? acting : await GuardUserAsync(id, s, token) ?? throw Error(FalhaAcesso.NaoEncontrado);
            if (u.Versao != versao) throw Error(FalhaAcesso.Versao);
            foreach (var validator in manager.PasswordValidators) Ensure(await validator.ValidateAsync(manager, u, senha));
            u.PasswordHash = manager.PasswordHasher.HashPassword(u, senha); u.DeveAlterarSenha = true; u.FailedAccessCount = 0; u.LockoutEnd = null;
            await RevokeAsync(u, s, token); await AuditAsync(s, acting.Id, "SenhaRedefinida", id, token); return true;
        }, ct);
    }
}
