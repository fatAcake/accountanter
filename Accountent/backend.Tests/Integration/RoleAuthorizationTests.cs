using System.Net;
using System.Net.Http.Json;
using backend.Data;
using backend.Models;
using backend.Models.DTOs;
using backend.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace backend.Tests.Integration;

public class RoleAuthorizationTests : IClassFixture<AccountingTestWebApplicationFactory>
{
    private readonly AccountingTestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RoleAuthorizationTests(AccountingTestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData(AccountingTestDataSeeder.AdminEmail, HttpStatusCode.OK)]
    [InlineData(AccountingTestDataSeeder.AccountantEmail, HttpStatusCode.OK)]
    [InlineData(AccountingTestDataSeeder.ObserverEmail, HttpStatusCode.OK)]
    public async Task Чтение_проводок_доступно_всем_ролям(string email, HttpStatusCode expected)
    {
        await _client.AsRoleAsync(email);
        var response = await _client.GetAsync("/api/transactions");
        Assert.Equal(expected, response.StatusCode);
    }

    [Theory]
    [InlineData(AccountingTestDataSeeder.AdminEmail, HttpStatusCode.Created)]
    [InlineData(AccountingTestDataSeeder.AccountantEmail, HttpStatusCode.Created)]
    [InlineData(AccountingTestDataSeeder.ObserverEmail, HttpStatusCode.Forbidden)]
    public async Task Создание_проводки_только_admin_и_accountant(
        string email,
        HttpStatusCode expected)
    {
        await _client.AsRoleAsync(email);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<backend.Abstractions.Data.IDatabaseContextFactory>()
            .CreateDbContext();
        var accounts = await db.Accounts.AsNoTracking().ToDictionaryAsync(a => a.number);

        var request = new CreateTransactionRequest
        {
            date = new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc),
            description = "Тест прав доступа",
            debit_account_id = accounts["26"].id,
            credit_account_id = accounts["51.01"].id,
            amount = 100m,
        };

        var response = await _client.PostAsJsonAsync("/api/transactions", request);
        Assert.Equal(expected, response.StatusCode);
    }

    [Theory]
    [InlineData(AccountingTestDataSeeder.AdminEmail, HttpStatusCode.NoContent)]
    [InlineData(AccountingTestDataSeeder.AccountantEmail, HttpStatusCode.NoContent)]
    [InlineData(AccountingTestDataSeeder.ObserverEmail, HttpStatusCode.Forbidden)]
    public async Task Удаление_проводки_только_admin_и_accountant(
        string email,
        HttpStatusCode expected)
    {
        await _client.AsRoleAsync(AccountingTestDataSeeder.AccountantEmail);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<backend.Abstractions.Data.IDatabaseContextFactory>()
            .CreateDbContext();
        var accounts = await db.Accounts.AsNoTracking().ToDictionaryAsync(a => a.number);

        var createResponse = await _client.PostAsJsonAsync("/api/transactions", new CreateTransactionRequest
        {
            date = new DateTime(2026, 2, 2, 12, 0, 0, DateTimeKind.Utc),
            description = "На удаление",
            debit_account_id = accounts["26"].id,
            credit_account_id = accounts["51.01"].id,
            amount = 50m,
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<TransactionResponse>(TestJsonOptions.Default)
            ?? throw new InvalidOperationException("Пустой ответ создания проводки.");

        await _client.AsRoleAsync(email);
        var deleteResponse = await _client.DeleteAsync($"/api/transactions/{created.id}");
        Assert.Equal(expected, deleteResponse.StatusCode);
    }

    [Theory]
    [InlineData(AccountingTestDataSeeder.AdminEmail, HttpStatusCode.OK)]
    [InlineData(AccountingTestDataSeeder.AccountantEmail, HttpStatusCode.Forbidden)]
    [InlineData(AccountingTestDataSeeder.ObserverEmail, HttpStatusCode.Forbidden)]
    public async Task Управление_пользователями_только_admin(
        string email,
        HttpStatusCode expected)
    {
        await _client.AsRoleAsync(email);
        var response = await _client.GetAsync("/api/users");
        Assert.Equal(expected, response.StatusCode);
    }

    [Theory]
    [InlineData(AccountingTestDataSeeder.AdminEmail, HttpStatusCode.Created)]
    [InlineData(AccountingTestDataSeeder.AccountantEmail, HttpStatusCode.Forbidden)]
    [InlineData(AccountingTestDataSeeder.ObserverEmail, HttpStatusCode.Forbidden)]
    public async Task Создание_пользователя_только_admin(
        string email,
        HttpStatusCode expected)
    {
        await _client.AsRoleAsync(email);

        var request = new CreateUserRequest
        {
            nickname = "Новый пользователь",
            email = $"user-{Guid.NewGuid():N}{DatabaseSeed.DemoEmailDomain}",
            password = DatabaseSeed.DemoPassword,
            role = Roles.observer,
        };

        var response = await _client.PostAsJsonAsync("/api/users", request);
        Assert.Equal(expected, response.StatusCode);
    }

    [Theory]
    [InlineData(AccountingTestDataSeeder.AdminEmail, HttpStatusCode.OK)]
    [InlineData(AccountingTestDataSeeder.AccountantEmail, HttpStatusCode.OK)]
    [InlineData(AccountingTestDataSeeder.ObserverEmail, HttpStatusCode.OK)]
    public async Task ОСВ_доступна_всем_авторизованным_ролям(string email, HttpStatusCode expected)
    {
        await _client.AsRoleAsync(email);
        var response = await _client.GetAsync(
            $"/api/reports/OSV?start_date={AccountingTestDataSeeder.PeriodStart:O}" +
            $"&end_date={AccountingTestDataSeeder.PeriodEnd:O}");
        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task Без_токена_запросы_к_защищенным_эндпоинтам_возвращают_401()
    {
        _client.WithoutAuth();

        var transactions = await _client.GetAsync("/api/transactions");
        var osv = await _client.GetAsync(
            $"/api/reports/OSV?start_date={AccountingTestDataSeeder.PeriodStart:O}" +
            $"&end_date={AccountingTestDataSeeder.PeriodEnd:O}");
        var users = await _client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Unauthorized, transactions.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, osv.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, users.StatusCode);
    }
}
