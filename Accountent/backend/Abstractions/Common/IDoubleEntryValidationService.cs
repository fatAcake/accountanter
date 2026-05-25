using backend.Models;
using backend.Services.Common.Models;

namespace backend.Abstractions.Common
{
    public interface IDoubleEntryValidationService
    {
        DoubleEntryValidationResult Validate(IReadOnlyList<TransactionLine> lines);
        string? GetBalanceError(IReadOnlyList<TransactionLine> lines);
    }
}
