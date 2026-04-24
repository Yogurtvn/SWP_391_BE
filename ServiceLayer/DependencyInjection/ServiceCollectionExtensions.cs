using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RepositoryLayer.DependencyInjection;
using ServiceLayer.Configuration;
using ServiceLayer.Contracts.Email;
using ServiceLayer.Contracts.Auth;
using ServiceLayer.Contracts.Cart;
using ServiceLayer.Contracts.CatalogSupport;
using ServiceLayer.Contracts.Category;
using ServiceLayer.Contracts.Inventory;
using ServiceLayer.Contracts.LensType;
using ServiceLayer.Contracts.Notifications;
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
using ServiceLayer.Contracts.Storage;
using ServiceLayer.Contracts.UserManagement;
using ServiceLayer.Contracts.Catalog;
using ServiceLayer.DTOs.Shipping;
using ServiceLayer.Security;
using ServiceLayer.Services.Auth;
using ServiceLayer.Services.CartManagement;
using ServiceLayer.Services.CatalogSupport;
using ServiceLayer.Services.Catalog;
using ServiceLayer.Services.CategoryManagement;
using ServiceLayer.Services.Email;
using ServiceLayer.Services.InventoryManagement;
using ServiceLayer.Services.LensTypeManagement;
using ServiceLayer.Services.Notifications;
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
using ServiceLayer.Services.Storage;
using ServiceLayer.Services.UserManagement;

namespace ServiceLayer.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServiceLayer(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRepositoryLayer(configuration);
        services.AddHttpClient();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<ICatalogSupportService, CatalogSupportService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IPayOsGatewayClient, PayOsGatewayClient>();
        services.AddScoped<IPrescriptionService, PrescriptionService>();
        services.AddScoped<IPrescriptionPricingService, PrescriptionPricingService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ILensTypeService, LensTypeService>();
        services.AddScoped<IPolicyService, PolicyService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IPromotionService, PromotionService>();
        services.AddScoped<IProductVariantService, ProductVariantService>();
        services.AddScoped<IProductImageService, ProductImageService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IStockReceiptService, StockReceiptService>();
        services.AddScoped<IPreOrderBackInStockNotificationService, PreOrderBackInStockNotificationService>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICategoryService, CategoryService>();

        services.Configure<CloudinaryOptions>(configuration.GetSection(CloudinaryOptions.SectionName));
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.Configure<PrescriptionPricingOptions>(configuration.GetSection(PrescriptionPricingOptions.SectionName));
        services.PostConfigure<CloudinaryOptions>(options =>
        {
            options.CloudName = ResolveConfigOverride(
                options.CloudName,
                "Cloudinary__CloudName",
                "CLOUDINARY_CLOUD_NAME");
            options.ApiKey = ResolveConfigOverride(
                options.ApiKey,
                "Cloudinary__ApiKey",
                "CLOUDINARY_API_KEY");
            options.ApiSecret = ResolveConfigOverride(
                options.ApiSecret,
                "Cloudinary__ApiSecret",
                "CLOUDINARY_API_SECRET");
        });
        services.AddSingleton<IImageStorageService, CloudinaryImageStorageService>();

        services.Configure<GhnSettings>(configuration.GetSection(GhnSettings.SectionName));
        services.AddHttpClient("GHN", (_, client) =>
        {
            var settings = configuration.GetSection(GhnSettings.SectionName).Get<GhnSettings>();
            if (settings is null)
            {
                return;
            }

            client.BaseAddress = new Uri(settings.BaseUrl);
            client.DefaultRequestHeaders.Add("Token", settings.Token);
            client.DefaultRequestHeaders.Add("ShopId", settings.ShopId.ToString());
        });
        services.AddScoped<IShippingService, GhnShippingService>();

        return services;
    }

    private static string ResolveConfigOverride(string currentValue, params string[] environmentVariableNames)
    {
        foreach (var environmentVariableName in environmentVariableNames)
        {
            var overrideValue = Environment.GetEnvironmentVariable(environmentVariableName);
            if (!string.IsNullOrWhiteSpace(overrideValue))
            {
                return overrideValue.Trim();
            }
        }

        return currentValue;
    }
}
