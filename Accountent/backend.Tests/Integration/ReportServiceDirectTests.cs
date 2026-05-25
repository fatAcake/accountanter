using backend.Abstractions.Services;
using backend.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Sdk;

namespace backend.Tests.Integration;

public class ReportServiceDirectTests : IClassFixture<AccountingTestWebApplicationFactory>
{
    private readonly AccountingTestWebApplicationFactory _factory;

    public ReportServiceDirectTests(AccountingTestWebApplicationFactory factory) =>
        _factory = factory;

    [Fact]
    public async Task GetOsvAsync_напрямую_без_ошибок()
    {
        using var scope = _factory.Services.CreateScope();
        var reports = scope.ServiceProvider.GetRequiredService<IReportService>();

        try
        {
            var result = await reports.GetOsvAsync(
                AccountingTestDataSeeder.PeriodStart,
                AccountingTestDataSeeder.PeriodEnd);

            if (!result.IsSuccess)
                throw new XunitException($"Service error: {result.Error?.Code} {result.Error?.Message}");

            Assert.NotEmpty(result.Data!.rows);
        }
        catch (Exception ex) when (ex is not XunitException)
        {
            throw new XunitException(ex.ToString());
        }
    }
}
