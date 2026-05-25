import { useEffect, useState } from 'react';
import { Card, Descriptions, Table, Tag, Button, Spin } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useParams, useNavigate } from 'react-router-dom';
import {
  ArrowLeftOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
} from '@ant-design/icons';
import { Transaction, TransactionLine } from '../../types';
import { useApiError } from '../../hooks/useApiError';
import { transactionsApi } from '../../api';
import { formatDate, formatMoney } from '../../utils/format';

export default function TransactionView() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { showError } = useApiError();
  const [transaction, setTransaction] = useState<Transaction | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const loadTransaction = async () => {
      try {
        const response = await transactionsApi.getById(id!);
        setTransaction(response.data);
      } catch (error) {
        showError(error, { action: 'load', entity: 'проводку' });
      } finally {
        setLoading(false);
      }
    };
    loadTransaction();
    // eslint-disable-next-line react-hooks/exhaustive-deps -- загрузка при смене id
  }, [id]);

  if (loading) {
    return (
      <div className="flex justify-center items-center min-h-[400px]">
        <Spin size="large" />
      </div>
    );
  }

  if (!transaction) {
    return (
      <Card>
        <p>Проводка не найдена</p>
      </Card>
    );
  }

  const complexColumns: ColumnsType<TransactionLine> = [
    {
      title: 'Счёт',
      key: 'account',
      render: (_, record) =>
        record.account
          ? `${record.account.number} - ${record.account.name}`
          : '—',
    },
    {
      title: 'Сторона',
      dataIndex: 'side',
      key: 'side',
      render: (side: 'debit' | 'credit') => (
        <Tag color={side === 'debit' ? 'blue' : 'green'}>
          {side === 'debit' ? 'Дебет' : 'Кредит'}
        </Tag>
      ),
    },
    {
      title: 'Сумма',
      dataIndex: 'amount',
      key: 'amount',
      render: (amount: number) => formatMoney(amount),
    },
    {
      title: 'Контрагент',
      key: 'counterparty',
      render: (_, record) => record.counterparty?.name || '—',
    },
  ];

  const lines = transaction.lines ?? [];

  return (
    <div>
      <div className="mb-6">
        <Button
          icon={<ArrowLeftOutlined />}
          onClick={() => navigate('/transactions')}
        >
          Назад к списку
        </Button>
      </div>

      <Card
        title={`Проводка №${transaction.id}`}
        extra={
          <Tag
            icon={
              transaction.is_balanced ? (
                <CheckCircleOutlined />
              ) : (
                <CloseCircleOutlined />
              )
            }
            color={transaction.is_balanced ? 'success' : 'error'}
          >
            {transaction.is_balanced ? 'Сбалансирована' : 'Не сбалансирована'}
          </Tag>
        }
      >
        <Descriptions bordered column={2}>
          <Descriptions.Item label="Дата">
            {formatDate(transaction.date)}
          </Descriptions.Item>
          <Descriptions.Item label="Тип">
            <Tag color={transaction.is_complex ? 'purple' : 'default'}>
              {transaction.is_complex ? 'Сложная' : 'Простая'}
            </Tag>
          </Descriptions.Item>
          {!transaction.is_complex && (
            <>
              <Descriptions.Item label="Счёт дебета">
                {transaction.debit_account
                  ? `${transaction.debit_account.number} - ${transaction.debit_account.name}`
                  : '—'}
              </Descriptions.Item>
              <Descriptions.Item label="Счёт кредита">
                {transaction.credit_account
                  ? `${transaction.credit_account.number} - ${transaction.credit_account.name}`
                  : '—'}
              </Descriptions.Item>
              <Descriptions.Item label="Сумма">
                {transaction.amount ? formatMoney(transaction.amount) : '—'}
              </Descriptions.Item>
              <Descriptions.Item label="Контрагент">
                {transaction.counterparty?.name || '—'}
              </Descriptions.Item>
            </>
          )}
          <Descriptions.Item label="Основание" span={2}>
            {transaction.description}
          </Descriptions.Item>
        </Descriptions>

        {transaction.is_complex && lines.length > 0 && (
          <div className="mt-6">
            <h3 className="text-lg font-semibold mb-4">Строки проводки</h3>
            <Table<TransactionLine>
              columns={complexColumns}
              dataSource={lines}
              rowKey={(record) => String(record.id ?? record.account_id)}
              pagination={false}
              summary={() => {
                const debitSum = lines
                  .filter((l) => l.side === 'debit')
                  .reduce((sum, l) => sum + l.amount, 0);
                const creditSum = lines
                  .filter((l) => l.side === 'credit')
                  .reduce((sum, l) => sum + l.amount, 0);
                return (
                  <Table.Summary>
                    <Table.Summary.Row>
                      <Table.Summary.Cell index={0}>
                        <strong>Итого</strong>
                      </Table.Summary.Cell>
                      <Table.Summary.Cell index={1} />
                      <Table.Summary.Cell index={2}>
                        <div>
                          <div>Дебет: {formatMoney(debitSum)}</div>
                          <div>Кредит: {formatMoney(creditSum)}</div>
                        </div>
                      </Table.Summary.Cell>
                      <Table.Summary.Cell index={3} />
                    </Table.Summary.Row>
                  </Table.Summary>
                );
              }}
            />
          </div>
        )}
      </Card>
    </div>
  );
}
