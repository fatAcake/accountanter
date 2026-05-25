import * as XLSX from 'xlsx';
import type { OsvReport } from '../types';
import { formatDate } from './format';

const HEADERS = [
  'Счёт',
  'Наименование',
  'Начальное сальдо Дт',
  'Начальное сальдо Кт',
  'Оборот Дт',
  'Оборот Кт',
  'Конечное сальдо Дт',
  'Конечное сальдо Кт',
] as const;

export function exportOsvToExcel(report: OsvReport): void {
  const data: (string | number)[][] = [
    [`Оборотно-сальдовая ведомость`],
    [`Период: ${formatDate(report.start_date)} — ${formatDate(report.end_date)}`],
    [],
    [...HEADERS],
    ...report.rows.map((row) => [
      row.number,
      row.name,
      row.opening_debit,
      row.opening_credit,
      row.turnover_debit,
      row.turnover_credit,
      row.closing_debit,
      row.closing_credit,
    ]),
    [
      'ИТОГО',
      '',
      report.total_opening_debit,
      report.total_opening_credit,
      report.total_turnover_debit,
      report.total_turnover_credit,
      report.total_closing_debit,
      report.total_closing_credit,
    ],
  ];

  const worksheet = XLSX.utils.aoa_to_sheet(data);
  worksheet['!cols'] = [
    { wch: 12 },
    { wch: 40 },
    { wch: 18 },
    { wch: 18 },
    { wch: 18 },
    { wch: 18 },
    { wch: 18 },
    { wch: 18 },
  ];

  const workbook = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(workbook, worksheet, 'ОСВ');

  const base = `OSV_${formatDate(report.start_date)}_${formatDate(report.end_date)}`;
  const fileName = `${base.replace(/\./g, '-')}.xlsx`;
  XLSX.writeFile(workbook, fileName);
}
