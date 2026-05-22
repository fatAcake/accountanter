using backend.Abstractions.Common;

namespace backend.Services.Common
{
    public sealed class SaldoCalculationService : ISaldoCalculationService
    {
        public (decimal debit, decimal credit) SplitSaldo(decimal debitTurnover, decimal creditTurnover)
        {
            var net = debitTurnover - creditTurnover;
            return net >= 0 ? (net, 0m) : (0m, -net);
        }
    }
}
