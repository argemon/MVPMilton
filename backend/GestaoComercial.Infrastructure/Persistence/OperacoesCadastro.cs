using GestaoComercial.Application.Acesso;
using GestaoComercial.Application.Clientes;
using GestaoComercial.Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Driver;
namespace GestaoComercial.Infrastructure.Persistence;

// Technical retry/security/audit support for concrete registration use cases, not a repository.
public sealed class OperacoesCadastro(IMongoClient client, MongoConfiguration configuration, TimeProvider clock, IHttpContextAccessor request)
{
    internal IMongoDatabase Db => client.GetDatabase(configuration.DatabaseName);
    internal DateTime Now => clock.GetUtcNow().UtcDateTime;
    internal async Task ExigirAsync(SessaoAtual actor, string[] roles, CancellationToken ct, IClientSessionHandle? session = null)
    {
        var users = Db.GetCollection<UsuarioIdentity>("users"); var sessions = Db.GetCollection<SessaoDocumento>("api_sessions");
        var u = session is null ? await users.Find(x => x.Id == actor.Usuario.Id).SingleOrDefaultAsync(ct) : await users.Find(session, x => x.Id == actor.Usuario.Id).SingleOrDefaultAsync(ct);
        var s = session is null ? await sessions.Find(x => x.Id == actor.Id).SingleOrDefaultAsync(ct) : await sessions.Find(session, x => x.Id == actor.Id).SingleOrDefaultAsync(ct);
        if (u is null || s is null || !u.Ativo || u.SecurityStamp != actor.Stamp || s.Stamp != u.SecurityStamp || !MongoAutenticacao.SessaoValida(s, Now)) throw new AcessoException(FalhaAcesso.Credenciais);
        if (u.DeveAlterarSenha || !roles.Contains(u.Perfil)) throw new AcessoException(FalhaAcesso.Proibido);
    }
    internal async Task<T> ExecutarAsync<T>(SessaoAtual actor, string[] roles, Func<IClientSessionHandle, CancellationToken, Task<T>> body, CancellationToken ct)
    {
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(ct); budget.CancelAfter(TimeSpan.FromSeconds(5)); var token = budget.Token;
        using var s = await client.StartSessionAsync(cancellationToken: token);
        for (var attempt = 0; ; attempt++)
        {
            s.StartTransaction(new TransactionOptions(ReadConcern.Snapshot, ReadPreference.Primary, WriteConcern.WMajority));
            try
            {
                await ExigirAsync(actor, roles, token, s); var result = await body(s, token);
                while (true)
                {
                    try { await s.CommitTransactionAsync(token); return result; }
                    catch (MongoException e) when (e.HasErrorLabel("UnknownTransactionCommitResult") && !token.IsCancellationRequested) { await Task.Delay(20, token); }
                }
            }
            catch (MongoException e) when (e.HasErrorLabel("TransientTransactionError") && attempt < 2 && !token.IsCancellationRequested)
            {
                if (s.IsInTransaction) await s.AbortTransactionAsync(token);
                await Task.Delay(250 * (attempt + 1) + Random.Shared.Next(20, 70), token);
            }
            catch { if (s.IsInTransaction) { try { await s.AbortTransactionAsync(CancellationToken.None); } catch (MongoException) { } } throw; }
        }
    }
    internal Task AuditarAsync(IClientSessionHandle s, SessaoAtual actor, string action, string type, Guid id, CancellationToken ct) => Db.GetCollection<BsonDocument>("audit_events").InsertOneAsync(s, new BsonDocument
    {
        { "_id", new BsonBinaryData(Guid.NewGuid(), GuidRepresentation.Standard) }, { "schemaVersion", 1 },
        { "atorId", new BsonBinaryData(actor.Usuario.Id, GuidRepresentation.Standard) }, { "origem", "Autenticado" },
        { "acao", action }, { "recursoTipo", type }, { "recursoId", new BsonBinaryData(id, GuidRepresentation.Standard) },
        { "ocorridoEm", Now }, { "traceId", request.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString("N") }
    }, cancellationToken: ct);
    internal static void Pagina(int page, int pageSize, string? search = null)
    { if (page is < 1 or > 100000 || pageSize is < 1 or > 100 || search?.Length > 150) throw new CadastroException("VALIDACAO"); }
}
