import { useState, useEffect, useCallback } from 'react';
import {
  Table,
  Button,
  Tag,
  Space,
  Modal,
  Form,
  Input,
  Select,
  message,
  Tooltip,
} from 'antd';
import {
  PlusOutlined,
  EditOutlined,
  DeleteOutlined,
  LockOutlined,
  ExclamationCircleOutlined,
} from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { isAxiosError } from 'axios';
import { Account, AccountType } from '../../types';
import { useAuth } from '../../hooks/useAuth';
import { useApiError } from '../../hooks/useApiError';
import { accountsApi } from '../../api';

const { confirm } = Modal;

const ACCOUNT_TYPE_LABELS: Record<AccountType, { color: string; label: string }> = {
  active: { color: 'blue', label: 'Активный' },
  passive: { color: 'green', label: 'Пассивный' },
  active_passive: { color: 'orange', label: 'Активно-пассивный' },
};

interface AccountFormValues {
  number: string;
  name: string;
  type: AccountType;
  parent_id?: number;
}

const flattenAccounts = (items: Account[]): Account[] => {
  const result: Account[] = [];
  const walk = (list: Account[]) => {
    list.forEach((item) => {
      result.push(item);
      if (item.children?.length) {
        walk(item.children);
      }
    });
  };
  walk(items);
  return result;
};

export default function ChartOfAccounts() {
  const { canEdit } = useAuth();
  const { showError, showSuccess } = useApiError();
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [loading, setLoading] = useState(false);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingAccount, setEditingAccount] = useState<Account | null>(null);
  const [form] = Form.useForm<AccountFormValues>();

  const loadAccounts = useCallback(async () => {
    setLoading(true);
    try {
      const response = await accountsApi.getTree();
      setAccounts(response.data);
    } catch (error) {
      showError(error, { action: 'load', entity: 'план счетов' });
    } finally {
      setLoading(false);
    }
  }, [showError]);

  useEffect(() => {
    loadAccounts();
  }, [loadAccounts]);

  const getAccountTypeTag = (type: AccountType) => {
    const config = ACCOUNT_TYPE_LABELS[type];
    return <Tag color={config.color}>{config.label}</Tag>;
  };

  const handleAdd = () => {
    setEditingAccount(null);
    form.resetFields();
    setIsModalOpen(true);
  };

  const handleEdit = (account: Account) => {
    setEditingAccount(account);
    form.setFieldsValue({
      number: account.number,
      name: account.name,
      type: account.type,
      parent_id: account.parent_id ?? undefined,
    });
    setIsModalOpen(true);
  };

  const handleDelete = (account: Account) => {
    if (account.is_system) {
      message.error('Системный счёт нельзя удалить');
      return;
    }
    confirm({
      title: 'Удалить счёт?',
      icon: <ExclamationCircleOutlined />,
      content: `Вы действительно хотите удалить счёт "${account.number} - ${account.name}"?`,
      okText: 'Удалить',
      okType: 'danger',
      cancelText: 'Отмена',
      onOk: async () => {
        try {
          await accountsApi.remove(account.id);
          showSuccess('Счёт удалён');
          loadAccounts();
        } catch (error) {
          showError(error, { action: 'delete', entity: 'счёт' });
        }
      },
    });
  };

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
      if (editingAccount) {
        const { name, type, parent_id } = values;
        await accountsApi.update(editingAccount.id, {
          name,
          type,
          parent_id: parent_id ?? null,
        });
        showSuccess('Счёт обновлён');
      } else {
        await accountsApi.create({
          ...values,
          parent_id: values.parent_id ?? null,
        });
        showSuccess('Счёт создан');
      }
      setIsModalOpen(false);
      loadAccounts();
    } catch (error) {
      if (isAxiosError(error)) {
        showError(error, {
          action: editingAccount ? 'update' : 'create',
          entity: 'счёт',
        });
      }
    }
  };

  const columns: ColumnsType<Account> = [
    { title: 'Номер', dataIndex: 'number', key: 'number', width: 120 },
    { title: 'Наименование', dataIndex: 'name', key: 'name' },
    {
      title: 'Тип',
      dataIndex: 'type',
      key: 'type',
      width: 180,
      render: (type: AccountType) => getAccountTypeTag(type),
    },
    {
      title: 'Системный',
      dataIndex: 'is_system',
      key: 'is_system',
      width: 120,
      render: (isSystem: boolean) =>
        isSystem ? (
          <Tag icon={<LockOutlined />} color="default">
            Да
          </Tag>
        ) : (
          <Tag>Нет</Tag>
        ),
    },
    {
      title: 'Действия',
      key: 'actions',
      width: 150,
      render: (_, record) => (
        <Space>
          {canEdit() && (
            <>
              <Tooltip title="Редактировать">
                <Button
                  type="link"
                  icon={<EditOutlined />}
                  onClick={() => handleEdit(record)}
                  size="small"
                />
              </Tooltip>
              <Tooltip
                title={
                  record.is_system ? 'Системный счёт нельзя удалить' : 'Удалить'
                }
              >
                <Button
                  type="link"
                  danger
                  icon={<DeleteOutlined />}
                  onClick={() => handleDelete(record)}
                  disabled={record.is_system}
                  size="small"
                />
              </Tooltip>
            </>
          )}
        </Space>
      ),
    },
  ];

  const flatAccounts = flattenAccounts(accounts);

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-bold m-0">План счетов</h2>
        {canEdit() && (
          <Button type="primary" icon={<PlusOutlined />} onClick={handleAdd}>
            Добавить счёт
          </Button>
        )}
      </div>

      <Table<Account>
        columns={columns}
        dataSource={accounts}
        rowKey="id"
        loading={loading}
        expandable={{ defaultExpandAllRows: true }}
        pagination={false}
      />

      <Modal
        title={editingAccount ? 'Редактировать счёт' : 'Добавить счёт'}
        open={isModalOpen}
        onOk={handleSubmit}
        onCancel={() => setIsModalOpen(false)}
        okText="Сохранить"
        cancelText="Отмена"
      >
        <Form form={form} layout="vertical">
          <Form.Item
            name="number"
            label="Номер счёта"
            rules={[{ required: true, message: 'Введите номер счёта' }]}
          >
            <Input placeholder="01" disabled={!!editingAccount} />
          </Form.Item>
          <Form.Item
            name="name"
            label="Наименование"
            rules={[{ required: true, message: 'Введите наименование' }]}
          >
            <Input placeholder="Основные средства" />
          </Form.Item>
          <Form.Item
            name="type"
            label="Тип счёта"
            rules={[{ required: true, message: 'Выберите тип счёта' }]}
          >
            <Select
              options={[
                { value: 'active', label: 'Активный' },
                { value: 'passive', label: 'Пассивный' },
                { value: 'active_passive', label: 'Активно-пассивный' },
              ]}
            />
          </Form.Item>
          <Form.Item name="parent_id" label="Родительский счёт">
            <Select
              allowClear
              placeholder="Выберите родительский счёт"
              showSearch
              filterOption={(input, option) =>
                (option?.label?.toString() ?? '')
                  .toLowerCase()
                  .includes(input.toLowerCase())
              }
              options={flatAccounts
                .filter((a) => a.id !== editingAccount?.id)
                .map((a) => ({
                  value: a.id,
                  label: `${a.number} - ${a.name}`,
                }))}
            />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}
