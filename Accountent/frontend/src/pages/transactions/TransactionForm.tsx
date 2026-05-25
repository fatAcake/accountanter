import { useState, useEffect } from 'react';
import {
  Form,
  Input,
  Select,
  DatePicker,
  InputNumber,
  Button,
  Radio,
  Table,
  Space,
  Card,
  message,
  Alert,
} from 'antd';
import { PlusOutlined, DeleteOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import type { Dayjs } from 'dayjs';
import { isAxiosError } from 'axios';
import { useNavigate, useParams } from 'react-router-dom';
import dayjs from 'dayjs';
import { TransactionLine, Account, Counterparty } from '../../types';
import { useApiError } from '../../hooks/useApiError';
import {
  accountsApi,
  counterpartiesApi,
  transactionsApi,
  type TransactionPayload,
} from '../../api';
import { formatMoney } from '../../utils/format';

const { TextArea } = Input;

interface SimpleFormValues {
  date: Dayjs;
  debit_account_id: number;
  credit_account_id: number;
  amount: number;
  counterparty_id?: number;
  description: string;
}

const emptyLine = (): TransactionLine => ({
  account_id: 0,
  side: 'debit',
  amount: 0,
});

export default function TransactionForm() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { showError, showSuccess } = useApiError();
  const [form] = Form.useForm<SimpleFormValues>();
  const [loading, setLoading] = useState(false);
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [counterparties, setCounterparties] = useState<Counterparty[]>([]);
  const [isComplex, setIsComplex] = useState(false);
  const [complexLines, setComplexLines] = useState<TransactionLine[]>([
    emptyLine(),
    { ...emptyLine(), side: 'credit' },
  ]);

  useEffect(() => {
    void loadAccounts();
    void loadCounterparties();
    if (id) {
      void loadTransaction();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps -- загрузка при монтировании и смене id
  }, [id]);

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

  const loadTransaction = async () => {
    try {
      const response = await transactionsApi.getById(id!);
      const transaction = response.data;
      form.setFieldsValue({
        date: dayjs(transaction.date),
        debit_account_id: transaction.debit_account_id,
        credit_account_id: transaction.credit_account_id,
        amount: transaction.amount,
        counterparty_id: transaction.counterparty_id,
        description: transaction.description,
      });
      setIsComplex(transaction.is_complex);
      if (transaction.is_complex && transaction.lines) {
        setComplexLines(transaction.lines);
      }
    } catch (error) {
      showError(error, { action: 'load', entity: 'проводку' });
    }
  };

  const calculateBalance = () => {
    const debitSum = complexLines
      .filter((l) => l.side === 'debit')
      .reduce((sum, l) => sum + (l.amount || 0), 0);
    const creditSum = complexLines
      .filter((l) => l.side === 'credit')
      .reduce((sum, l) => sum + (l.amount || 0), 0);
    const difference = debitSum - creditSum;
    return {
      debitSum,
      creditSum,
      difference,
      isBalanced: Math.abs(difference) < 0.01,
    };
  };

  const handleSubmit = async () => {
    try {
      await form.validateFields();
      const values = form.getFieldsValue();
      const data: TransactionPayload = {
        date: values.date.format('YYYY-MM-DD'),
        description: values.description,
      };

      if (isComplex) {
        const balance = calculateBalance();
        if (!balance.isBalanced) {
          message.error(
            'Проводка не сбалансирована! Дебет должен равняться кредиту',
          );
          return;
        }
        if (complexLines.length < 2) {
          message.error('Минимум 2 строки для сложной проводки');
          return;
        }
        data.lines = complexLines.map((l) => ({
          account_id: l.account_id,
          side: l.side,
          amount: l.amount,
          counterparty_id: l.counterparty_id ?? null,
        }));
      } else {
        if (!values.debit_account_id || !values.credit_account_id) {
          message.error('Выберите счета дебета и кредита');
          return;
        }
        if (values.debit_account_id === values.credit_account_id) {
          message.error('Счета дебета и кредита должны быть разными');
          return;
        }
        data.debit_account_id = values.debit_account_id;
        data.credit_account_id = values.credit_account_id;
        data.amount = values.amount;
        data.counterparty_id = values.counterparty_id ?? null;
      }

      setLoading(true);
      if (id) {
        await transactionsApi.update(id, data);
        showSuccess('Проводка обновлена');
      } else {
        await transactionsApi.create(data);
        showSuccess('Проводка создана');
      }
      navigate('/transactions');
    } catch (error) {
      if (isAxiosError(error)) {
        showError(error, { action: 'save', entity: 'проводку' });
      }
    } finally {
      setLoading(false);
    }
  };

  const addComplexLine = () => {
    setComplexLines([...complexLines, emptyLine()]);
  };

  const removeComplexLine = (index: number) => {
    if (complexLines.length > 2) {
      setComplexLines(complexLines.filter((_, i) => i !== index));
    } else {
      message.warning('Минимум 2 строки должны быть в сложной проводке');
    }
  };

  const updateComplexLine = <K extends keyof TransactionLine>(
    index: number,
    field: K,
    value: TransactionLine[K],
  ) => {
    const newLines = [...complexLines];
    newLines[index] = { ...newLines[index], [field]: value };
    setComplexLines(newLines);
  };

  const complexColumns: ColumnsType<TransactionLine> = [
    {
      title: 'Счёт',
      key: 'account',
      render: (_, record, index) => (
        <Select
          showSearch
          placeholder="Выберите счёт"
          style={{ width: '100%' }}
          value={record.account_id || undefined}
          onChange={(value) => updateComplexLine(index, 'account_id', value)}
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
      ),
    },
    {
      title: 'Дт/Кт',
      key: 'side',
      width: 100,
      render: (_, record, index) => (
        <Select
          value={record.side}
          onChange={(value: 'debit' | 'credit') =>
            updateComplexLine(index, 'side', value)
          }
          style={{ width: '100%' }}
          options={[
            { value: 'debit', label: 'Дебет' },
            { value: 'credit', label: 'Кредит' },
          ]}
        />
      ),
    },
    {
      title: 'Сумма',
      key: 'amount',
      width: 150,
      render: (_, record, index) => (
        <InputNumber
          min={0.01}
          precision={2}
          style={{ width: '100%' }}
          value={record.amount}
          onChange={(value) => updateComplexLine(index, 'amount', value || 0)}
        />
      ),
    },
    {
      title: 'Контрагент',
      key: 'counterparty',
      render: (_, record, index) => (
        <Select
          allowClear
          showSearch
          placeholder="Опционально"
          style={{ width: '100%' }}
          value={record.counterparty_id || undefined}
          onChange={(value) => updateComplexLine(index, 'counterparty_id', value)}
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
      ),
    },
    {
      title: '',
      key: 'actions',
      width: 50,
      render: (_, __, index) => (
        <Button
          type="link"
          danger
          icon={<DeleteOutlined />}
          onClick={() => removeComplexLine(index)}
          disabled={complexLines.length <= 2}
        />
      ),
    },
  ];

  const balance = isComplex ? calculateBalance() : null;

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-bold m-0">
          {id ? 'Редактировать проводку' : 'Новая проводка'}
        </h2>
      </div>

      <Card>
        <Form form={form} layout="vertical">
          <Form.Item label="Тип проводки">
            <Radio.Group
              value={isComplex}
              onChange={(e) => setIsComplex(e.target.value)}
              disabled={!!id}
            >
              <Radio value={false}>Простая</Radio>
              <Radio value={true}>Сложная (несколько строк)</Radio>
            </Radio.Group>
          </Form.Item>

          <Form.Item
            name="date"
            label="Дата"
            rules={[{ required: true, message: 'Выберите дату' }]}
          >
            <DatePicker format="DD.MM.YYYY" style={{ width: 200 }} />
          </Form.Item>

          {!isComplex ? (
            <>
              <Form.Item
                name="debit_account_id"
                label="Счёт дебета"
                rules={[{ required: true, message: 'Выберите счёт дебета' }]}
              >
                <Select
                  showSearch
                  placeholder="Выберите счёт"
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
              <Form.Item
                name="credit_account_id"
                label="Счёт кредита"
                rules={[{ required: true, message: 'Выберите счёт кредита' }]}
              >
                <Select
                  showSearch
                  placeholder="Выберите счёт"
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
              <Form.Item
                name="amount"
                label="Сумма"
                rules={[
                  { required: true, message: 'Введите сумму' },
                  {
                    type: 'number',
                    min: 0.01,
                    message: 'Сумма должна быть больше 0',
                  },
                ]}
              >
                <InputNumber
                  min={0.01}
                  precision={2}
                  style={{ width: 200 }}
                  placeholder="0.00"
                  addonAfter="₽"
                />
              </Form.Item>
              <Form.Item name="counterparty_id" label="Контрагент (субконто)">
                <Select
                  allowClear
                  showSearch
                  placeholder="Опционально"
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
            </>
          ) : (
            <div className="mb-4">
              <div className="flex justify-between items-center mb-2">
                <h3 className="text-lg font-semibold">Строки проводки</h3>
                <Button icon={<PlusOutlined />} onClick={addComplexLine}>
                  Добавить строку
                </Button>
              </div>
              <Table<TransactionLine>
                columns={complexColumns}
                dataSource={complexLines}
                rowKey={(_, index) => String(index)}
                pagination={false}
                size="small"
              />
              {balance && (
                <Alert
                  type={balance.isBalanced ? 'success' : 'error'}
                  className="mt-4"
                  title={
                    <div className="flex justify-between flex-wrap gap-2">
                      <span>Дебет: {formatMoney(balance.debitSum)}</span>
                      <span>Кредит: {formatMoney(balance.creditSum)}</span>
                      <span>
                        Разница: {formatMoney(Math.abs(balance.difference))}
                        {balance.isBalanced ? ' ✓' : ' ✗'}
                      </span>
                    </div>
                  }
                />
              )}
            </div>
          )}

          <Form.Item
            name="description"
            label="Основание"
            rules={[{ required: true, message: 'Введите основание' }]}
          >
            <TextArea rows={3} placeholder="Описание операции" />
          </Form.Item>

          <Form.Item>
            <Space>
              <Button type="primary" onClick={handleSubmit} loading={loading}>
                Сохранить
              </Button>
              <Button onClick={() => navigate('/transactions')}>Отмена</Button>
            </Space>
          </Form.Item>
        </Form>
      </Card>
    </div>
  );
}
