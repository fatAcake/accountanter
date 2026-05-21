using System.ComponentModel.DataAnnotations;

namespace backend.Models.DTOs
{
    public class CounterpartyResponse
    {
        public int id { get; set; }
        public string name { get; set; } = string.Empty;
        public string? inn { get; set; }
        public string? contact { get; set; }
        public DateTime created_at { get; set; }
        public DateTime? edited_at { get; set; }
    }

    public class CreateCounterpartyRequest
    {
        [StringLength(255, MinimumLength = 1)]
        [Required]
        public string name { get; set; } = string.Empty;

        [StringLength(12)]
        [RegularExpression(@"^(\d{10}|\d{12})?$", ErrorMessage = "ИНН должен содержать 10 или 12 цифр.")]
        public string? inn { get; set; }

        [StringLength(500)]
        public string? contact { get; set; }
    }

    public class UpdateCounterpartyRequest : CreateCounterpartyRequest
    {
    }

    public class CounterpartyFilter
    {
        public string? search { get; set; }
    }
}
