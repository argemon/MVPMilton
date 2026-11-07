using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;
using GestaoComercial.Infrastructure.Persistence;
namespace GestaoComercial.Infrastructure.Identity;

// Scoped to a request/command. The coordinator owns transaction lifetime; stores never commit.
public sealed class MongoUserStore(IMongoClient client, MongoConfiguration configuration) :
    IUserStore<UsuarioIdentity>, IUserPasswordStore<UsuarioIdentity>, IUserEmailStore<UsuarioIdentity>,
    IUserSecurityStampStore<UsuarioIdentity>, IUserLockoutStore<UsuarioIdentity>
{
    internal IMongoCollection<UsuarioIdentity> Users { get; } = client.GetDatabase(configuration.DatabaseName).GetCollection<UsuarioIdentity>("users");
    internal IClientSessionHandle? Session { get; set; }
    internal async Task<UsuarioIdentity?> FindAsync(FilterDefinition<UsuarioIdentity> filter, CancellationToken ct)
        => Session is { } s ? await Users.Find(s, filter).SingleOrDefaultAsync(ct) : await Users.Find(filter).SingleOrDefaultAsync(ct);
    public async Task<IdentityResult> CreateAsync(UsuarioIdentity user, CancellationToken ct)
    {
        try
        {
            if (Session is { } s) await Users.InsertOneAsync(s, user, cancellationToken: ct); else await Users.InsertOneAsync(user, cancellationToken: ct);
            return IdentityResult.Success;
        }
        catch (MongoWriteException e) when (e.WriteError.Category == ServerErrorCategory.DuplicateKey)
        { return IdentityResult.Failed(new IdentityError { Code = "DuplicateEmail", Description = "Identificação indisponível." }); }
    }
    public async Task<IdentityResult> UpdateAsync(UsuarioIdentity user, CancellationToken ct)
    {
        var version = user.Versao; user.Versao = checked(version + 1);
        var filter = Builders<UsuarioIdentity>.Filter.Where(x => x.Id == user.Id && x.Versao == version);
        var result = Session is { } s ? await Users.ReplaceOneAsync(s, filter, user, cancellationToken: ct) : await Users.ReplaceOneAsync(filter, user, cancellationToken: ct);
        return result.MatchedCount == 1 ? IdentityResult.Success : IdentityResult.Failed(new IdentityError { Code = "ConcurrencyFailure" });
    }
    public Task<IdentityResult> DeleteAsync(UsuarioIdentity u, CancellationToken ct) => throw new NotSupportedException("Contas são inativadas, nunca excluídas.");
    public Task<UsuarioIdentity?> FindByIdAsync(string id, CancellationToken ct) => Guid.TryParse(id, out var value) ? FindAsync(Builders<UsuarioIdentity>.Filter.Eq(x => x.Id, value), ct) : Task.FromResult<UsuarioIdentity?>(null);
    public Task<UsuarioIdentity?> FindByNameAsync(string name, CancellationToken ct) => FindByEmailAsync(name, ct);
    public Task<UsuarioIdentity?> FindByEmailAsync(string email, CancellationToken ct) => FindAsync(Builders<UsuarioIdentity>.Filter.Eq(x => x.EmailNormalizado, email), ct);
    public Task<string> GetUserIdAsync(UsuarioIdentity u, CancellationToken ct) => Task.FromResult(u.Id.ToString());
    public Task<string?> GetUserNameAsync(UsuarioIdentity u, CancellationToken ct) => Task.FromResult<string?>(u.Email);
    public Task SetUserNameAsync(UsuarioIdentity u, string? n, CancellationToken ct) { u.Email = n ?? ""; return Task.CompletedTask; }
    public Task<string?> GetNormalizedUserNameAsync(UsuarioIdentity u, CancellationToken ct) => Task.FromResult<string?>(u.EmailNormalizado);
    public Task SetNormalizedUserNameAsync(UsuarioIdentity u, string? n, CancellationToken ct) { u.EmailNormalizado = n ?? ""; return Task.CompletedTask; }
    public Task SetPasswordHashAsync(UsuarioIdentity u, string? hash, CancellationToken ct) { u.PasswordHash = hash ?? ""; return Task.CompletedTask; }
    public Task<string?> GetPasswordHashAsync(UsuarioIdentity u, CancellationToken ct) => Task.FromResult<string?>(u.PasswordHash);
    public Task<bool> HasPasswordAsync(UsuarioIdentity u, CancellationToken ct) => Task.FromResult(u.PasswordHash.Length != 0);
    public Task SetEmailAsync(UsuarioIdentity u, string? email, CancellationToken ct) { u.Email = email ?? ""; return Task.CompletedTask; }
    public Task<string?> GetEmailAsync(UsuarioIdentity u, CancellationToken ct) => Task.FromResult<string?>(u.Email);
    public Task<bool> GetEmailConfirmedAsync(UsuarioIdentity u, CancellationToken ct) => Task.FromResult(false);
    public Task SetEmailConfirmedAsync(UsuarioIdentity u, bool confirmed, CancellationToken ct) => throw new NotSupportedException("Confirmação por email não integra o MVP.");
    public Task<string?> GetNormalizedEmailAsync(UsuarioIdentity u, CancellationToken ct) => Task.FromResult<string?>(u.EmailNormalizado);
    public Task SetNormalizedEmailAsync(UsuarioIdentity u, string? email, CancellationToken ct) { u.EmailNormalizado = email ?? ""; return Task.CompletedTask; }
    public Task SetSecurityStampAsync(UsuarioIdentity u, string stamp, CancellationToken ct) { u.SecurityStamp = stamp; return Task.CompletedTask; }
    public Task<string?> GetSecurityStampAsync(UsuarioIdentity u, CancellationToken ct) => Task.FromResult<string?>(u.SecurityStamp);
    public Task<DateTimeOffset?> GetLockoutEndDateAsync(UsuarioIdentity u, CancellationToken ct) => Task.FromResult(u.LockoutEnd is { } d ? (DateTimeOffset?)new DateTimeOffset(d, TimeSpan.Zero) : null);
    public Task SetLockoutEndDateAsync(UsuarioIdentity u, DateTimeOffset? date, CancellationToken ct) { u.LockoutEnd = date?.UtcDateTime; return Task.CompletedTask; }
    public Task<int> IncrementAccessFailedCountAsync(UsuarioIdentity u, CancellationToken ct) => Task.FromResult(++u.FailedAccessCount);
    public Task ResetAccessFailedCountAsync(UsuarioIdentity u, CancellationToken ct) { u.FailedAccessCount = 0; return Task.CompletedTask; }
    public Task<int> GetAccessFailedCountAsync(UsuarioIdentity u, CancellationToken ct) => Task.FromResult(u.FailedAccessCount);
    public Task<bool> GetLockoutEnabledAsync(UsuarioIdentity u, CancellationToken ct) => Task.FromResult(true);
    public Task SetLockoutEnabledAsync(UsuarioIdentity u, bool enabled, CancellationToken ct) { if (!enabled) throw new NotSupportedException("Lockout obrigatório."); return Task.CompletedTask; }
    public void Dispose() { }
}
