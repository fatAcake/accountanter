namespace backend.Models.DTOs
{
    public class DashboardKpiResponse
    {
        public decimal net_result { get; set; }
        public decimal total_income { get; set; }
        public decimal total_expenses { get; set; }
        public decimal cash_balance { get; set; }
        public int transactions_count { get; set; }
    }

    public class IncomeExpenseChartPoint
    {
        public string period { get; set; } = string.Empty;
        public decimal income { get; set; }
        public decimal expenses { get; set; }
    }

    public class ExpenseStructureItem
    {
        public int account_id { get; set; }
        public string number { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public decimal amount { get; set; }
    }

    public class BalanceDynamicsPoint
    {
        public string period { get; set; } = string.Empty;
        public string account_number { get; set; } = string.Empty;
        public string account_name { get; set; } = string.Empty;
        public decimal balance { get; set; }
    }

    public class DashboardRecentTransaction
    {
        public int id { get; set; }
        public DateTime date { get; set; }
        public string? debit_number { get; set; }
        public string? credit_number { get; set; }
        public decimal amount { get; set; }
        public string description { get; set; } = string.Empty;
    }

    public class TopCounterpartyItem
    {
        public int id { get; set; }
        public string name { get; set; } = string.Empty;
        public decimal turnover { get; set; }
        public string role_type { get; set; } = string.Empty;
    }

    public class DashboardAlert
    {
        public string type { get; set; } = "warning";
        public string message { get; set; } = string.Empty;
        public string? link { get; set; }
    }

    public class DashboardResponse
    {
        public DateTime start_date { get; set; }
        public DateTime end_date { get; set; }
        public DashboardKpiResponse kpi { get; set; } = new();
        public List<IncomeExpenseChartPoint> income_expense_chart { get; set; } = new();
        public List<ExpenseStructureItem> expense_structure { get; set; } = new();
        public List<BalanceDynamicsPoint> balance_dynamics { get; set; } = new();
        public List<DashboardRecentTransaction> recent_transactions { get; set; } = new();
        public List<TopCounterpartyItem> top_counterparties { get; set; } = new();
        public List<DashboardAlert> alerts { get; set; } = new();
    }
}
