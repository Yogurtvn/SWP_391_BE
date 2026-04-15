using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RepositoryLayer.DependencyInjection;
using ServiceLayer.Contracts.Auth;
using ServiceLayer.Contracts.Security;
using ServiceLayer.Security;
using ServiceLayer.Services.Auth;

namespace ServiceLayer.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServiceLayer(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRepositoryLayer(configuration);
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        return services;
    }
}
