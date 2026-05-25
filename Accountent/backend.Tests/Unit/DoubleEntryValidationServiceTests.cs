using backend.Models;
using backend.Services.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace backend.Tests.Unit;

public class DoubleEntryValidationServiceTests
{
    private readonly DoubleEntryValidationService _service =
        new(NullLogger<DoubleEntryValidationService>.Instance);

    [Fact]
    public void Validate_сбалансированная_простая_проводка_успешна()
    {
        var lines = new[]
        {
            TestLines.Debit(1_000m),
            TestLines.Credit(1_000m),
        };

        var result = _service.Validate(lines);

        Assert.True(result.is_balanced);
        Assert.Null(result.error);
        Assert.Equal(1_000m, result.debit_total);
        Assert.Equal(1_000m, result.credit_total);
    }

    [Fact]
    public void Validate_сбалансированная_сложная_проводка_суммирует_стороны()
    {
        var lines = new[]
        {
            TestLines.Debit(600m),
            TestLines.Debit(400m),
            TestLines.Credit(1_000m),
        };

        var result = _service.Validate(lines);

        Assert.True(result.is_balanced);
        Assert.Equal(1_000m, result.debit_total);
        Assert.Equal(1_000m, result.credit_total);
    }

    [Fact]
    public void Validate_меньше_двух_строк_отклоняется()
    {
        var result = _service.Validate([TestLines.Debit(100m)]);

        Assert.False(result.is_balanced);
        Assert.Contains("минимум две строки", result.error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_без_строки_кредита_отклоняется()
    {
        var lines = new[] { TestLines.Debit(100m), TestLines.Debit(100m) };
        var result = _service.Validate(lines);

        Assert.False(result.is_balanced);
        Assert.Contains("дебету и одну по кредиту", result.error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_без_строки_дебета_отклоняется()
    {
        var lines = new[] { TestLines.Credit(100m), TestLines.Credit(100m) };
        var result = _service.Validate(lines);

        Assert.False(result.is_balanced);
        Assert.Contains("дебету и одну по кредиту", result.error, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_неположительная_сумма_отклоняется(decimal amount)
    {
        var lines = new[] { TestLines.Debit(amount), TestLines.Credit(100m) };
        var result = _service.Validate(lines);

        Assert.False(result.is_balanced);
        Assert.Contains("больше нуля", result.error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_несбалансированные_суммы_возвращают_описание_расхождения()
    {
        var lines = new[] { TestLines.Debit(1_000m), TestLines.Credit(900m) };
        var result = _service.Validate(lines);

        Assert.False(result.is_balanced);
        Assert.Contains("1", result.error);
        Assert.Contains("900", result.error);
        Assert.Contains("двойной записи", result.error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetBalanceError_для_сбалансированной_проводки_возвращает_null()
    {
        var lines = new[] { TestLines.Debit(500m), TestLines.Credit(500m) };
        Assert.Null(_service.GetBalanceError(lines));
    }

    [Fact]
    public void GetBalanceError_для_несбалансированной_проводки_возвращает_текст()
    {
        var lines = new[] { TestLines.Debit(500m), TestLines.Credit(400m) };
        Assert.NotNull(_service.GetBalanceError(lines));
    }

    private static class TestLines
    {
        public static TransactionLine Debit(decimal amount) => new()
        {
            account_id = 1,
            side = EntrySide.debit,
            amount = amount,
        };

        public static TransactionLine Credit(decimal amount) => new()
        {
            account_id = 2,
            side = EntrySide.credit,
            amount = amount,
        };
    }
}
