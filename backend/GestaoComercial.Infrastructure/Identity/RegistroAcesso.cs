using GestaoComercial.Application.Acesso;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
namespace GestaoComercial.Infrastructure.Identity;

public static class RegistroAcesso
{
    public static IServiceCollection AddAcesso(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<AuthSchema>();
        services.AddScoped<MongoUserStore>();
        services.AddIdentityCore<UsuarioIdentity>(o =>
        {
            o.User.RequireUniqueEmail = true;
            o.Password.RequiredLength = 12; o.Password.RequireDigit = true; o.Password.RequireLowercase = true; o.Password.RequireUppercase = true; o.Password.RequireNonAlphanumeric = true;
            o.Lockout.MaxFailedAccessAttempts = 5; o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        }).AddUserStore<MongoUserStore>();
        // UserManager and coordinator must use the exact same scoped store/session.
        services.AddScoped<IUserStore<UsuarioIdentity>>(s => s.GetRequiredService<MongoUserStore>());
        services.AddScoped<IAutenticacao, MongoAutenticacao>();
        return services;
    }
}
