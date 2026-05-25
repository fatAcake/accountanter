using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    [Table("transactions")]
    public class Transaction
    {
        [Key]
        public int id { get; set; }

        [Column(TypeName = "timestamp with time zone")]
        [Required]
        public DateTime date { get; set; }

        public int? debit_account_id { get; set; }

        [ForeignKey(nameof(debit_account_id))]
        public Account? debit_account { get; set; }

        public int? credit_account_id { get; set; }

        [ForeignKey(nameof(credit_account_id))]
        public Account? credit_account { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? amount { get; set; }

        public int? counterparty_id { get; set; }

        [ForeignKey(nameof(counterparty_id))]
        public Counterparty? counterparty { get; set; }

        [StringLength(1000)]
        [Required]
        public string description { get; set; } = string.Empty;

        public bool is_complex { get; set; }

        public ICollection<TransactionLine> lines { get; set; } = new List<TransactionLine>();

        [Column(TypeName = "timestamp with time zone")]
        public DateTime created_at { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "timestamp with time zone")]
        public DateTime? edited_at { get; set; }

        [Column(TypeName = "timestamp with time zone")]
        public DateTime? deleted_at { get; set; }

        public bool deleted { get; set; }
    }

    [Table("transaction_lines")]
    public class TransactionLine
    {
        [Key]
        public int id { get; set; }

        public int transaction_id { get; set; }

        [ForeignKey(nameof(transaction_id))]
        public Transaction transaction { get; set; } = null!;

        public int account_id { get; set; }

        [ForeignKey(nameof(account_id))]
        public Account account { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        [Required]
        public decimal amount { get; set; }

        [Required]
        public EntrySide side { get; set; }

        public int? counterparty_id { get; set; }

        [ForeignKey(nameof(counterparty_id))]
        public Counterparty? counterparty { get; set; }
    }

    public enum EntrySide
    {
        debit,
        credit,
    }
}
