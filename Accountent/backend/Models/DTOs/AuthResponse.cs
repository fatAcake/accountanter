using backend.Models;

namespace backend.Models.DTOs
{
    public class AuthResponse
    {
        public int id { get; set; }
        public string nickname { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public Roles role { get; set; }
        public string jwt_token { get; set; } = string.Empty;
        public DateTime jwt_token_expires_at { get; set; }
        public string refresh_token { get; set; } = string.Empty;
        public DateTime refresh_token_expires_at { get; set; }
    }
}
