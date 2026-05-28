import apiClient from './client';
import type { Counterparty } from '../types';

export interface CounterpartyPayload {
  name: string;
  inn?: string;
  contact?: string;
}

export const counterpartiesApi = {
  getAll: () => apiClient.get<Counterparty[]>('/api/counterparties'),

  create: (data: CounterpartyPayload) =>
    apiClient.post<Counterparty>('/api/counterparties', data),

  update: (id: number, data: CounterpartyPayload) =>
    apiClient.put<Counterparty>(`/api/counterparties/${id}`, data),

  remove: (id: number) => apiClient.delete(`/api/counterparties/${id}`),
};
