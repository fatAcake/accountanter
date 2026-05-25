import { useState, useEffect, useCallback } from 'react';
import {
  Card,
  Form,
  DatePicker,
  Select,
  Button,
  Table,
  Empty,
} from 'antd';
import { FileExcelOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import type { Dayjs } from 'dayjs';
import dayjs from 'dayjs';
import { useSearchParams } from 'react-router-dom';
import { OsvReport as OsvReportType, OsvRow, Account } from '../../types';
import { useApiError } from '../../hooks/useApiError';
import { accountsApi, reportsApi } from '../../api';
import { formatDate, formatMoney } from '../../utils/format';
import { exportOsvToExcel } from '../../utils/exportOsvExcel';

const { RangePicker } = DatePicker;

interface OsvFormValues {
  dateRange: [Dayjs, Dayjs];
  account_id?: number;
}

export default function OsvReport() {
  const { showError, showInfo, showSuccess } = useApiError();
  const [searchParams] = useSearchParams();
  const [form] = Form.useForm<OsvFormValues>();
  const [loading, setLoading] = useState(false);
  const [report, setReport] = useState<OsvReportType | null>(null);
  const [accounts, setAccounts] = useState<Account[]>([]);

  useEffect(() => {
    const loadAccounts = async () => {
      try {
        const response = await accountsApi.getFlat();
        setAccounts(response.data);
      } catch (error) {
        showError(error, { action: 'load', entity: 'счетов' });
      }
    };
    loadAccounts();
  }, [showError]);

  useEffect(() => {
    const from = searchParams.get('from');
    const to = searchParams.get('to');
    if (from && to) {
      const start = dayjs(from);
      const end = dayjs(to);
      if (start.isValid() && end.isValid()) {
        form.setFieldsValue({ dateRange: [start, end] });
      }
    }
  }, [searchParams, form]);

  const handleGenerateReport = async () => {
    try {
      const values = await form.validateFields();
      setLoading(true);
      const response = await reportsApi.getOsv({
        start_date: values.dateRange[0].format('YYYY-MM-DD'),
        end_date: values.dateRange[1].format('YYYY-MM-DD'),
        account_id: values.account_id,
      });
      setReport(response.data);
      if (response.data.rows.length === 0) {
        showInfo('Нет данных за выбранный период');
      }
    } catch (error) {
      showError(error, { action: 'report' });
    } finally {
      setLoading(false);
    }
  };

  const handleExport = useCallback(() => {
    if (!report || report.rows.length === 0) {
      showInfo('Сначала сформируйте отчёт с данными');
      return;
    }
    try {
      exportOsvToExcel(report);
      showSuccess('Файл Excel сохранён');
    } catch (error) {
      showError(error, { action: 'export', entity: 'отчёта' });
    }
  }, [report, showError, showInfo, showSuccess]);

  const formatCell = (value: number) => (value === 0 ? '—' : formatMoney(value));

  const columns: ColumnsType<OsvRow> = [
    { title: 'Счёт', dataIndex: 'number', key: 'number', width: 100, fixed: 'left' },
    {
      title: 'Наименование',
      dataIndex: 'name',
      key: 'name',
      width: 250,
      fixed: 'left',
    },
    {
      title: 'Начальное сальдо Дт',
      dataIndex: 'opening_debit',
      key: 'opening_debit',
      width: 150,
      align: 'right',
      render: formatCell,
    },
    {
      title: 'Начальное сальдо Кт',
      dataIndex: 'opening_credit',
      key: 'opening_credit',
      width: 150,
      align: 'right',
      render: formatCell,
    },
    {
      title: 'Оборот Дт',
      dataIndex: 'turnover_debit',
      key: 'turnover_debit',
      width: 150,
      align: 'right',
      render: formatCell,
    },
    {
      title: 'Оборот Кт',
      dataIndex: 'turnover_credit',
      key: 'turnover_credit',
      width: 150,
      align: 'right',
      render: formatCell,
    },
    {
      title: 'Конечное сальдо Дт',
      dataIndex: 'closing_debit',
      key: 'closing_debit',
      width: 150,
      align: 'right',
      render: formatCell,
    },
    {
      title: 'Конечное сальдо Кт',
      dataIndex: 'closing_credit',
      key: 'closing_credit',
      width: 150,
      align: 'right',
      render: formatCell,
    },
  ];

  return (
    <div>
      <h2 className="text-2xl font-bold mb-6">
        Оборотно-сальдовая ведомость (ОСВ)
      </h2>

      <Card className="mb-6">
        <Form form={form} layout="inline">
          <Form.Item
            name="dateRange"
            label="Период"
            rules={[{ required: true, message: 'Выберите период' }]}
          >
            <RangePicker format="DD.MM.YYYY" />
          </Form.Item>
          <Form.Item name="account_id" label="Счёт (опционально)">
            <Select
              allowClear
              showSearch
              placeholder="Все счета"
              style={{ width: 250 }}
              filterOption={(input, option) =>
                (option?.label?.toString() ?? '')
                  .toLowerCase()
                  .includes(input.toLowerCase())
              }
              options={accounts.map((a) => ({
                value: a.id,
                label: `${a.number} - ${a.name}`,
              }))}
            />
          </Form.Item>
          <Form.Item>
            <Button type="primary" onClick={handleGenerateReport} loading={loading}>
              Сформировать
            </Button>
          </Form.Item>
          <Form.Item>
            <Button
              icon={<FileExcelOutlined />}
              onClick={handleExport}
              disabled={!report || report.rows.length === 0}
            >
              Экспорт в Excel
            </Button>
          </Form.Item>
        </Form>
      </Card>

      {report && (
        <Card>
          <div className="mb-4">
            <p className="text-gray-600">
              Период: {formatDate(report.start_date)} — {formatDate(report.end_date)}
            </p>
          </div>
          {report.rows.length > 0 ? (
            <Table<OsvRow>
              columns={columns}
              dataSource={report.rows}
              rowKey="account_id"
              pagination={false}
              scroll={{ x: 1200 }}
              summary={() => (
                <Table.Summary>
                  <Table.Summary.Row
                    style={{ fontWeight: 'bold', backgroundColor: '#fafafa' }}
                  >
                    <Table.Summary.Cell index={0} colSpan={2}>
                      ИТОГО
                    </Table.Summary.Cell>
                    <Table.Summary.Cell index={2} align="right">
                      {formatMoney(report.total_opening_debit)}
                    </Table.Summary.Cell>
                    <Table.Summary.Cell index={3} align="right">
                      {formatMoney(report.total_opening_credit)}
                    </Table.Summary.Cell>
                    <Table.Summary.Cell index={4} align="right">
                      {formatMoney(report.total_turnover_debit)}
                    </Table.Summary.Cell>
                    <Table.Summary.Cell index={5} align="right">
                      {formatMoney(report.total_turnover_credit)}
                    </Table.Summary.Cell>
                    <Table.Summary.Cell index={6} align="right">
                      {formatMoney(report.total_closing_debit)}
                    </Table.Summary.Cell>
                    <Table.Summary.Cell index={7} align="right">
                      {formatMoney(report.total_closing_credit)}
                    </Table.Summary.Cell>
                  </Table.Summary.Row>
                </Table.Summary>
              )}
            />
          ) : (
            <Empty description="Нет данных за период" />
          )}
        </Card>
      )}
    </div>
  );
}
