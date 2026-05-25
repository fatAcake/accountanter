using System.Net.Http.Json;
using backend.Abstractions.Common;
using backend.Abstractions.Data;
using backend.Data;
using backend.Models;
using backend.Models.DTOs;
using backend.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace backend.Tests.Integration;

public class DoubleEntryIntegrationTests : IClassFixture<AccountingTestWebApplicationFactory>
{
    private readonly AccountingTestWebApplicationFactory _factory;

    public DoubleEntryIntegrationTests(AccountingTestWebApplicationFactory factory) =>
        _factory = factory;

    [Fact]
    public async Task Каждая_проводка_в_БД_сбалансирована_по_дебету_и_кредиту()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDatabaseContextFactory>().CreateDbContext();
        var validator = scope.ServiceProvider.GetRequiredService<IDoubleEntryValidationService>();

        var transactions = await db.Transactions
            .Include(t => t.lines)
            .Where(t => !t.deleted)
            .ToListAsync();

        Assert.NotEmpty(transactions);

        foreach (var transaction in transactions)
        {
            var error = validator.GetBalanceError(transaction.lines.ToList());
            Assert.Null(error);
        }
    }

    [Fact]
    public async Task Глобальный_баланс_дебета_и_кредита_совпадает_после_всех_операций()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDatabaseContextFactory>().CreateDbContext();

        var debitTotal = await SumDebitAsync(db);
        var creditTotal = await SumCreditAsync(db);

        Assert.Equal(debitTotal, creditTotal);
        Assert.True(debitTotal >= 18_000m);
    }

    [Fact]
    public async Task Создание_несбалансированной_проводки_отклоняется()
    {
        var client = _factory.CreateClient();
        await client.AsRoleAsync(AccountingTestDataSeeder.AccountantEmail);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDatabaseContextFactory>().CreateDbContext();
        var accounts = await db.Accounts.AsNoTracking().ToDictionaryAsync(a => a.number);

        var request = new CreateTransactionRequest
        {
            date = new DateTime(2026, 1, 25, 12, 0, 0, DateTimeKind.Utc),
            description = "Несбалансированная проводка",
            lines =
            [
                new TransactionLineRequest
                {
                    account_id = accounts["51.01"].id,
                    amount = 1_000m,
                    side = EntrySide.debit,
                },
                new TransactionLineRequest
                {
                    account_id = accounts["62.01"].id,
                    amount = 900m,
                    side = EntrySide.credit,
                },
            ],
        };

        var response = await client.PostAsJsonAsync("/api/transactions", request);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("двойной записи", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task После_создания_проводки_глобальный_баланс_сохраняется()
    {
        var client = _factory.CreateClient();
        await client.AsRoleAsync(AccountingTestDataSeeder.AccountantEmail);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDatabaseContextFactory>().CreateDbContext();
        var accounts = await db.Accounts.AsNoTracking().ToDictionaryAsync(a => a.number);

        var beforeDebit = await SumDebitAsync(db);
        var beforeCredit = await SumCreditAsync(db);
        const decimal amount = 2_000m;

        var request = new CreateTransactionRequest
        {
            date = new DateTime(2026, 1, 26, 12, 0, 0, DateTimeKind.Utc),
            description = "Оплата поставщику",
            debit_account_id = accounts["60.01"].id,
            credit_account_id = accounts["51.01"].id,
            amount = amount,
        };

        var response = await client.PostAsJsonAsync("/api/transactions", request);
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);

        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider
            .GetRequiredService<IDatabaseContextFactory>()
            .CreateDbContext();
        var afterDebit = await SumDebitAsync(verifyDb);
        var afterCredit = await SumCreditAsync(verifyDb);

        Assert.Equal(beforeDebit + amount, afterDebit);
        Assert.Equal(beforeCredit + amount, afterCredit);
        Assert.Equal(afterDebit, afterCredit);
    }

    private static async Task<decimal> SumDebitAsync(ApplicationDbContext db)
    {
        var amounts = await db.TransactionLines
            .Where(l => !l.transaction.deleted && l.side == EntrySide.debit)
            .Select(l => l.amount)
            .ToListAsync();
        return amounts.Sum();
    }

    private static async Task<decimal> SumCreditAsync(ApplicationDbContext db)
    {
        var amounts = await db.TransactionLines
            .Where(l => !l.transaction.deleted && l.side == EntrySide.credit)
            .Select(l => l.amount)
            .ToListAsync();
        return amounts.Sum();
    }
}
