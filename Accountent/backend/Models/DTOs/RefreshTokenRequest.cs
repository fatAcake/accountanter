using System.ComponentModel.DataAnnotations;

namespace backend.Models.DTOs
{
    public class RefreshTokenRequest
    {
        [Required]
        public string refresh_token { get; set; } = string.Empty;
    }
}
