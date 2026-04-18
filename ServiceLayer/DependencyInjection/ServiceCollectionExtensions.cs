using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RepositoryLayer.DependencyInjection;
using ServiceLayer.Contracts.Auth;
using ServiceLayer.Contracts.Cart;
using ServiceLayer.Contracts.CatalogSupport;
using ServiceLayer.Contracts.Category;
using ServiceLayer.Contracts.UserManagement;
using ServiceLayer.Contracts.Inventory;
using ServiceLayer.Contracts.LensType;
using ServiceLayer.Contracts.Orders;
using ServiceLayer.Contracts.Payment;
using ServiceLayer.Contracts.Policy;
using ServiceLayer.Contracts.Prescription;
using ServiceLayer.Contracts.Product;
using ServiceLayer.Contracts.ProductImage;
using ServiceLayer.Contracts.ProductVariant;
using ServiceLayer.Contracts.Report;
using ServiceLayer.Contracts.Security;
using ServiceLayer.Contracts.Shipping;
using ServiceLayer.Contracts.StockReceipt;
using ServiceLayer.DTOs.Shipping;
using ServiceLayer.Security;
using ServiceLayer.Services.Auth;
using ServiceLayer.Services.CartManagement;
using ServiceLayer.Services.CatalogSupport;
using ServiceLayer.Services.CategoryManagement;
using ServiceLayer.Services.UserManagement;
using ServiceLayer.Services.InventoryManagement;
using ServiceLayer.Services.LensTypeManagement;
using ServiceLayer.Services.Orders;
using ServiceLayer.Services.PaymentManagement;
using ServiceLayer.Services.Policy;
using ServiceLayer.Services.PrescriptionManagement;
using ServiceLayer.Services.ProductImageManagement;
using ServiceLayer.Services.ProductManagement;
using ServiceLayer.Services.ProductVariantManagement;
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
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<ICatalogSupportService, CatalogSupportService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IPayOsGatewayClient, PayOsGatewayClient>();
        services.AddScoped<IPrescriptionService, PrescriptionService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ILensTypeService, LensTypeService>();
        services.AddScoped<IPolicyService, PolicyService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IProductVariantService, ProductVariantService>();
        services.AddScoped<IProductImageService, ProductImageService>();
        services.AddScoped<IInventoryService, InventoryService>(); // Đăng ký InventoryService vào DI container
        services.AddScoped<IStockReceiptService, StockReceiptService>(); // Đăng ký StockReceiptService vào DI container
        services.AddScoped<IReportService, ReportService>(); // Đăng ký ReportService vào DI container
        services.AddScoped<IUserService, UserService>(); // Đăng ký UserService vào DI container
        services.AddScoped<ICategoryService, CategoryService>(); // Đăng ký CategoryService vào DI container

        // GHN Shipping Integration
        services.Configure<GhnSettings>(configuration.GetSection(GhnSettings.SectionName));
        services.AddHttpClient("GHN", (sp, client) =>
        {
            var settings = configuration.GetSection(GhnSettings.SectionName).Get<GhnSettings>();
            if (settings != null)
            {
                client.BaseAddress = new Uri(settings.BaseUrl);
                client.DefaultRequestHeaders.Add("Token", settings.Token);
                // Một số API GHN yêu cầu ShopId trong header (tùy phiên bản)
                client.DefaultRequestHeaders.Add("ShopId", settings.ShopId.ToString());
            }
        });
        services.AddScoped<IShippingService, GhnShippingService>();

        return services;
    }
}
