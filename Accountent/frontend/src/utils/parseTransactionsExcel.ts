import * as XLSX from 'xlsx';
import type { ImportTransactionRow } from '../types';

const HEADER_ALIASES: Record<keyof Omit<ImportTransactionRow, 'line'>, string[]> = {
  date: ['date', 'дата', 'data'],
  debit_number: ['debit', 'debit_number', 'дебет', 'счет дт', 'счёт дт'],
  credit_number: ['credit', 'credit_number', 'кредит', 'счет кт', 'счёт кт'],
  amount: ['amount', 'сумма', 'sum'],
  description: ['description', 'основание', 'назначение', 'комментарий'],
  counterparty_inn: ['inn', 'counterparty_inn', 'инн'],
  counterparty_name: ['counterparty', 'counterparty_name', 'контрагент'],
};

function normalizeHeader(value: unknown): string {
  return String(value ?? '')
    .trim()
    .toLowerCase()
    .replace(/\s+/g, ' ');
}

function findColumnIndex(headers: string[], aliases: string[]): number {
  return headers.findIndex((h) => aliases.some((a) => h === a || h.includes(a)));
}

function parseAmount(value: unknown): number | null {
  if (typeof value === 'number' && !Number.isNaN(value)) return value;
  const raw = String(value ?? '')
    .replace(/\s/g, '')
    .replace(',', '.');
  const num = Number.parseFloat(raw);
  return Number.isFinite(num) ? num : null;
}

function formatExcelDate(value: unknown): string {
  if (value instanceof Date) {
    const d = value.getDate().toString().padStart(2, '0');
    const m = (value.getMonth() + 1).toString().padStart(2, '0');
    return `${d}.${m}.${value.getFullYear()}`;
  }
  if (typeof value === 'number') {
    const parsed = XLSX.SSF.parse_date_code(value);
    if (parsed) {
      const d = parsed.d.toString().padStart(2, '0');
      const m = parsed.m.toString().padStart(2, '0');
      return `${d}.${m}.${parsed.y}`;
    }
  }
  return String(value ?? '').trim();
}

export function parseTransactionsExcel(file: File): Promise<ImportTransactionRow[]> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = (e) => {
      try {
        const data = new Uint8Array(e.target?.result as ArrayBuffer);
        const workbook = XLSX.read(data, { type: 'array', cellDates: true });
        const sheet = workbook.Sheets[workbook.SheetNames[0]];
        if (!sheet) {
          resolve([]);
          return;
        }

        const matrix = XLSX.utils.sheet_to_json<unknown[]>(sheet, {
          header: 1,
          defval: '',
          raw: false,
        }) as unknown[][];

        if (matrix.length === 0) {
          resolve([]);
          return;
        }

        const headerRow = matrix[0].map(normalizeHeader);
        const cols = {
          date: findColumnIndex(headerRow, HEADER_ALIASES.date),
          debit: findColumnIndex(headerRow, HEADER_ALIASES.debit_number),
          credit: findColumnIndex(headerRow, HEADER_ALIASES.credit_number),
          amount: findColumnIndex(headerRow, HEADER_ALIASES.amount),
          description: findColumnIndex(headerRow, HEADER_ALIASES.description),
          inn: findColumnIndex(headerRow, HEADER_ALIASES.counterparty_inn),
          counterparty: findColumnIndex(headerRow, HEADER_ALIASES.counterparty_name),
        };

        const hasHeader =
          cols.date >= 0 && cols.debit >= 0 && cols.credit >= 0 && cols.amount >= 0;
        const startRow = hasHeader ? 1 : 0;

        if (!hasHeader) {
          cols.date = 0;
          cols.debit = 1;
          cols.credit = 2;
          cols.amount = 3;
          cols.description = 4;
          cols.inn = 5;
          cols.counterparty = -1;
        }

        const rows: ImportTransactionRow[] = [];

        for (let i = startRow; i < matrix.length; i++) {
          const row = matrix[i];
          if (!row?.length) continue;

          const get = (idx: number) => (idx >= 0 ? row[idx] : '');
          const amount = parseAmount(get(cols.amount));
          const debit = String(get(cols.debit)).trim();
          const credit = String(get(cols.credit)).trim();
          const description = String(get(cols.description)).trim();

          if (!debit && !credit && !description && amount === null) continue;

          rows.push({
            line: i + 1,
            date: formatExcelDate(get(cols.date)),
            debit_number: debit,
            credit_number: credit,
            amount: amount ?? 0,
            description,
            counterparty_inn:
              cols.inn >= 0 ? String(get(cols.inn)).trim() || undefined : undefined,
            counterparty_name:
              cols.counterparty >= 0
                ? String(get(cols.counterparty)).trim() || undefined
                : undefined,
          });
        }

        resolve(rows);
      } catch (err) {
        reject(err);
      }
    };
    reader.onerror = () => reject(reader.error);
    reader.readAsArrayBuffer(file);
  });
}

export function downloadTransactionsTemplate(): void {
  const headers = [
    'Дата',
    'Дебет',
    'Кредит',
    'Сумма',
    'Основание',
    'ИНН контрагента',
  ];
  const example = [
    '01.05.2026',
    '51.01',
    '62.01',
    '10000',
    'Оплата от покупателя',
    '',
  ];
  const sheet = XLSX.utils.aoa_to_sheet([headers, example]);
  const workbook = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(workbook, sheet, 'Проводки');
  XLSX.writeFile(workbook, 'import_transactions_template.xlsx');
}
