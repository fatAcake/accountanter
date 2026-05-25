using System.ComponentModel.DataAnnotations;
using backend.Models;

namespace backend.Models.DTOs
{
    public class AccountResponse
    {
        public int id { get; set; }
        public string number { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public AccountType type { get; set; }
        public int? parent_id { get; set; }
        public bool is_system { get; set; }
        public bool is_analytical { get; set; }
        public List<AccountResponse> children { get; set; } = new();
    }

    public class CreateAccountRequest
    {
        [StringLength(20, MinimumLength = 1)]
        [Required]
        public string number { get; set; } = string.Empty;

        [StringLength(255, MinimumLength = 1)]
        [Required]
        public string name { get; set; } = string.Empty;

        [Required]
        public AccountType type { get; set; }

        public int? parent_id { get; set; }
    }

    public class UpdateAccountRequest
    {
        [StringLength(255, MinimumLength = 1)]
        [Required]
        public string name { get; set; } = string.Empty;

        [Required]
        public AccountType type { get; set; }

        public int? parent_id { get; set; }
    }
}
