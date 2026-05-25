using backend.Models;

namespace backend.Models.DTOs
{
    public class OsvRowResponse
    {
        public int account_id { get; set; }
        public string number { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public AccountType type { get; set; }
        public decimal opening_debit { get; set; }
        public decimal opening_credit { get; set; }
        public decimal turnover_debit { get; set; }
        public decimal turnover_credit { get; set; }
        public decimal closing_debit { get; set; }
        public decimal closing_credit { get; set; }
    }

    public class OsvReportResponse
    {
        public DateTime start_date { get; set; }
        public DateTime end_date { get; set; }
        public List<OsvRowResponse> rows { get; set; } = new();
        public decimal total_opening_debit { get; set; }
        public decimal total_opening_credit { get; set; }
        public decimal total_turnover_debit { get; set; }
        public decimal total_turnover_credit { get; set; }
        public decimal total_closing_debit { get; set; }
        public decimal total_closing_credit { get; set; }
    }
}
