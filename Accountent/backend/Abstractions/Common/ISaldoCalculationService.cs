namespace backend.Abstractions.Common
{
    public interface ISaldoCalculationService
    {
        (decimal debit, decimal credit) SplitSaldo(decimal debitTurnover, decimal creditTurnover);
    }
}
