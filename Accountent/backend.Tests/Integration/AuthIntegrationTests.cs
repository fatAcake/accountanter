using backend.Abstractions.Services;
using backend.Data;
using backend.Models.DTOs;
using backend.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace backend.Tests.Integration;

public class AuthIntegrationTests : IClassFixture<AccountingTestWebApplicationFactory>
{
    private readonly AccountingTestWebApplicationFactory _factory;

    public AuthIntegrationTests(AccountingTestWebApplicationFactory factory) =>
        _factory = factory;

    [Theory]
    [InlineData(AccountingTestDataSeeder.AdminEmail)]
    [InlineData(AccountingTestDataSeeder.AccountantEmail)]
    [InlineData(AccountingTestDataSeeder.ObserverEmail)]
    public async Task Login_через_AuthService_успешен(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var result = await auth.LoginAsync(new LoginRequest
        {
            email = email,
            password = DatabaseSeed.DemoPassword,
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.False(string.IsNullOrWhiteSpace(result.Data!.jwt_token));
    }
}
