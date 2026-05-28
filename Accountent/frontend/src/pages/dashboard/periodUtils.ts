import dayjs, { type Dayjs } from 'dayjs';
import type { DashboardPeriodPreset } from '../../types/dashboard';

export const getPeriodRange = (
  preset: DashboardPeriodPreset,
  customRange?: [Dayjs, Dayjs] | null,
): [Dayjs, Dayjs] => {
  const now = dayjs();
  switch (preset) {
    case 'today':
      return [now.startOf('day'), now.endOf('day')];
    case 'month':
      return [now.startOf('month'), now.endOf('month')];
    case 'quarter': {
      const quarterStartMonth = Math.floor(now.month() / 3) * 3;
      const start = now.month(quarterStartMonth).startOf('month');
      return [start, start.add(2, 'month').endOf('month')];
    }
    case 'custom':
      return customRange ?? [now.startOf('month'), now.endOf('month')];
    default:
      return [now.startOf('month'), now.endOf('month')];
  }
};
