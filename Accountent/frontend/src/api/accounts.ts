import apiClient from './client';
import type { Account, AccountType } from '../types';

export interface CreateAccountPayload {
  number: string;
  name: string;
  type: AccountType;
  parent_id?: number | null;
}

export interface UpdateAccountPayload {
  name: string;
  type: AccountType;
  parent_id?: number | null;
}

export const accountsApi = {
  getTree: () => apiClient.get<Account[]>('/api/chart_of_accounts'),

  getFlat: () => apiClient.get<Account[]>('/api/chart_of_accounts?flat=true'),

  create: (data: CreateAccountPayload) =>
    apiClient.post<Account>('/api/chart_of_accounts', data),

  update: (id: number, data: UpdateAccountPayload) =>
    apiClient.put<Account>(`/api/chart_of_accounts/${id}`, data),

  remove: (id: number) => apiClient.delete(`/api/chart_of_accounts/${id}`),
};
