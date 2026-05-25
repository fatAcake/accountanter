using backend.Services.Common;
using Xunit;

namespace backend.Tests.Unit;

public class SaldoCalculationServiceTests
{
    private readonly SaldoCalculationService _service = new();

    public static TheoryData<decimal, decimal, decimal, decimal> SplitSaldoCases =>
        new()
        {
            { 1_000m, 400m, 600m, 0m },
            { 400m, 1_000m, 0m, 600m },
            { 500m, 500m, 0m, 0m },
            { 0m, 0m, 0m, 0m },
            { 99_999_999.99m, 0.01m, 99_999_999.98m, 0m },
        };

    [Theory]
    [MemberData(nameof(SplitSaldoCases))]
    public void SplitSaldo_раскладывает_чистое_сальдо_на_дебет_или_кредит(
        decimal debitTurnover,
        decimal creditTurnover,
        decimal expectedDebit,
        decimal expectedCredit)
    {
        var (debit, credit) = _service.SplitSaldo(debitTurnover, creditTurnover);

        Assert.Equal(expectedDebit, debit);
        Assert.Equal(expectedCredit, credit);
    }

    [Fact]
    public void SplitSaldo_при_дебетовом_превышении_не_даёт_кредитовое_сальдо()
    {
        var (debit, credit) = _service.SplitSaldo(10_000m, 2_500m);

        Assert.Equal(7_500m, debit);
        Assert.Equal(0m, credit);
    }

    [Fact]
    public void SplitSaldo_при_кредитовом_превышении_не_даёт_дебетовое_сальдо()
    {
        var (debit, credit) = _service.SplitSaldo(100m, 10_100m);

        Assert.Equal(0m, debit);
        Assert.Equal(10_000m, credit);
    }
}
