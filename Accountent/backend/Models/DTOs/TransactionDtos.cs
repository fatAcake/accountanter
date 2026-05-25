using System.ComponentModel.DataAnnotations;
using backend.Models;

namespace backend.Models.DTOs
{
    public class TransactionLineRequest
    {
        [Required]
        public int account_id { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal amount { get; set; }

        [Required]
        public EntrySide side { get; set; }

        public int? counterparty_id { get; set; }
    }

    public class CreateTransactionRequest
    {
        [Required]
        public DateTime date { get; set; }

        public int? debit_account_id { get; set; }

        public int? credit_account_id { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? amount { get; set; }

        public int? counterparty_id { get; set; }

        [StringLength(1000, MinimumLength = 1)]
        [Required]
        public string description { get; set; } = string.Empty;

        public List<TransactionLineRequest>? lines { get; set; }
    }

    public class UpdateTransactionRequest : CreateTransactionRequest
    {
    }

    public class TransactionFilter
    {
        public DateTime? start_date { get; set; }
        public DateTime? end_date { get; set; }
        public int? account_id { get; set; }
        public int? counterparty_id { get; set; }
    }

    public class AccountBrief
    {
        public int id { get; set; }
        public string number { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
    }

    public class CounterpartyBrief
    {
        public int id { get; set; }
        public string name { get; set; } = string.Empty;
        public string? inn { get; set; }
    }

    public class TransactionLineResponse
    {
        public int id { get; set; }
        public int account_id { get; set; }
        public AccountBrief account { get; set; } = new();
        public decimal amount { get; set; }
        public EntrySide side { get; set; }
        public int? counterparty_id { get; set; }
        public CounterpartyBrief? counterparty { get; set; }
    }

    public class ImportTransactionRowRequest
    {
        public int line { get; set; }
        public string date { get; set; } = string.Empty;
        public string debit_number { get; set; } = string.Empty;
        public string credit_number { get; set; } = string.Empty;
        public decimal amount { get; set; }
        public string description { get; set; } = string.Empty;
        public string? counterparty_inn { get; set; }
        public string? counterparty_name { get; set; }
    }

    public class ImportTransactionsRequest
    {
        public List<ImportTransactionRowRequest> rows { get; set; } = [];
    }

    public class ImportTransactionRowError
    {
        public int line { get; set; }
        public string message { get; set; } = string.Empty;
    }

    public class ImportTransactionsResult
    {
        public int created { get; set; }
        public int skipped { get; set; }
        public List<ImportTransactionRowError> errors { get; set; } = [];
    }

    public class TransactionResponse
    {
        public int id { get; set; }
        public DateTime date { get; set; }
        public decimal amount { get; set; }
        public string description { get; set; } = string.Empty;
        public bool is_complex { get; set; }
        public bool is_balanced { get; set; }
        public decimal debit_total { get; set; }
        public decimal credit_total { get; set; }
        public int? debit_account_id { get; set; }
        public AccountBrief? debit_account { get; set; }
        public int? credit_account_id { get; set; }
        public AccountBrief? credit_account { get; set; }
        public int? counterparty_id { get; set; }
        public CounterpartyBrief? counterparty { get; set; }
        public List<TransactionLineResponse> lines { get; set; } = new();
    }
}
