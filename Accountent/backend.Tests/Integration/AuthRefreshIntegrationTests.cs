using System.Net;
using System.Net.Http.Json;
using backend.Abstractions.Data;
using backend.Data;
using backend.Models.DTOs;
using backend.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace backend.Tests.Integration;

public class AuthRefreshIntegrationTests : IClassFixture<AccountingTestWebApplicationFactory>
{
    private readonly AccountingTestWebApplicationFactory _factory;

    public AuthRefreshIntegrationTests(AccountingTestWebApplicationFactory factory) =>
        _factory = factory;

    [Fact]
    public async Task Refresh_с_валидным_токеном_возвращает_новую_пару_токенов()
    {
        using var client = _factory.CreateClient();

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            email = AccountingTestDataSeeder.AdminEmail,
            password = DatabaseSeed.DemoPassword,
        });
        login.EnsureSuccessStatusCode();
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>(TestJsonOptions.Default);
        Assert.NotNull(auth);

        var refresh = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            refresh_token = auth!.refresh_token,
        });

        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        var refreshed = await refresh.Content.ReadFromJsonAsync<AuthResponse>(TestJsonOptions.Default);
        Assert.NotNull(refreshed);
        Assert.False(string.IsNullOrWhiteSpace(refreshed!.jwt_token));
        Assert.NotEqual(auth.jwt_token, refreshed.jwt_token);
        Assert.NotEqual(auth.refresh_token, refreshed.refresh_token);
    }

    [Fact]
    public async Task Refresh_с_неверным_токеном_возвращает_401()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            refresh_token = "invalid-token",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_сохраняет_refresh_token_в_БД_как_bcrypt_хеш()
    {
        using var client = _factory.CreateClient();

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            email = AccountingTestDataSeeder.AccountantEmail,
            password = DatabaseSeed.DemoPassword,
        });
        login.EnsureSuccessStatusCode();
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>(TestJsonOptions.Default);
        Assert.NotNull(auth);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDatabaseContextFactory>().CreateDbContext();
        var stored = await db.Users
            .AsNoTracking()
            .Where(u => u.email == AccountingTestDataSeeder.AccountantEmail)
            .Select(u => u.refresh_token)
            .FirstAsync();

        Assert.NotNull(stored);
        Assert.StartsWith("$2", stored);
        Assert.NotEqual(auth!.refresh_token, stored);
        Assert.True(BCrypt.Net.BCrypt.Verify(auth.refresh_token, stored));
    }
}
