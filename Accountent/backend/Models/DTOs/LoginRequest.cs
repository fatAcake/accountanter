using System.ComponentModel.DataAnnotations;

namespace backend.Models.DTOs
{
    public class LoginRequest
    {
        [StringLength(100, MinimumLength = 5)]
        [Required]
        [EmailAddress]
        public string email { get; set; } = string.Empty;

        [StringLength(255, MinimumLength = 5)]
        [Required]
        public string password { get; set; } = string.Empty;
    }
}
