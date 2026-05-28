import { useState, useEffect, useCallback } from 'react';
import {
  Table,
  Button,
  Space,
  Modal,
  Form,
  Input,
  Select,
  Tag,
  Tooltip,
  Tabs,
  Upload,
  Alert,
} from 'antd';
import type { UploadFile } from 'antd';
import {
  PlusOutlined,
  EditOutlined,
  DeleteOutlined,
  ExclamationCircleOutlined,
  SearchOutlined,
  UploadOutlined,
  DisconnectOutlined,
  HistoryOutlined,
  UserOutlined,
} from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { isAxiosError } from 'axios';
import {
  AdminAuditLog,
  ImportUsersResult,
  User,
  UserRole,
  UserSession,
} from '../../types';
import { useAuth } from '../../hooks/useAuth';
import { useApiError } from '../../hooks/useApiError';
import { usersApi } from '../../api/users';
import { formatDateTime } from '../../utils/format';

const { confirm } = Modal;
const { Search } = Input;

const ROLE_LABELS: Record<UserRole, string> = {
  admin: 'Администратор',
  accountant: 'Бухгалтер',
  observer: 'Наблюдатель',
};

const ROLE_COLORS: Record<UserRole, string> = {
  admin: 'red',
  accountant: 'blue',
  observer: 'default',
};

const ACTION_LABELS: Record<string, string> = {
  'user.create': 'Создание пользователя',
  'user.update': 'Изменение пользователя',
  'user.delete': 'Удаление пользователя',
  'user.import': 'Импорт из CSV',
  'session.revoke': 'Отзыв сессии',
};

const CSV_TEMPLATE = `nickname,email,password,role
Иван Иванов,ivan@example.com,Password1!,observer
Мария Бухгалтерова,maria@example.com,Password1!,accountant`;

interface UserFormValues {
  nickname: string;
  email: string;
  password?: string;
  role: UserRole;
}

export default function AdminUsers() {
  const { user: currentUser } = useAuth();
  const { showError, showSuccess } = useApiError();
  const [activeTab, setActiveTab] = useState('users');

  const [users, setUsers] = useState<User[]>([]);
  const [sessions, setSessions] = useState<UserSession[]>([]);
  const [auditLogs, setAuditLogs] = useState<AdminAuditLog[]>([]);
  const [loading, setLoading] = useState(false);

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isImportOpen, setIsImportOpen] = useState(false);
  const [importFile, setImportFile] = useState<UploadFile | null>(null);
  const [importResult, setImportResult] = useState<ImportUsersResult | null>(null);
  const [importing, setImporting] = useState(false);

  const [editingUser, setEditingUser] = useState<User | null>(null);
  const [form] = Form.useForm<UserFormValues>();

  const loadUsers = useCallback(
    async (search?: string) => {
      setLoading(true);
      try {
        const response = await usersApi.getAll(search);
        setUsers(response.data);
      } catch (error) {
        showError(error, { action: 'load', entity: 'пользователей' });
      } finally {
        setLoading(false);
      }
    },
    [showError],
  );

  const loadSessions = useCallback(async () => {
    setLoading(true);
    try {
      const response = await usersApi.getSessions();
      setSessions(response.data);
    } catch (error) {
      showError(error, { action: 'load', entity: 'сессий' });
    } finally {
      setLoading(false);
    }
  }, [showError]);

  const loadAuditLogs = useCallback(
    async (search?: string) => {
      setLoading(true);
      try {
        const response = await usersApi.getAuditLogs({ search, limit: 200 });
        setAuditLogs(response.data);
      } catch (error) {
        showError(error, { action: 'load', entity: 'журнала' });
      } finally {
        setLoading(false);
      }
    },
    [showError],
  );

  useEffect(() => {
    if (activeTab === 'users') loadUsers();
    else if (activeTab === 'sessions') loadSessions();
    else if (activeTab === 'audit') loadAuditLogs();
  }, [activeTab, loadUsers, loadSessions, loadAuditLogs]);

  const handleAdd = () => {
    setEditingUser(null);
    form.resetFields();
    form.setFieldsValue({ role: 'observer' });
    setIsModalOpen(true);
  };

  const handleEdit = (record: User) => {
    setEditingUser(record);
    form.setFieldsValue({
      nickname: record.nickname,
      email: record.email,
      role: record.role,
      password: undefined,
    });
    setIsModalOpen(true);
  };

  const handleDelete = (record: User) => {
    confirm({
      title: 'Удалить пользователя?',
      icon: <ExclamationCircleOutlined />,
      content: `Удалить учётную запись «${record.nickname}» (${record.email})?`,
      okText: 'Удалить',
      okType: 'danger',
      cancelText: 'Отмена',
      onOk: async () => {
        try {
          await usersApi.remove(record.id);
          showSuccess('Пользователь удалён');
          loadUsers();
        } catch (error) {
          showError(error, { action: 'delete', entity: 'пользователя' });
        }
      },
    });
  };

  const handleRevokeSession = (session: UserSession) => {
    confirm({
      title: 'Отозвать сессию?',
      icon: <ExclamationCircleOutlined />,
      content: `Завершить вход пользователя «${session.nickname}»? Потребуется повторный вход.`,
      okText: 'Отозвать',
      okType: 'danger',
      cancelText: 'Отмена',
      onOk: async () => {
        try {
          await usersApi.revokeSession(session.user_id);
          showSuccess('Сессия отозвана');
          loadSessions();
        } catch (error) {
          showError(error, { action: 'update', entity: 'сессии' });
        }
      },
    });
  };

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
      if (editingUser) {
        const payload = {
          nickname: values.nickname,
          email: values.email,
          role: values.role,
          ...(values.password ? { password: values.password } : {}),
        };
        await usersApi.update(editingUser.id, payload);
        showSuccess('Пользователь обновлён');
      } else {
        await usersApi.create({
          nickname: values.nickname,
          email: values.email,
          password: values.password!,
          role: values.role,
        });
        showSuccess('Пользователь создан');
      }
      setIsModalOpen(false);
      loadUsers();
    } catch (error) {
      if (isAxiosError(error)) {
        showError(error, {
          action: editingUser ? 'update' : 'create',
          entity: 'пользователя',
        });
      }
    }
  };

  const handleImport = async () => {
    const file = importFile?.originFileObj;
    if (!file) {
      showError(new Error('Выберите CSV-файл'));
      return;
    }
    setImporting(true);
    setImportResult(null);
    try {
      const response = await usersApi.importCsv(file);
      setImportResult(response.data);
      if (response.data.created > 0) {
        showSuccess(`Импортировано пользователей: ${response.data.created}`);
        loadUsers();
      }
    } catch (error) {
      showError(error, { action: 'create', entity: 'пользователей' });
    } finally {
      setImporting(false);
    }
  };

  const downloadTemplate = () => {
    const blob = new Blob([CSV_TEMPLATE], { type: 'text/csv;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'users_import_template.csv';
    a.click();
    URL.revokeObjectURL(url);
  };

  const userColumns: ColumnsType<User> = [
    {
      title: 'Имя',
      dataIndex: 'nickname',
      key: 'nickname',
      sorter: (a, b) => a.nickname.localeCompare(b.nickname),
    },
    { title: 'Email', dataIndex: 'email', key: 'email' },
    {
      title: 'Роль',
      dataIndex: 'role',
      key: 'role',
      width: 160,
      render: (role: UserRole) => (
        <Tag color={ROLE_COLORS[role]}>{ROLE_LABELS[role]}</Tag>
      ),
    },
    {
      title: 'Регистрация',
      dataIndex: 'registration_date',
      key: 'registration_date',
      width: 170,
      render: (date: string) => formatDateTime(date),
    },
    {
      title: 'Изменён',
      dataIndex: 'edited_at',
      key: 'edited_at',
      width: 170,
      render: (date?: string | null) => (date ? formatDateTime(date) : '—'),
    },
    {
      title: 'Действия',
      key: 'actions',
      width: 120,
      render: (_, record) => {
        const isSelf = currentUser?.id === record.id;
        return (
          <Space>
            <Tooltip title="Редактировать">
              <Button
                type="link"
                icon={<EditOutlined />}
                onClick={() => handleEdit(record)}
                size="small"
              />
            </Tooltip>
            <Tooltip title={isSelf ? 'Нельзя удалить себя' : 'Удалить'}>
              <Button
                type="link"
                danger
                icon={<DeleteOutlined />}
                onClick={() => handleDelete(record)}
                size="small"
                disabled={isSelf}
              />
            </Tooltip>
          </Space>
        );
      },
    },
  ];

  const sessionColumns: ColumnsType<UserSession> = [
    { title: 'Пользователь', dataIndex: 'nickname', key: 'nickname' },
    { title: 'Email', dataIndex: 'email', key: 'email' },
    {
      title: 'Роль',
      dataIndex: 'role',
      key: 'role',
      width: 150,
      render: (role: UserRole) => (
        <Tag color={ROLE_COLORS[role]}>{ROLE_LABELS[role]}</Tag>
      ),
    },
    {
      title: 'Последняя активность',
      dataIndex: 'last_seen_at',
      key: 'last_seen_at',
      width: 180,
      render: (v?: string | null) => (v ? formatDateTime(v) : '—'),
    },
    {
      title: 'Сессия до',
      dataIndex: 'expires_at',
      key: 'expires_at',
      width: 180,
      render: (v: string) => formatDateTime(v),
    },
    {
      title: 'Действия',
      key: 'actions',
      width: 100,
      render: (_, record) => (
        <Tooltip title="Отозвать сессию">
          <Button
            type="link"
            danger
            icon={<DisconnectOutlined />}
            onClick={() => handleRevokeSession(record)}
            size="small"
          />
        </Tooltip>
      ),
    },
  ];

  const auditColumns: ColumnsType<AdminAuditLog> = [
    {
      title: 'Время',
      dataIndex: 'created_at',
      key: 'created_at',
      width: 170,
      render: (v: string) => formatDateTime(v),
    },
    {
      title: 'Администратор',
      dataIndex: 'admin_nickname',
      key: 'admin_nickname',
      width: 160,
    },
    {
      title: 'Действие',
      dataIndex: 'action',
      key: 'action',
      width: 200,
      render: (action: string) => ACTION_LABELS[action] ?? action,
    },
    {
      title: 'Объект',
      key: 'target',
      render: (_, r) => r.target_email ?? (r.target_user_id ? `#${r.target_user_id}` : '—'),
    },
    {
      title: 'Подробности',
      dataIndex: 'details',
      key: 'details',
      ellipsis: true,
      render: (v?: string | null) => v || '—',
    },
  ];

  const tabItems = [
    {
      key: 'users',
      label: (
        <span>
          <UserOutlined /> Пользователи
        </span>
      ),
      children: (
        <>
          <div className="flex justify-between items-center mb-4">
            <Search
              placeholder="Поиск по имени или email"
              allowClear
              enterButton={<SearchOutlined />}
              onSearch={loadUsers}
              style={{ width: 400 }}
            />
            <Space>
              <Button icon={<UploadOutlined />} onClick={() => setIsImportOpen(true)}>
                Импорт CSV
              </Button>
              <Button type="primary" icon={<PlusOutlined />} onClick={handleAdd}>
                Добавить
              </Button>
            </Space>
          </div>
          <Table<User>
            columns={userColumns}
            dataSource={users}
            rowKey="id"
            loading={loading}
            pagination={{ pageSize: 20, showTotal: (t) => `Всего: ${t}` }}
          />
        </>
      ),
    },
    {
      key: 'sessions',
      label: (
        <span>
          <DisconnectOutlined /> Сессии
        </span>
      ),
      children: (
        <>
          <Alert
            className="mb-4"
            type="info"
            showIcon
            title="Активные сессии — пользователи с действующим refresh-токеном. Отзыв завершает вход на всех устройствах."
          />
          <Table<UserSession>
            columns={sessionColumns}
            dataSource={sessions}
            rowKey="user_id"
            loading={loading}
            pagination={{ pageSize: 20, showTotal: (t) => `Активных: ${t}` }}
          />
        </>
      ),
    },
    {
      key: 'audit',
      label: (
        <span>
          <HistoryOutlined /> Журнал
        </span>
      ),
      children: (
        <>
          <div className="mb-4">
            <Search
              placeholder="Поиск по администратору, email, деталям"
              allowClear
              enterButton={<SearchOutlined />}
              onSearch={loadAuditLogs}
              style={{ width: 400 }}
            />
          </div>
          <Table<AdminAuditLog>
            columns={auditColumns}
            dataSource={auditLogs}
            rowKey="id"
            loading={loading}
            pagination={{ pageSize: 25, showTotal: (t) => `Записей: ${t}` }}
          />
        </>
      ),
    },
  ];

  return (
    <div>
      <h2 className="text-2xl font-bold mb-6 mt-0">Администрирование</h2>

      <Tabs activeKey={activeTab} onChange={setActiveTab} items={tabItems} />

      <Modal
        title={editingUser ? 'Редактировать пользователя' : 'Новый пользователь'}
        open={isModalOpen}
        onOk={handleSubmit}
        onCancel={() => setIsModalOpen(false)}
        okText="Сохранить"
        cancelText="Отмена"
        destroyOnHidden
      >
        <Form form={form} layout="vertical">
          <Form.Item
            name="nickname"
            label="Имя"
            rules={[{ required: true, message: 'Введите имя' }]}
          >
            <Input placeholder="Иван Иванов" />
          </Form.Item>
          <Form.Item
            name="email"
            label="Email"
            rules={[
              { required: true, message: 'Введите email' },
              { type: 'email', message: 'Некорректный email' },
            ]}
          >
            <Input placeholder="user@example.com" />
          </Form.Item>
          <Form.Item
            name="role"
            label="Роль"
            rules={[{ required: true, message: 'Выберите роль' }]}
          >
            <Select
              options={(['admin', 'accountant', 'observer'] as UserRole[]).map((r) => ({
                value: r,
                label: ROLE_LABELS[r],
                disabled:
                  currentUser?.id === editingUser?.id &&
                  r !== 'admin' &&
                  editingUser?.role === 'admin',
              }))}
            />
          </Form.Item>
          <Form.Item
            name="password"
            label={editingUser ? 'Новый пароль' : 'Пароль'}
            rules={
              editingUser
                ? [{ min: 5, message: 'Минимум 5 символов' }]
                : [
                    { required: true, message: 'Введите пароль' },
                    { min: 5, message: 'Минимум 5 символов' },
                  ]
            }
            extra={editingUser ? 'Оставьте пустым, чтобы не менять пароль' : undefined}
          >
            <Input.Password placeholder="••••••••" />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        title="Импорт пользователей из CSV"
        open={isImportOpen}
        onOk={handleImport}
        onCancel={() => {
          setIsImportOpen(false);
          setImportFile(null);
          setImportResult(null);
        }}
        okText="Импортировать"
        cancelText="Отмена"
        confirmLoading={importing}
        destroyOnHidden
      >
        <p className="text-gray-600 mb-3">
          Формат: <code>nickname,email,password,role</code> (разделитель — запятая или точка с
          запятой). Первая строка может быть заголовком.
        </p>
        <Button type="link" className="p-0 mb-3" onClick={downloadTemplate}>
          Скачать шаблон CSV
        </Button>
        <Upload
          accept=".csv,text/csv"
          maxCount={1}
          beforeUpload={() => false}
          fileList={importFile ? [importFile] : []}
          onChange={({ fileList }) => setImportFile(fileList[0] ?? null)}
        >
          <Button icon={<UploadOutlined />}>Выбрать файл</Button>
        </Upload>
        {importResult && (
          <Alert
            className="mt-4"
            type={importResult.errors.length > 0 ? 'warning' : 'success'}
            title={`Создано: ${importResult.created}, пропущено: ${importResult.skipped}`}
            description={
              importResult.errors.length > 0 ? (
                <ul className="m-0 pl-4">
                  {importResult.errors.map((e) => (
                    <li key={`${e.line}-${e.email}`}>
                      Строка {e.line}: {e.email} — {e.message}
                    </li>
                  ))}
                </ul>
              ) : undefined
            }
          />
        )}
      </Modal>
    </div>
  );
}
