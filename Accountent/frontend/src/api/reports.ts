import apiClient from './client';
import type { OsvReport } from '../types';

export interface OsvReportParams {
  start_date: string;
  end_date: string;
  account_id?: number;
}

export const reportsApi = {
  getOsv: (params: OsvReportParams) =>
    apiClient.get<OsvReport>('/api/reports/OSV', { params }),
};
