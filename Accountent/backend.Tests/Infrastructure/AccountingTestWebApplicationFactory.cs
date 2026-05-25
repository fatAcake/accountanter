using System.Security.Claims;
using System.Text;
using backend.Abstractions.Data;
using backend.Configuration;
using backend.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace backend.Tests.Infrastructure;

public sealed class AccountingTestWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string JwtSecret = "integration-test-jwt-secret-key-32chars-min";

    static AccountingTestWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("Jwt__Secret", JwtSecret);
        Environment.SetEnvironmentVariable("Jwt__Issuer", "AccountentTest");
        Environment.SetEnvironmentVariable("Jwt__Audience", "AccountentTest");
    }

    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var dbName = $"accountent_tests_{Guid.NewGuid():N}";
        _connection = new SqliteConnection($"Data Source={dbName};Mode=Memory;Cache=Shared");
        _connection.Open();

        builder.UseSetting(WebHostDefaults.EnvironmentKey, "Testing");
        builder.UseSetting("Testing:UseSqlite", "true");
        builder.UseSetting("Testing:SkipDatabaseBootstrap", "true");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _connection.ConnectionString);
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Testing:SkipDatabaseBootstrap"] = "true",
                ["Testing:UseSqlite"] = "true",
                ["ConnectionStrings:DefaultConnection"] = _connection!.ConnectionString,
                [$"{JwtSettings.SectionName}:Secret"] = JwtSecret,
                [$"{JwtSettings.SectionName}:Issuer"] = "AccountentTest",
                [$"{JwtSettings.SectionName}:Audience"] = "AccountentTest",
                [$"{JwtSettings.SectionName}:AccessTokenMinutes"] = "60",
                [$"{JwtSettings.SectionName}:RefreshTokenDays"] = "7",
            });
        });

        builder.ConfigureServices(services =>
        {
            foreach (var descriptor in services
                         .Where(d => d.ServiceType == typeof(IDbContextFactory<ApplicationDbContext>))
                         .ToList())
            {
                services.Remove(descriptor);
            }

            services.AddDbContextFactory<ApplicationDbContext>(options => options.UseSqlite(_connection));

            services.PostConfigure<JwtSettings>(settings =>
            {
                settings.Secret = JwtSecret;
                settings.Issuer = "AccountentTest";
                settings.Audience = "AccountentTest";
            });

            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
                options.TokenValidationParameters.ValidIssuer = "AccountentTest";
                options.TokenValidationParameters.ValidAudience = "AccountentTest";
                options.TokenValidationParameters.IssuerSigningKey = key;
                options.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;
            });
        });
    }

    private readonly object _seedLock = new();
    private bool _seeded;

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        lock (_seedLock)
        {
            if (!_seeded)
            {
                SeedDatabase(host.Services);
                _seeded = true;
            }
        }

        return host;
    }

    private static void SeedDatabase(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDatabaseContextFactory>().CreateDbContext();
        db.Database.EnsureCreated();
        AccountingTestDataSeeder.SeedAsync(db).GetAwaiter().GetResult();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection?.Dispose();
    }
}
