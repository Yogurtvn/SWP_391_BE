using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RepositoryLayer.DependencyInjection;
using ServiceLayer.Contracts.Auth;
using ServiceLayer.Contracts.Orders;
using ServiceLayer.Contracts.Policy;
using ServiceLayer.Contracts.Security;
using ServiceLayer.Security;
using ServiceLayer.Services.Auth;
using ServiceLayer.Services.Orders;
using ServiceLayer.Services.Policy;

namespace ServiceLayer.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServiceLayer(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRepositoryLayer(configuration);
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IPolicyService, PolicyService>();
        return services;
    }
}
