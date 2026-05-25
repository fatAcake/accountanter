import { lazy, Suspense, useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
import {
  Alert,
  Button,
  Card,
  Col,
  DatePicker,
  Empty,
  Flex,
  Row,
  Segmented,
  Space,
  Spin,
  Statistic,
  Table,
  Tag,
  Typography,
} from 'antd';
import {
  ArrowDownOutlined,
  ArrowUpOutlined,
  BankOutlined,
  BarChartOutlined,
  DollarOutlined,
  FileExcelOutlined,
  FileTextOutlined,
  PlusOutlined,
  SettingOutlined,
  SwapOutlined,
  TeamOutlined,
  UploadOutlined,
  WalletOutlined,
} from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import type { Dayjs } from 'dayjs';
import { useNavigate } from 'react-router-dom';
import { dashboardApi, reportsApi } from '../api';
import { useAuth } from '../hooks/useAuth';
import { useApiError } from '../hooks/useApiError';
import { exportOsvToExcel } from '../utils/exportOsvExcel';
import type {
  DashboardAlert,
  DashboardData,
  DashboardPeriodPreset,
  DashboardRecentTransaction,
  TopCounterpartyItem,
} from '../types/dashboard';
import { formatDate, formatMoney, formatNumber } from '../utils/format';
import { getPeriodRange } from './dashboard/periodUtils';
import ImportTransactionsModal from '../components/ImportTransactionsModal';

const { RangePicker } = DatePicker;
const { Text, Title } = Typography;

const DashboardCharts = lazy(() => import('./dashboard/DashboardCharts'));

type KpiKey = 'net' | 'income' | 'expenses' | 'cash' | 'count';

const KPI_CONFIG: {
  key: KpiKey;
  title: string;
  icon: ReactNode;
  field?: keyof DashboardData['kpi'];
  isMoney?: boolean;
  filter?: string;
}[] = [
  { key: 'net', title: 'Чистый результат', icon: <DollarOutlined />, field: 'net_result', isMoney: true },
  { key: 'income', title: 'Всего доходов', icon: <ArrowUpOutlined />, field: 'total_income', isMoney: true, filter: '90.01' },
  { key: 'expenses', title: 'Всего расходов', icon: <ArrowDownOutlined />, field: 'total_expenses', isMoney: true, filter: '90.02' },
  { key: 'cash', title: 'Сальдо (50, 51)', icon: <WalletOutlined />, field: 'cash_balance', isMoney: true, filter: '50' },
  { key: 'count', title: 'Проводок за период', icon: <SwapOutlined />, field: 'transactions_count' },
];

export default function Dashboard() {
  const navigate = useNavigate();
  const { canEdit, isAdmin } = useAuth();
  const { showError, showSuccess, showInfo } = useApiError();
  const [preset, setPreset] = useState<DashboardPeriodPreset>('month');
  const [customRange, setCustomRange] = useState<[Dayjs, Dayjs] | null>(null);
  const [data, setData] = useState<DashboardData | null>(null);
  const [loading, setLoading] = useState(false);
  const [exportLoading, setExportLoading] = useState(false);
  const [importOpen, setImportOpen] = useState(false);

  const [start, end] = useMemo(
    () => getPeriodRange(preset, customRange),
    [preset, customRange],
  );

  const loadDashboard = useCallback(async () => {
    setLoading(true);
    try {
      const response = await dashboardApi.get({
        start_date: start.format('YYYY-MM-DD'),
        end_date: end.format('YYYY-MM-DD'),
      });
      setData(response.data);
    } catch (error) {
      showError(error, { action: 'load', entity: 'дашборда' });
    } finally {
      setLoading(false);
    }
  }, [start, end, showError]);

  useEffect(() => {
    void loadDashboard();
  }, [loadDashboard]);

  const goTransactions = (extra?: Record<string, string>) => {
    const params = new URLSearchParams({
      from: start.format('YYYY-MM-DD'),
      to: end.format('YYYY-MM-DD'),
      ...extra,
    });
    navigate(`/transactions?${params.toString()}`);
  };

  const handleExportOsv = useCallback(async () => {
    setExportLoading(true);
    try {
      const response = await reportsApi.getOsv({
        start_date: start.format('YYYY-MM-DD'),
        end_date: end.format('YYYY-MM-DD'),
      });
      if (response.data.rows.length === 0) {
        showInfo('Нет данных за период для экспорта');
        return;
      }
      exportOsvToExcel(response.data);
      showSuccess('Файл Excel сохранён');
    } catch (error) {
      showError(error, { action: 'export', entity: 'ОСВ' });
    } finally {
      setExportLoading(false);
    }
  }, [start, end, showError, showInfo, showSuccess]);

  const quickActions: {
    key: string;
    icon: ReactNode;
    label: string;
    show: boolean;
    disabled?: boolean;
    loading?: boolean;
    onClick: () => void;
  }[] = [
    {
      key: 'new-tx',
      icon: <PlusOutlined />,
      label: 'Новая проводка',
      onClick: () => navigate('/transactions/new'),
      show: canEdit(),
    },
    {
      key: 'osv',
      icon: <FileTextOutlined />,
      label: 'Сформировать ОСВ',
      onClick: () =>
        navigate(
          `/reports/osv?from=${start.format('YYYY-MM-DD')}&to=${end.format('YYYY-MM-DD')}`,
        ),
      show: true,
    },
    {
      key: 'export',
      icon: <FileExcelOutlined />,
      label: 'Экспорт ОСВ в Excel',
      onClick: () => void handleExportOsv(),
      show: true,
      loading: exportLoading,
    },
    {
      key: 'counterparty',
      icon: <TeamOutlined />,
      label: 'Добавить контрагента',
      onClick: () => navigate('/counterparties'),
      show: canEdit(),
    },
    {
      key: 'import',
      icon: <UploadOutlined />,
      label: 'Импорт из Excel',
      onClick: () => setImportOpen(true),
      show: canEdit(),
    },
    {
      key: 'accounts',
      icon: <SettingOutlined />,
      label: 'План счетов',
      onClick: () => navigate('/chart-of-accounts'),
      show: canEdit(),
    },
    ...(isAdmin()
      ? [
          {
            key: 'users',
            icon: <TeamOutlined />,
            label: 'Пользователи',
            onClick: () => navigate('/admin/users'),
            show: true,
          },
        ]
      : []),
  ].filter((a) => a.show);

  const recentColumns: ColumnsType<DashboardRecentTransaction> = [
    {
      title: 'Дата',
      dataIndex: 'date',
      width: 110,
      render: (v: string) => formatDate(v),
    },
    { title: 'Дебет', dataIndex: 'debit_number', width: 100 },
    { title: 'Кредит', dataIndex: 'credit_number', width: 100 },
    {
      title: 'Сумма',
      dataIndex: 'amount',
      align: 'right',
      render: (v: number) => formatMoney(v),
    },
    { title: 'Основание', dataIndex: 'description', ellipsis: true },
    {
      title: '',
      width: 80,
      render: (_, row) =>
        canEdit() ? (
          <Button type="link" size="small" onClick={() => navigate(`/transactions/${row.id}`)}>
            Открыть
          </Button>
        ) : null,
    },
  ];

  const topColumns: ColumnsType<TopCounterpartyItem> = [
    { title: 'Контрагент', dataIndex: 'name' },
    {
      title: 'Оборот',
      dataIndex: 'turnover',
      align: 'right',
      render: (v: number) => formatMoney(v),
    },
    {
      title: 'Тип',
      dataIndex: 'role_type',
      render: (v: string) => <Tag>{v}</Tag>,
    },
  ];

  const alertColor = (type: DashboardAlert['type']) => {
    if (type === 'error') return 'error';
    if (type === 'warning') return 'warning';
    return 'info';
  };

  return (
    <Spin spinning={loading}>
      <div className="space-y-6">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <Title level={3} className="!mb-0">
            Главная
          </Title>
          <Space wrap>
            <Segmented
              value={preset}
              onChange={(v) => setPreset(v as DashboardPeriodPreset)}
              options={[
                { label: 'Сегодня', value: 'today' },
                { label: 'Месяц', value: 'month' },
                { label: 'Квартал', value: 'quarter' },
                { label: 'Период', value: 'custom' },
              ]}
            />
            {preset === 'custom' && (
              <RangePicker
                value={customRange ?? [start, end]}
                onChange={(vals) => {
                  if (vals?.[0] && vals[1]) setCustomRange([vals[0], vals[1]]);
                }}
                format="DD.MM.YYYY"
              />
            )}
            <Text type="secondary">
              {start.format('DD.MM.YYYY')} — {end.format('DD.MM.YYYY')}
            </Text>
          </Space>
        </div>

        <Row gutter={[16, 16]}>
          {KPI_CONFIG.map((item) => {
            const value = item.field && data ? data.kpi[item.field] : 0;
            const display =
              item.isMoney && typeof value === 'number'
                ? formatMoney(value)
                : formatNumber(Number(value));
            const color =
              item.key === 'net' && typeof value === 'number'
                ? value >= 0
                  ? '#3f8600'
                  : '#cf1322'
                : undefined;

            return (
              <Col xs={24} sm={12} lg={8} xl={24 / 5} key={item.key}>
                <Card
                  hoverable
                  className="cursor-pointer h-full"
                  onClick={() => goTransactions(item.filter ? { account: item.filter } : undefined)}
                >
                  <Statistic
                    title={
                      <Space>
                        {item.icon}
                        {item.title}
                      </Space>
                    }
                    value={display}
                    styles={{
                      content: { color, fontSize: item.isMoney ? 20 : 24 },
                    }}
                  />
                </Card>
              </Col>
            );
          })}
        </Row>

        <Row gutter={[16, 16]}>
          <Col xs={24} lg={18}>
            <Suspense
              fallback={
                <Card>
                  <Spin />
                </Card>
              }
            >
              {data ? (
                <DashboardCharts data={data} />
              ) : (
                <Card>
                  <Empty />
                </Card>
              )}
            </Suspense>
          </Col>
          <Col xs={24} lg={6}>
            <Card title="Быстрые действия" className="h-full">
              <Space orientation="vertical" className="w-full" size="middle">
                {quickActions.map((action) => (
                  <Button
                    key={action.key}
                    block
                    icon={action.icon}
                    disabled={action.disabled}
                    loading={action.loading}
                    onClick={action.onClick}
                  >
                    {action.label}
                  </Button>
                ))}
                {!canEdit() && (
                  <Alert
                    type="info"
                    showIcon
                    title="Режим просмотра: редактирование недоступно"
                  />
                )}
              </Space>
            </Card>
          </Col>
        </Row>

        <Row gutter={[16, 16]}>
          <Col xs={24} xl={14}>
            <Card
              title="Последние операции"
              extra={
                <Button type="link" onClick={() => goTransactions()}>
                  Все проводки
                </Button>
              }
            >
              <Table
                rowKey="id"
                size="small"
                pagination={false}
                columns={recentColumns}
                dataSource={data?.recent_transactions ?? []}
                locale={{ emptyText: 'Нет операций за период' }}
                onRow={(row) => ({
                  onClick: () => navigate(`/transactions/${row.id}`),
                  className: 'cursor-pointer',
                })}
              />
            </Card>
          </Col>
          <Col xs={24} xl={10}>
            <Card
              title="Топ контрагентов"
              extra={
                <Button type="link" onClick={() => navigate('/counterparties')}>
                  Все
                </Button>
              }
            >
              <Table
                rowKey="id"
                size="small"
                pagination={false}
                columns={topColumns}
                dataSource={data?.top_counterparties ?? []}
                locale={{ emptyText: 'Нет данных' }}
              />
            </Card>
          </Col>
        </Row>

        <Card title="Уведомления" extra={<BarChartOutlined />}>
          {data?.alerts?.length ? (
            <Flex vertical gap="middle">
              {data.alerts.map((alert, index) => (
                <Alert
                  key={`${alert.type}-${index}`}
                  type={alertColor(alert.type)}
                  showIcon
                  title={alert.message}
                  action={
                    alert.link ? (
                      <Button size="small" onClick={() => navigate(alert.link!)}>
                        Перейти
                      </Button>
                    ) : undefined
                  }
                />
              ))}
            </Flex>
          ) : (
            <Empty description="Предупреждений нет" />
          )}
        </Card>

        {isAdmin() && (
          <Card>
            <Space>
              <BankOutlined />
              <Text>Администратор: доступны аудит и управление пользователями в разделе «Пользователи».</Text>
            </Space>
          </Card>
        )}
      </div>

      <ImportTransactionsModal
        open={importOpen}
        onClose={() => setImportOpen(false)}
        onSuccess={() => void loadDashboard()}
      />
    </Spin>
  );
}
