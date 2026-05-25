import dayjs from 'dayjs';
import 'dayjs/locale/ru';
import timezone from 'dayjs/plugin/timezone';
import utc from 'dayjs/plugin/utc';
import { env } from '../config/env';

dayjs.extend(utc);
dayjs.extend(timezone);
dayjs.locale('ru');

const TIMEZONE = env.timezone;

export const formatDate = (date: string | Date): string => {
  return dayjs(date).tz(TIMEZONE).format('DD.MM.YYYY');
};

export const formatDateTime = (date: string | Date): string => {
  return dayjs(date).tz(TIMEZONE).format('DD.MM.YYYY HH:mm');
};

export const formatMoney = (amount: number): string => {
  return new Intl.NumberFormat('ru-RU', {
    style: 'currency',
    currency: 'RUB',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(amount);
};

export const formatNumber = (value: number): string => {
  return new Intl.NumberFormat('ru-RU', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value);
};
