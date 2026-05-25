using System.Net;
using backend.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace backend.Tests.Integration;

public class SaldoRedirectIntegrationTests : IClassFixture<AccountingTestWebApplicationFactory>
{
    private readonly AccountingTestWebApplicationFactory _factory;

    public SaldoRedirectIntegrationTests(AccountingTestWebApplicationFactory factory) =>
        _factory = factory;

    [Fact]
    public async Task Устаревший_api_saldo_перенаправляет_на_reports_OSV()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        await client.AsRoleAsync(AccountingTestDataSeeder.AccountantEmail);

        var start = AccountingTestDataSeeder.PeriodStart.ToString("O");
        var end = AccountingTestDataSeeder.PeriodEnd.ToString("O");

        var response = await client.GetAsync(
            $"/api/saldo?start_date={start}&end_date={end}",
            HttpCompletionOption.ResponseHeadersRead);

        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);

        var location = response.Headers.Location?.ToString();
        Assert.NotNull(location);
        Assert.Contains("/api/reports/OSV", location, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("start_date=", location, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("end_date=", location, StringComparison.OrdinalIgnoreCase);
    }
}
