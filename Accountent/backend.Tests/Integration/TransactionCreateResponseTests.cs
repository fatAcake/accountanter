using System.Net;
using System.Net.Http.Json;
using backend.Abstractions.Data;
using backend.Models.DTOs;
using backend.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace backend.Tests.Integration;

public class TransactionCreateResponseTests : IClassFixture<AccountingTestWebApplicationFactory>
{
    private readonly AccountingTestWebApplicationFactory _factory;

    public TransactionCreateResponseTests(AccountingTestWebApplicationFactory factory) =>
        _factory = factory;

    [Fact]
    public async Task Создание_проводки_возвращает_полный_DTO_с_счетами_в_одном_ответе()
    {
        var client = _factory.CreateClient();
        await client.AsRoleAsync(AccountingTestDataSeeder.AccountantEmail);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDatabaseContextFactory>().CreateDbContext();
        var accounts = await db.Accounts.AsNoTracking().ToDictionaryAsync(a => a.number);

        var request = new CreateTransactionRequest
        {
            date = new DateTime(2026, 1, 27, 10, 0, 0, DateTimeKind.Utc),
            description = "Тест ответа create",
            debit_account_id = accounts["60.01"].id,
            credit_account_id = accounts["51.01"].id,
            amount = 1_500m,
        };

        var response = await client.PostAsJsonAsync("/api/transactions", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var transaction = await response.Content.ReadFromJsonAsync<TransactionResponse>(TestJsonOptions.Default)
            ?? throw new InvalidOperationException("Пустой ответ создания проводки.");

        Assert.True(transaction.id > 0);
        Assert.True(transaction.is_balanced);
        Assert.Equal(1_500m, transaction.amount);
        Assert.NotNull(transaction.debit_account);
        Assert.NotNull(transaction.credit_account);
        Assert.Equal("60.01", transaction.debit_account!.number);
        Assert.Equal("51.01", transaction.credit_account!.number);
        Assert.Equal(2, transaction.lines.Count);
        Assert.All(transaction.lines, l => Assert.NotNull(l.account));
    }
}
