using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RepositoryLayer.DependencyInjection;
using ServiceLayer.Contracts.Auth;
using ServiceLayer.Contracts.Inventory;
using ServiceLayer.Contracts.Orders;
using ServiceLayer.Contracts.Policy;
using ServiceLayer.Contracts.Report;
using ServiceLayer.Contracts.Security;
using ServiceLayer.Contracts.Shipping;
using ServiceLayer.Contracts.StockReceipt;
using ServiceLayer.DTOs.Shipping;
using ServiceLayer.Security;
using ServiceLayer.Services.Auth;
using ServiceLayer.Services.InventoryManagement;
using ServiceLayer.Services.Orders;
using ServiceLayer.Services.Policy;
using ServiceLayer.Services.ReportManagement;
using ServiceLayer.Services.Shipping;
using ServiceLayer.Services.StockReceiptManagement;

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
        services.AddScoped<IInventoryService, InventoryService>(); // Đăng ký InventoryService vào DI container
        services.AddScoped<IStockReceiptService, StockReceiptService>(); // Đăng ký StockReceiptService vào DI container
        services.AddScoped<IReportService, ReportService>(); // Đăng ký ReportService vào DI container

        // ===== Đăng ký Shipping Service (GHN) =====
        // Bind section "GHN" từ appsettings.json vào class GhnSettings
        services.Configure<GhnSettings>(configuration.GetSection(GhnSettings.SectionName));

        // Cấu hình Named HttpClient "GHN" với BaseAddress và headers xác thực
        var ghnSettings = configuration.GetSection(GhnSettings.SectionName).Get<GhnSettings>();
        services.AddHttpClient("GHN", client =>
        {
            client.BaseAddress = new Uri(ghnSettings?.BaseUrl ?? "https://online-gateway.ghn.vn"); // URL gốc GHN API
            client.DefaultRequestHeaders.Add("Token", ghnSettings?.Token ?? "");                   // Token xác thực GHN
            client.DefaultRequestHeaders.Add("ShopId", ghnSettings?.ShopId.ToString() ?? "");      // Mã shop trên GHN
            client.Timeout = TimeSpan.FromSeconds(30);                                             // Timeout 30s cho mỗi request
        });

        services.AddScoped<IShippingService, GhnShippingService>(); // Đăng ký ShippingService vào DI container

        return services;
    }
}
