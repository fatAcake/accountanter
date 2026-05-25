import apiClient from './client';
import type { DashboardData } from '../types/dashboard';

export interface DashboardParams {
  start_date: string;
  end_date: string;
}

export const dashboardApi = {
  get: (params: DashboardParams) =>
    apiClient.get<DashboardData>('/api/dashboard', { params }),
  getKpi: (params: DashboardParams) =>
    apiClient.get<DashboardData['kpi']>('/api/dashboard/kpi', { params }),
};
