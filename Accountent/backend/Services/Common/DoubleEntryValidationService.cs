using backend.Abstractions.Common;
using backend.Models;
using backend.Services.Common.Models;

namespace backend.Services.Common
{
    public sealed class DoubleEntryValidationService : IDoubleEntryValidationService
    {
        private readonly ILogger<DoubleEntryValidationService> _logger;

        public DoubleEntryValidationService(ILogger<DoubleEntryValidationService> logger)
        {
            _logger = logger;
        }

        public DoubleEntryValidationResult Validate(IReadOnlyList<TransactionLine> lines)
        {
            if (lines.Count < 2)
                return DoubleEntryValidationResult.Fail(
                    "Проводка должна содержать минимум две строки (дебет и кредит).");

            var debits = lines.Where(l => l.side == EntrySide.debit).ToList();
            var credits = lines.Where(l => l.side == EntrySide.credit).ToList();

            if (debits.Count == 0 || credits.Count == 0)
                return DoubleEntryValidationResult.Fail(
                    "Двойная запись требует хотя бы одну строку по дебету и одну по кредиту.");

            foreach (var line in lines)
            {
                if (line.amount <= 0)
                    return DoubleEntryValidationResult.Fail("Сумма в каждой строке должна быть больше нуля.");
            }

            var debitTotal = debits.Sum(l => l.amount);
            var creditTotal = credits.Sum(l => l.amount);

            if (debitTotal != creditTotal)
            {
                _logger.LogWarning(
                    "Нарушение двойной записи: дебет {Debit}, кредит {Credit}",
                    debitTotal, creditTotal);
                return DoubleEntryValidationResult.Fail(
                    $"Нарушение двойной записи: сумма дебета ({debitTotal:N2}) не равна сумме кредита ({creditTotal:N2}).");
            }

            return DoubleEntryValidationResult.Ok(debitTotal, creditTotal);
        }

        public string? GetBalanceError(IReadOnlyList<TransactionLine> lines) =>
            Validate(lines).error;
    }
}
