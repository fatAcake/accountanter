import apiClient from './client';
import type { AuthResponse } from '../types';

export const authApi = {
  login: (email: string, password: string) =>
    apiClient.post<AuthResponse>('/api/auth/login', { email, password }),

  register: (nickname: string, email: string, password: string) =>
    apiClient.post('/api/auth/register', {
      nickname,
      email,
      password,
      role: 'observer',
    }),

  logout: () => apiClient.post('/api/auth/logout'),
};
