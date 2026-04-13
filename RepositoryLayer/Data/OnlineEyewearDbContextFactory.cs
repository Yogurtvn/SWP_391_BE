using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace RepositoryLayer.Data;

public class OnlineEyewearDbContextFactory : IDesignTimeDbContextFactory<OnlineEyewearDbContext>
{
    public OnlineEyewearDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var basePath = ResolveBasePath();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=.;Database=OnlineEyewearDB;Trusted_Connection=True;TrustServerCertificate=True;";

        var optionsBuilder = new DbContextOptionsBuilder<OnlineEyewearDbContext>();
        optionsBuilder.UseSqlServer(
            connectionString,
            sqlOptions => sqlOptions.MigrationsAssembly(typeof(OnlineEyewearDbContext).Assembly.FullName));

        return new OnlineEyewearDbContext(optionsBuilder.Options);
    }

    private static string ResolveBasePath()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var controllerPath = Path.GetFullPath(Path.Combine(currentDirectory, "..", "ControllerLayer"));

        return File.Exists(Path.Combine(controllerPath, "appsettings.json"))
            ? controllerPath
            : currentDirectory;
    }
}
