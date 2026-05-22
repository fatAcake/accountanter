using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    [Table("users")]
    public class Users
    {
        [Key]
        public int id { get; set; }

        [StringLength(100, MinimumLength = 1)]
        [Required]
        public string nickname { get; set; } = string.Empty;

        [StringLength(100, MinimumLength = 5)]
        [Required]
        [EmailAddress]
        public string email { get; set; } = string.Empty;

        [StringLength(255, MinimumLength = 5)]
        [Required]
        public string password { get; set; } = string.Empty;

        [Column(TypeName = "timestamp with time zone")]
        public DateTime registration_date { get; set; } = DateTime.UtcNow;

        [Required]
        public Roles role { get; set; }

        [Column(TypeName = "timestamp with time zone")]
        public DateTime? edited_at { get; set; }

        [Column(TypeName = "timestamp with time zone")]
        public DateTime? deleted_at { get; set; }

        public bool deleted { get; set; } = false;


        [StringLength(255)]
        public string? refresh_token { get; set; }

        [Column(TypeName = "timestamp with time zone")]
        public DateTime? refresh_token_expires_at { get; set; }
    }

    public enum Roles
    {
        admin,
        accountant,
        observer,
        None,
    }
}
