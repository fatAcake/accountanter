import apiClient from './client';
import type {
  AdminAuditLog,
  ImportUsersResult,
  User,
  UserPayload,
  UserSession,
} from '../types';

export const usersApi = {
  getAll: (search?: string) =>
    apiClient.get<User[]>('/api/users', { params: search ? { search } : undefined }),

  create: (data: UserPayload & { password: string }) =>
    apiClient.post<User>('/api/users', data),

  update: (id: number, data: UserPayload) =>
    apiClient.put<User>(`/api/users/${id}`, data),

  remove: (id: number) => apiClient.delete(`/api/users/${id}`),

  getSessions: () => apiClient.get<UserSession[]>('/api/users/sessions'),

  revokeSession: (userId: number) =>
    apiClient.delete(`/api/users/${userId}/session`),

  getAuditLogs: (params?: { search?: string; action?: string; limit?: number }) =>
    apiClient.get<AdminAuditLog[]>('/api/users/audit-logs', { params }),

  importCsv: (file: File) => {
    const form = new FormData();
    form.append('file', file);
    return apiClient.post<ImportUsersResult>('/api/users/import', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },
};
