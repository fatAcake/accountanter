export interface DashboardKpi {
  net_result: number;
  total_income: number;
  total_expenses: number;
  cash_balance: number;
  transactions_count: number;
}

export interface IncomeExpenseChartPoint {
  period: string;
  income: number;
  expenses: number;
}

export interface ExpenseStructureItem {
  account_id: number;
  number: string;
  name: string;
  amount: number;
}

export interface BalanceDynamicsPoint {
  period: string;
  account_number: string;
  account_name: string;
  balance: number;
}

export interface DashboardRecentTransaction {
  id: number;
  date: string;
  debit_number?: string | null;
  credit_number?: string | null;
  amount: number;
  description: string;
}

export interface TopCounterpartyItem {
  id: number;
  name: string;
  turnover: number;
  role_type: string;
}

export interface DashboardAlert {
  type: 'warning' | 'error' | 'info';
  message: string;
  link?: string | null;
}

export interface DashboardData {
  start_date: string;
  end_date: string;
  kpi: DashboardKpi;
  income_expense_chart: IncomeExpenseChartPoint[];
  expense_structure: ExpenseStructureItem[];
  balance_dynamics: BalanceDynamicsPoint[];
  recent_transactions: DashboardRecentTransaction[];
  top_counterparties: TopCounterpartyItem[];
  alerts: DashboardAlert[];
}

export type DashboardPeriodPreset = 'today' | 'month' | 'quarter' | 'custom';
