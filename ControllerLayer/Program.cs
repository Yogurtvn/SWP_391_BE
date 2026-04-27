using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RepositoryLayer.Data;
using ServiceLayer.Contracts.Auth;
using ServiceLayer.DependencyInjection;
using System.Globalization;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
// Lấy các cấu hình JWT từ appsettings.json
var jwtIssuer = GetRequiredConfigurationValue(builder.Configuration, "Jwt:Issuer");
var jwtAudience = GetRequiredConfigurationValue(builder.Configuration, "Jwt:Audience");
var jwtKey = GetRequiredConfigurationValue(builder.Configuration, "Jwt:Key");
var corsAllowedOrigins = GetCorsAllowedOrigins(builder.Configuration);
var swaggerEnabled = builder.Configuration.GetValue<bool>("Swagger:Enabled");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
// Cấu hình CORS để cho phép Frontend truy cập API
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(corsAllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
// Cấu hình Swagger để kiểm thử API
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => type.FullName!.Replace("+", "."));

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter a JWT access token. The Bearer prefix is added automatically."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
// Đăng ký các dịch vụ của ServiceLayer vào Dependency Injection
builder.Services.AddServiceLayer(builder.Configuration);
// Cấu hình Authentication sử dụng JWT Bearer
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            if (!TryGetAuthenticatedUserId(context.Principal, out var userId)
                || !TryGetTokenVersion(context.Principal, out var tokenVersion))
            {
                context.Fail("The token is missing required claims.");
                return;
            }

            var dbContext = context.HttpContext.RequestServices.GetRequiredService<OnlineEyewearDbContext>();
            var user = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(currentUser => currentUser.UserId == userId, context.HttpContext.RequestAborted);

            if (user is null || !user.IsActive)
            {
                context.Fail("The user is no longer active.");
                return;
            }

            if (user.TokenVersion != tokenVersion)
            {
                context.Fail("The token has been revoked.");
            }
        }
    };
});
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

var app = builder.Build();
var shouldEnableSwagger = app.Environment.IsDevelopment() || swaggerEnabled;

// Apply pending migrations automatically on startup.
// comment beacause error when deploy to Azure App Service, because the user doesn't have permission to create migration history table in the database
//using (var scope = app.Services.CreateScope())
//{
//    var dbContext = scope.ServiceProvider.GetRequiredService<OnlineEyewearDbContext>();
//    dbContext.Database.Migrate();
//}

if (shouldEnableSwagger)
{
    app.UseSwaggerUI();
}

app.UseStaticFiles();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (shouldEnableSwagger)
{
    app.MapSwagger().AllowAnonymous();
    app.MapGet("/", () => Results.Redirect("/swagger")).AllowAnonymous();
}

app.Run();

static string GetRequiredConfigurationValue(IConfiguration configuration, string key)
{
    var value = configuration[key];

    if (string.IsNullOrWhiteSpace(value) || value.StartsWith("__SET_", StringComparison.Ordinal))
    {
        throw new InvalidOperationException($"Configuration value '{key}' is missing or still using a placeholder.");
    }

    return value;
}

static string[] GetCorsAllowedOrigins(IConfiguration configuration)
{
    var configuredOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

    if (configuredOrigins is { Length: > 0 })
    {
        return configuredOrigins;
    }

    return
    [
        "http://localhost:5173",
        "https://localhost:5173",
        "http://127.0.0.1:5173",
        "https://127.0.0.1:5173"
    ];
}

static bool TryGetAuthenticatedUserId(ClaimsPrincipal? principal, out int userId)
{
    var userIdClaim = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    return int.TryParse(userIdClaim, out userId);
}

static bool TryGetTokenVersion(ClaimsPrincipal? principal, out int tokenVersion)
{
    var rawTokenVersion = principal?.FindFirst(TokenClaimNames.TokenVersion)?.Value;

    if (string.IsNullOrWhiteSpace(rawTokenVersion))
    {
        tokenVersion = 0;
        return true;
    }

    return int.TryParse(rawTokenVersion, NumberStyles.None, CultureInfo.InvariantCulture, out tokenVersion)
        && tokenVersion >= 0;
}
