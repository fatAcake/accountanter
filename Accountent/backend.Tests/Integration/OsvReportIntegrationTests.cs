using System.Net.Http.Json;
using backend.Models.DTOs;
using backend.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace backend.Tests.Integration;

public class OsvReportIntegrationTests : IClassFixture<AccountingTestWebApplicationFactory>
{
    private readonly AccountingTestWebApplicationFactory _factory;

    public OsvReportIntegrationTests(AccountingTestWebApplicationFactory factory) =>
        _factory = factory;

    [Fact]
    public async Task ОСВ_на_тестовых_данных_январь_2026_формируется_корректно()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<backend.Abstractions.Data.IDatabaseContextFactory>()
                .CreateDbContext();
            await AccountingTestDataSeeder.EnsureFreshSeedAsync(db);
        }

        var expected = AccountingTestDataSeeder.ExpectedJanuary2026Osv();
        var client = _factory.CreateClient();
        await client.AsRoleAsync(AccountingTestDataSeeder.AccountantEmail);

        var url =
            $"/api/reports/OSV?start_date={AccountingTestDataSeeder.PeriodStart:O}" +
            $"&end_date={AccountingTestDataSeeder.PeriodEnd:O}";

        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var report = await response.Content.ReadFromJsonAsync<OsvReportResponse>(TestJsonOptions.Default)
            ?? throw new InvalidOperationException("Пустой ответ ОСВ.");

        Assert.Equal(expected.TotalTurnoverDebit, report.total_turnover_debit);
        Assert.Equal(expected.TotalTurnoverCredit, report.total_turnover_credit);
        Assert.Equal(expected.TotalClosingDebit, report.total_closing_debit);
        Assert.Equal(expected.TotalClosingCredit, report.total_closing_credit);

        var row51 = report.rows.Single(r => r.number == "51.01");
        Assert.Equal(expected.Account51TurnoverDebit, row51.turnover_debit);
        Assert.Equal(expected.Account51ClosingDebit, row51.closing_debit);
        Assert.Equal(0m, row51.closing_credit);

        var row62 = report.rows.Single(r => r.number == "62.01");
        Assert.Equal(expected.Account62TurnoverDebit, row62.turnover_debit);
        Assert.Equal(expected.Account62TurnoverCredit, row62.turnover_credit);
        Assert.Equal(expected.Account62ClosingCredit, row62.closing_credit);

        var row90 = report.rows.Single(r => r.number == "90.01");
        Assert.Equal(expected.Account90TurnoverCredit, row90.turnover_credit);
        Assert.Equal(expected.Account90ClosingCredit, row90.closing_credit);
    }

    [Fact]
    public async Task ОСВ_итоги_оборотов_и_сальдо_сбалансированы()
    {
        var client = _factory.CreateClient();
        await client.AsRoleAsync(AccountingTestDataSeeder.ObserverEmail);

        var response = await client.GetAsync(
            $"/api/reports/OSV?start_date={AccountingTestDataSeeder.PeriodStart:O}" +
            $"&end_date={AccountingTestDataSeeder.PeriodEnd:O}");

        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<OsvReportResponse>(TestJsonOptions.Default)
            ?? throw new InvalidOperationException("Пустой ответ ОСВ.");

        Assert.Equal(report.total_turnover_debit, report.total_turnover_credit);
        Assert.Equal(report.total_closing_debit, report.total_closing_credit);
        Assert.Equal(0m, report.total_opening_debit);
        Assert.Equal(0m, report.total_opening_credit);
    }

    [Fact]
    public async Task ОСВ_по_одному_счету_возвращает_только_запрошенный_счет()
    {
        var client = _factory.CreateClient();
        await client.AsRoleAsync(AccountingTestDataSeeder.AdminEmail);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<backend.Abstractions.Data.IDatabaseContextFactory>()
            .CreateDbContext();
        var accountId = db.Accounts.Single(a => a.number == "90.01").id;

        var response = await client.GetAsync(
            $"/api/reports/OSV?start_date={AccountingTestDataSeeder.PeriodStart:O}" +
            $"&end_date={AccountingTestDataSeeder.PeriodEnd:O}&account_id={accountId}");

        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<OsvReportResponse>(TestJsonOptions.Default)
            ?? throw new InvalidOperationException("Пустой ответ ОСВ.");

        Assert.Single(report.rows);
        Assert.Equal("90.01", report.rows[0].number);
        Assert.Equal(3_000m, report.rows[0].turnover_credit);
    }
}
