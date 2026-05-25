import apiClient from './client';
import type { ImportTransactionRow, ImportTransactionsResult, Transaction } from '../types';

export interface TransactionListParams {
  start_date?: string;
  end_date?: string;
  account_id?: number;
  counterparty_id?: number;
}

export interface TransactionPayload {
  date: string;
  description: string;
  debit_account_id?: number;
  credit_account_id?: number;
  amount?: number;
  counterparty_id?: number | null;
  lines?: {
    account_id: number;
    side: 'debit' | 'credit';
    amount: number;
    counterparty_id?: number | null;
  }[];
}

export const transactionsApi = {
  list: (params?: TransactionListParams) =>
    apiClient.get<Transaction[]>('/api/transactions', { params }),

  getById: (id: number | string) =>
    apiClient.get<Transaction>(`/api/transactions/${id}`),

  create: (data: TransactionPayload) =>
    apiClient.post<Transaction>('/api/transactions', data),

  update: (id: number | string, data: TransactionPayload) =>
    apiClient.put<Transaction>(`/api/transactions/${id}`, data),

  remove: (id: number) => apiClient.delete(`/api/transactions/${id}`),

  import: (rows: ImportTransactionRow[]) =>
    apiClient.post<ImportTransactionsResult>('/api/transactions/import', { rows }),
};
