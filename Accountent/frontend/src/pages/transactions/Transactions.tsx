import { useState, useEffect, useCallback } from 'react';
import {
  Table,
  Button,
  Space,
  Tag,
  DatePicker,
  Select,
  Form,
  Modal,
  Tooltip,
  Grid,
} from 'antd';
import {
  PlusOutlined,
  EditOutlined,
  DeleteOutlined,
  EyeOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
  ExclamationCircleOutlined,
  UploadOutlined,
} from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import type { Dayjs } from 'dayjs';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { Transaction, Account, Counterparty } from '../../types';
import { useAuth } from '../../hooks/useAuth';
import { useApiError } from '../../hooks/useApiError';
import { accountsApi, counterpartiesApi, transactionsApi } from '../../api';
import { formatDate, formatMoney } from '../../utils/format';
import ImportTransactionsModal from '../../components/ImportTransactionsModal';

const { RangePicker } = DatePicker;
const { confirm } = Modal;

interface TransactionFilters {
  dateRange?: [Dayjs, Dayjs];
  account_id?: number;
  counterparty_id?: number;
}

export default function Transactions() {
  const { canEdit } = useAuth();
  const { showError, showSuccess } = useApiError();
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [importOpen, setImportOpen] = useState(searchParams.get('import') === '1');
  const [loading, setLoading] = useState(false);
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [counterparties, setCounterparties] = useState<Counterparty[]>([]);
  const [filterForm] = Form.useForm<TransactionFilters>();
  const screens = Grid.useBreakpoint();
  const isMobile = screens.md === false;

  const loadAccounts = async () => {
    try {
      const response = await accountsApi.getFlat();
      setAccounts(response.data);
    } catch (error) {
      showError(error, { action: 'load', entity: 'счетов' });
    }
  };

  const loadCounterparties = async () => {
    try {
      const response = await counterpartiesApi.getAll();
      setCounterparties(response.data);
    } catch (error) {
      showError(error, { action: 'load', entity: 'контрагентов' });
    }
  };

  const loadTransactions = useCallback(async (filters?: TransactionFilters) => {
    setLoading(true);
    try {
      const response = await transactionsApi.list({
        start_date: filters?.dateRange?.[0]?.format('YYYY-MM-DD'),
        end_date: filters?.dateRange?.[1]?.format('YYYY-MM-DD'),
        account_id: filters?.account_id,
        counterparty_id: filters?.counterparty_id,
      });
      setTransactions(response.data);
    } catch (error) {
      showError(error, { action: 'load', entity: 'проводок' });
    } finally {
      setLoading(false);
    }
  }, [showError]);

  useEffect(() => {
    loadAccounts();
    loadCounterparties();
    loadTransactions();
  }, [loadTransactions]);

  useEffect(() => {
    if (searchParams.get('import') === '1' && canEdit()) {
      setImportOpen(true);
      setSearchParams({}, { replace: true });
    }
  }, [searchParams, setSearchParams, canEdit]);

  const handleFilter = async () => {
    const values = filterForm.getFieldsValue();
    await loadTransactions(values);
  };

  const handleResetFilter = async () => {
    filterForm.resetFields();
    await loadTransactions();
  };

  const handleDelete = (transaction: Transaction) => {
    confirm({
      title: 'Удалить проводку?',
      icon: <ExclamationCircleOutlined />,
      content: 'Вы действительно хотите удалить эту проводку?',
      okText: 'Удалить',
      okType: 'danger',
      cancelText: 'Отмена',
      onOk: async () => {
        try {
          await transactionsApi.remove(transaction.id);
          showSuccess('Проводка удалена');
          loadTransactions(filterForm.getFieldsValue());
        } catch (error) {
          showError(error, { action: 'delete', entity: 'проводку' });
        }
      },
    });
  };

  const columns: ColumnsType<Transaction> = [
    {
      title: 'Дата',
      dataIndex: 'date',
      key: 'date',
      width: 120,
      render: (date: string) => formatDate(date),
    },
    {
      title: 'Дебет',
      key: 'debit',
      width: 200,
      render: (_, record) =>
        record.debit_account
          ? `${record.debit_account.number} - ${record.debit_account.name}`
          : '—',
    },
    {
      title: 'Кредит',
      key: 'credit',
      width: 200,
      render: (_, record) =>
        record.credit_account
          ? `${record.credit_account.number} - ${record.credit_account.name}`
          : '—',
    },
    {
      title: 'Сумма',
      dataIndex: 'amount',
      key: 'amount',
      width: 150,
      render: (amount?: number) => (amount ? formatMoney(amount) : '—'),
    },
    {
      title: 'Контрагент',
      key: 'counterparty',
      width: 180,
      render: (_, record) => record.counterparty?.name || '—',
    },
    {
      title: 'Основание',
      dataIndex: 'description',
      key: 'description',
      ellipsis: true,
    },
    {
      title: 'Тип',
      dataIndex: 'is_complex',
      key: 'is_complex',
      width: 100,
      render: (isComplex: boolean) => (
        <Tag color={isComplex ? 'purple' : 'default'}>
          {isComplex ? 'Сложная' : 'Простая'}
        </Tag>
      ),
    },
    {
      title: 'Баланс',
      dataIndex: 'is_balanced',
      key: 'is_balanced',
      width: 100,
      render: (isBalanced: boolean) =>
        isBalanced ? (
          <Tag icon={<CheckCircleOutlined />} color="success">
            ✓
          </Tag>
        ) : (
          <Tag icon={<CloseCircleOutlined />} color="error">
            ✗
          </Tag>
        ),
    },
    {
      title: 'Действия',
      key: 'actions',
      width: 110,
      fixed: 'right',
      align: 'center',
      render: (_, record) => (
        <Space size={0}>
          <Tooltip title="Просмотр">
            <Button
              type="link"
              icon={<EyeOutlined />}
              onClick={() => navigate(`/transactions/${record.id}`)}
              size="small"
            />
          </Tooltip>
          {canEdit() && (
            <>
              <Tooltip title="Редактировать">
                <Button
                  type="link"
                  icon={<EditOutlined />}
                  onClick={() => navigate(`/transactions/${record.id}/edit`)}
                  size="small"
                />
              </Tooltip>
              <Tooltip title="Удалить">
                <Button
                  type="link"
                  danger
                  icon={<DeleteOutlined />}
                  onClick={() => handleDelete(record)}
                  size="small"
                />
              </Tooltip>
            </>
          )}
        </Space>
      ),
    },
  ];

  return (
    <div>
      <div className="flex flex-col sm:flex-row sm:justify-between sm:items-center gap-3 mb-6">
        <h2 className="text-2xl font-bold m-0">Журнал проводок</h2>
        {canEdit() && (
          <Space>
            <Button icon={<UploadOutlined />} onClick={() => setImportOpen(true)}>
              Импорт из Excel
            </Button>
            <Button
              type="primary"
              icon={<PlusOutlined />}
              onClick={() => navigate('/transactions/new')}
            >
              Добавить операцию
            </Button>
          </Space>
        )}
      </div>

      <Form form={filterForm} layout={isMobile ? 'vertical' : 'inline'} className="mb-4">
        <Form.Item name="dateRange" label="Период">
          <RangePicker format="DD.MM.YYYY" />
        </Form.Item>
        <Form.Item name="account_id" label="Счёт">
          <Select
            allowClear
            showSearch
            placeholder="Выберите счёт"
            style={{ width: isMobile ? '100%' : 250 }}
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
        <Form.Item name="counterparty_id" label="Контрагент">
          <Select
            allowClear
            showSearch
            placeholder="Выберите контрагента"
            style={{ width: isMobile ? '100%' : 200 }}
            filterOption={(input, option) =>
              (option?.label?.toString() ?? '')
                .toLowerCase()
                .includes(input.toLowerCase())
            }
            options={counterparties.map((c) => ({
              value: c.id,
              label: c.name,
            }))}
          />
        </Form.Item>
        <Form.Item>
          <Space
            direction={isMobile ? 'vertical' : 'horizontal'}
            style={isMobile ? { width: '100%' } : undefined}
          >
            <Button type="primary" block={isMobile} onClick={handleFilter}>
              Применить
            </Button>
            <Button block={isMobile} onClick={handleResetFilter}>
              Сбросить
            </Button>
          </Space>
        </Form.Item>
      </Form>

      <Table<Transaction>
        columns={columns}
        dataSource={transactions}
        rowKey="id"
        loading={loading}
        scroll={{ x: 1400 }}
        pagination={{
          pageSize: 20,
          showTotal: (total) => `Всего: ${total}`,
        }}
      />

      <ImportTransactionsModal
        open={importOpen}
        onClose={() => setImportOpen(false)}
        onSuccess={() => loadTransactions(filterForm.getFieldsValue())}
      />
    </div>
  );
}
