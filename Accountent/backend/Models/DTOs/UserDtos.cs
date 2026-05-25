using System.ComponentModel.DataAnnotations;
using backend.Models;

namespace backend.Models.DTOs
{
    public class UserResponse
    {
        public int id { get; set; }
        public string nickname { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public Roles role { get; set; }
        public DateTime registration_date { get; set; }
        public DateTime? edited_at { get; set; }
    }

    public class UserFilter
    {
        public string? search { get; set; }
    }

    public class CreateUserRequest
    {
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

        [EnumDataType(typeof(Roles), ErrorMessage = "Недопустимая роль.")]
        public Roles role { get; set; } = Roles.observer;
    }

    public class UpdateUserRequest
    {
        [StringLength(100, MinimumLength = 1)]
        [Required]
        public string nickname { get; set; } = string.Empty;

        [StringLength(100, MinimumLength = 5)]
        [Required]
        [EmailAddress]
        public string email { get; set; } = string.Empty;

        [EnumDataType(typeof(Roles), ErrorMessage = "Недопустимая роль.")]
        public Roles role { get; set; }

        [StringLength(255, MinimumLength = 5)]
        public string? password { get; set; }
    }

    public class UserSessionResponse
    {
        public int user_id { get; set; }
        public string nickname { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public Roles role { get; set; }
        public DateTime expires_at { get; set; }
        public DateTime? last_seen_at { get; set; }
    }

    public class AdminAuditLogResponse
    {
        public int id { get; set; }
        public int admin_user_id { get; set; }
        public string admin_nickname { get; set; } = string.Empty;
        public string action { get; set; } = string.Empty;
        public int? target_user_id { get; set; }
        public string? target_email { get; set; }
        public string? details { get; set; }
        public DateTime created_at { get; set; }
    }

    public class AuditLogFilter
    {
        public string? search { get; set; }
        public string? action { get; set; }
        public int limit { get; set; } = 100;
    }

    public class ImportUsersResult
    {
        public int created { get; set; }
        public int skipped { get; set; }
        public List<ImportUserRowError> errors { get; set; } = [];
    }

    public class ImportUserRowError
    {
        public int line { get; set; }
        public string email { get; set; } = string.Empty;
        public string message { get; set; } = string.Empty;
    }
}
