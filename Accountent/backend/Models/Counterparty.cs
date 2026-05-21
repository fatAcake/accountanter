using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    [Table("counterparties")]
    public class Counterparty
    {
        [Key]
        public int id { get; set; }

        [StringLength(255, MinimumLength = 1)]
        [Required]
        public string name { get; set; } = string.Empty;

        [StringLength(12)]
        public string? inn { get; set; }

        [StringLength(500)]
        public string? contact { get; set; }

        [Column(TypeName = "timestamp with time zone")]
        public DateTime created_at { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "timestamp with time zone")]
        public DateTime? edited_at { get; set; }

        [Column(TypeName = "timestamp with time zone")]
        public DateTime? deleted_at { get; set; }

        public bool deleted { get; set; }
    }
}
