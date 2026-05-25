// Роли пользователей
export type UserRole = 'admin' | 'accountant' | 'observer';

// Пользователь
export interface User {
  id: number;
  nickname: string;
  email: string;
  role: UserRole;
  registration_date: string;
  edited_at?: string | null;
}

export interface UserPayload {
  nickname: string;
  email: string;
  password?: string;
  role: UserRole;
}

export interface UserSession {
  user_id: number;
  nickname: string;
  email: string;
  role: UserRole;
  expires_at: string;
  last_seen_at?: string | null;
}

export interface AdminAuditLog {
  id: number;
  admin_user_id: number;
  admin_nickname: string;
  action: string;
  target_user_id?: number | null;
  target_email?: string | null;
  details?: string | null;
  created_at: string;
}

export interface ImportUserRowError {
  line: number;
  email: string;
  message: string;
}

export interface ImportUsersResult {
  created: number;
  skipped: number;
  errors: ImportUserRowError[];
}

// Ответ авторизации
export interface AuthResponse {
  id: number;
  jwt_token: string;
  jwt_token_expires_at: string;
  refresh_token: string;
  role: UserRole;
  nickname: string;
}

// Тип счёта
export type AccountType = 'active' | 'passive' | 'active_passive';

// Счёт в плане счетов
export interface Account {
  id: number;
  number: string;
  name: string;
  type: AccountType;
  parent_id: number | null;
  is_system: boolean;
  children?: Account[];
}

// Контрагент
export interface Counterparty {
  id: number;
  name: string;
  inn?: string;
  contact?: string;
}

// Строка проводки (для сложных проводок)
export interface TransactionLine {
  id?: number;
  account_id: number;
  account?: Account;
  side: 'debit' | 'credit';
  amount: number;
  counterparty_id?: number;
  counterparty?: Counterparty;
}

// Проводка
export interface Transaction {
  id: number;
  date: string;
  debit_account_id?: number;
  debit_account?: Account;
  credit_account_id?: number;
  credit_account?: Account;
  amount?: number;
  counterparty_id?: number;
  counterparty?: Counterparty;
  description: string;
  is_complex: boolean;
  is_balanced: boolean;
  lines?: TransactionLine[];
  created_at?: string;
  updated_at?: string;
}

// Строка ОСВ отчёта
export interface OsvRow {
  account_id: number;
  number: string;
  name: string;
  opening_debit: number;
  opening_credit: number;
  turnover_debit: number;
  turnover_credit: number;
  closing_debit: number;
  closing_credit: number;
}

export interface ImportTransactionRow {
  line: number;
  date: string;
  debit_number: string;
  credit_number: string;
  amount: number;
  description: string;
  counterparty_inn?: string;
  counterparty_name?: string;
}

export interface ImportTransactionRowError {
  line: number;
  message: string;
}

export interface ImportTransactionsResult {
  created: number;
  skipped: number;
  errors: ImportTransactionRowError[];
}

// Отчёт ОСВ
export interface OsvReport {
  start_date: string;
  end_date: string;
  rows: OsvRow[];
  total_opening_debit: number;
  total_opening_credit: number;
  total_turnover_debit: number;
  total_turnover_credit: number;
  total_closing_debit: number;
  total_closing_credit: number;
}
