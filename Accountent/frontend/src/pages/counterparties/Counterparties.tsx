import { useState, useEffect, useCallback } from 'react';
import {
  Table,
  Button,
  Space,
  Modal,
  Form,
  Input,
  Tooltip,
} from 'antd';
import {
  PlusOutlined,
  EditOutlined,
  DeleteOutlined,
  ExclamationCircleOutlined,
  SearchOutlined,
} from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { isAxiosError } from 'axios';
import { Counterparty } from '../../types';
import { useAuth } from '../../hooks/useAuth';
import { useApiError } from '../../hooks/useApiError';
import { counterpartiesApi } from '../../api';

const { confirm } = Modal;
const { Search } = Input;

interface CounterpartyFormValues {
  name: string;
  inn?: string;
  contact?: string;
}

export default function Counterparties() {
  const { canEdit } = useAuth();
  const { showError, showSuccess } = useApiError();
  const [counterparties, setCounterparties] = useState<Counterparty[]>([]);
  const [filteredCounterparties, setFilteredCounterparties] = useState<Counterparty[]>([]);
  const [loading, setLoading] = useState(false);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingCounterparty, setEditingCounterparty] = useState<Counterparty | null>(null);
  const [form] = Form.useForm<CounterpartyFormValues>();

  const loadCounterparties = useCallback(async () => {
    setLoading(true);
    try {
      const response = await counterpartiesApi.getAll();
      setCounterparties(response.data);
      setFilteredCounterparties(response.data);
    } catch (error) {
      showError(error, { action: 'load', entity: 'контрагентов' });
    } finally {
      setLoading(false);
    }
  }, [showError]);

  useEffect(() => {
    loadCounterparties();
  }, [loadCounterparties]);

  const handleSearch = (value: string) => {
    if (!value) {
      setFilteredCounterparties(counterparties);
      return;
    }
    const lower = value.toLowerCase();
    setFilteredCounterparties(
      counterparties.filter(
        (c) =>
          c.name.toLowerCase().includes(lower) ||
          c.inn?.toLowerCase().includes(lower),
      ),
    );
  };

  const handleAdd = () => {
    setEditingCounterparty(null);
    form.resetFields();
    setIsModalOpen(true);
  };

  const handleEdit = (counterparty: Counterparty) => {
    setEditingCounterparty(counterparty);
    form.setFieldsValue({
      name: counterparty.name,
      inn: counterparty.inn,
      contact: counterparty.contact,
    });
    setIsModalOpen(true);
  };

  const handleDelete = (counterparty: Counterparty) => {
    confirm({
      title: 'Удалить контрагента?',
      icon: <ExclamationCircleOutlined />,
      content: `Вы действительно хотите удалить контрагента "${counterparty.name}"?`,
      okText: 'Удалить',
      okType: 'danger',
      cancelText: 'Отмена',
      onOk: async () => {
        try {
          await counterpartiesApi.remove(counterparty.id);
          showSuccess('Контрагент удалён');
          loadCounterparties();
        } catch (error) {
          showError(error, { action: 'delete', entity: 'контрагента' });
        }
      },
    });
  };

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
      if (editingCounterparty) {
        await counterpartiesApi.update(editingCounterparty.id, values);
        showSuccess('Контрагент обновлён');
      } else {
        await counterpartiesApi.create(values);
        showSuccess('Контрагент создан');
      }
      setIsModalOpen(false);
      loadCounterparties();
    } catch (error) {
      if (isAxiosError(error)) {
        showError(error, {
          action: editingCounterparty ? 'update' : 'create',
          entity: 'контрагента',
        });
      }
    }
  };

  const validateINN = (_: unknown, value?: string) => {
    if (!value) {
      return Promise.resolve();
    }
    if (!/^\d{10}$|^\d{12}$/.test(value)) {
      return Promise.reject(new Error('ИНН должен содержать 10 или 12 цифр'));
    }
    return Promise.resolve();
  };

  const columns: ColumnsType<Counterparty> = [
    {
      title: 'Наименование',
      dataIndex: 'name',
      key: 'name',
      sorter: (a, b) => a.name.localeCompare(b.name),
    },
    {
      title: 'ИНН',
      dataIndex: 'inn',
      key: 'inn',
      width: 150,
      render: (inn?: string) => inn || '—',
    },
    {
      title: 'Контакты',
      dataIndex: 'contact',
      key: 'contact',
      ellipsis: true,
      render: (contact?: string) => contact || '—',
    },
    {
      title: 'Действия',
      key: 'actions',
      width: 150,
      render: (_, record) =>
        canEdit() ? (
          <Space>
            <Tooltip title="Редактировать">
              <Button
                type="link"
                icon={<EditOutlined />}
                onClick={() => handleEdit(record)}
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
          </Space>
        ) : null,
    },
  ];

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-bold m-0">Контрагенты</h2>
        {canEdit() && (
          <Button type="primary" icon={<PlusOutlined />} onClick={handleAdd}>
            Добавить контрагента
          </Button>
        )}
      </div>

      <div className="mb-4">
        <Search
          placeholder="Поиск по наименованию или ИНН"
          allowClear
          enterButton={<SearchOutlined />}
          onSearch={handleSearch}
          onChange={(e) => handleSearch(e.target.value)}
          style={{ width: 400 }}
        />
      </div>

      <Table<Counterparty>
        columns={columns}
        dataSource={filteredCounterparties}
        rowKey="id"
        loading={loading}
        pagination={{
          pageSize: 20,
          showTotal: (total) => `Всего: ${total}`,
        }}
      />

      <Modal
        title={
          editingCounterparty ? 'Редактировать контрагента' : 'Добавить контрагента'
        }
        open={isModalOpen}
        onOk={handleSubmit}
        onCancel={() => setIsModalOpen(false)}
        okText="Сохранить"
        cancelText="Отмена"
      >
        <Form form={form} layout="vertical">
          <Form.Item
            name="name"
            label="Наименование"
            rules={[{ required: true, message: 'Введите наименование' }]}
          >
            <Input placeholder="ООО Рога и Копыта" />
          </Form.Item>
          <Form.Item name="inn" label="ИНН" rules={[{ validator: validateINN }]}>
            <Input placeholder="1234567890" maxLength={12} />
          </Form.Item>
          <Form.Item
            name="contact"
            label="Контакты"
            rules={[{ max: 500, message: 'Максимум 500 символов' }]}
          >
            <Input.TextArea
              rows={3}
              placeholder="Телефон, email, адрес"
              maxLength={500}
              showCount
            />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}
