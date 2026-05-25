using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    [Table("accounts")]
    public class Account
    {
        [Key]
        public int id { get; set; }

        [StringLength(20, MinimumLength = 1)]
        [Required]
        public string number { get; set; } = string.Empty;

        [StringLength(255, MinimumLength = 1)]
        [Required]
        public string name { get; set; } = string.Empty;

        [Required]
        public AccountType type { get; set; }

        public int? parent_id { get; set; }

        [ForeignKey(nameof(parent_id))]
        public Account? parent { get; set; }

        public ICollection<Account> children { get; set; } = new List<Account>();

        /// <summary>Счёт из типового плана (нельзя удалить).</summary>
        public bool is_system { get; set; }

        [Column(TypeName = "timestamp with time zone")]
        public DateTime created_at { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "timestamp with time zone")]
        public DateTime? edited_at { get; set; }

        [Column(TypeName = "timestamp with time zone")]
        public DateTime? deleted_at { get; set; }

        public bool deleted { get; set; }
    }

    public enum AccountType
    {
        active,
        passive,
        active_passive,
    }
}
