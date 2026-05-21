using System.ComponentModel.DataAnnotations;
using backend.Models;

namespace backend.Models.DTOs
{
    public class RegisterRequest
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
}
