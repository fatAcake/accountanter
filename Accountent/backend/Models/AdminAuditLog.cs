using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    [Table("admin_audit_logs")]
    public class AdminAuditLog
    {
        [Key]
        public int id { get; set; }

        public int admin_user_id { get; set; }

        [StringLength(100)]
        [Required]
        public string admin_nickname { get; set; } = string.Empty;

        [StringLength(50)]
        [Required]
        public string action { get; set; } = string.Empty;

        public int? target_user_id { get; set; }

        [StringLength(100)]
        public string? target_email { get; set; }

        [StringLength(1000)]
        public string? details { get; set; }

        [Column(TypeName = "timestamp with time zone")]
        public DateTime created_at { get; set; } = DateTime.UtcNow;
    }

    public static class AdminAuditActions
    {
        public const string UserCreate = "user.create";
        public const string UserUpdate = "user.update";
        public const string UserDelete = "user.delete";
        public const string UserImport = "user.import";
        public const string SessionRevoke = "session.revoke";
    }
}
