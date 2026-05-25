namespace backend.Services.Common.Models
{
    public class DoubleEntryValidationResult
    {
        public bool is_balanced { get; set; }
        public decimal debit_total { get; set; }
        public decimal credit_total { get; set; }
        public string? error { get; set; }

        public static DoubleEntryValidationResult Ok(decimal debit, decimal credit) => new()
        {
            is_balanced = true,
            debit_total = debit,
            credit_total = credit,
        };

        public static DoubleEntryValidationResult Fail(string message) => new()
        {
            is_balanced = false,
            error = message,
        };
    }
}
