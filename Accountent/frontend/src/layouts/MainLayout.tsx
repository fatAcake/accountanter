import { Button, Drawer, Grid, Layout, Menu, Avatar, Dropdown } from 'antd';
import {
  HomeOutlined,
  FileTextOutlined,
  SwapOutlined,
  TeamOutlined,
  BarChartOutlined,
  LogoutOutlined,
  UserOutlined,
  SafetyOutlined,
  MenuOutlined,
} from '@ant-design/icons';
import { useAuth } from '../hooks/useAuth';
import { useNavigate, useLocation, Outlet } from 'react-router-dom';
import { useState } from 'react';

const { Header, Sider, Content } = Layout;

export default function MainLayout() {
  const { user, logout, isAdmin } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const screens = Grid.useBreakpoint();
  const isMobile = screens.lg === false;
  const [drawerOpen, setDrawerOpen] = useState(false);

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  const userMenuItems = [
    {
      key: 'profile',
      icon: <UserOutlined />,
      label: user?.nickname || 'Пользователь',
      disabled: true,
    },
    { type: 'divider' as const },
    {
      key: 'logout',
      icon: <LogoutOutlined />,
      label: 'Выход',
      onClick: handleLogout,
    },
  ];

  const menuItems = [
    {
      key: '/dashboard',
      icon: <HomeOutlined />,
      label: 'Главная',
      onClick: () => navigate('/dashboard'),
    },
    {
      key: '/chart-of-accounts',
      icon: <FileTextOutlined />,
      label: 'План счетов',
      onClick: () => navigate('/chart-of-accounts'),
    },
    {
      key: '/transactions',
      icon: <SwapOutlined />,
      label: 'Журнал проводок',
      onClick: () => navigate('/transactions'),
    },
    {
      key: '/counterparties',
      icon: <TeamOutlined />,
      label: 'Контрагенты',
      onClick: () => navigate('/counterparties'),
    },
    {
      key: 'reports',
      icon: <BarChartOutlined />,
      label: 'Отчёты',
      children: [
        {
          key: '/reports/osv',
          label: 'ОСВ',
          onClick: () => navigate('/reports/osv'),
        },
      ],
    },
    ...(isAdmin()
      ? [
          {
            key: '/admin/users',
            icon: <SafetyOutlined />,
            label: 'Пользователи',
            onClick: () => navigate('/admin/users'),
          },
        ]
      : []),
  ];

  return (
    <Layout className="min-h-screen">
      <Header className="flex items-center justify-between px-4 sm:px-6 bg-white border-b border-gray-200">
        <div className="flex items-center gap-3">
          {isMobile && (
            <Button
              type="text"
              icon={<MenuOutlined />}
              aria-label="Открыть меню"
              onClick={() => setDrawerOpen(true)}
            />
          )}
          <FileTextOutlined className="text-2xl text-blue-500" />
          <h1 className="text-xl font-bold m-0">Accountent</h1>
        </div>
        <div className="flex items-center gap-4">
          <span className="text-gray-600">
            {user?.nickname} ({user?.role})
          </span>
          <Dropdown menu={{ items: userMenuItems }} placement="bottomRight">
            <Avatar icon={<UserOutlined />} className="cursor-pointer bg-blue-500" />
          </Dropdown>
        </div>
      </Header>
      <Layout>
        {isMobile ? (
          <Drawer
            open={drawerOpen}
            onClose={() => setDrawerOpen(false)}
            placement="left"
            width={250}
            bodyStyle={{ padding: 0 }}
          >
            <Menu
              mode="inline"
              selectedKeys={[location.pathname]}
              defaultOpenKeys={['reports']}
              items={menuItems}
              className="border-0"
              onClick={() => setDrawerOpen(false)}
            />
          </Drawer>
        ) : (
          <Sider width={250} theme="light" className="border-r border-gray-200">
            <Menu
              mode="inline"
              selectedKeys={[location.pathname]}
              defaultOpenKeys={['reports']}
              items={menuItems}
              className="border-0"
            />
          </Sider>
        )}
        <Layout className="p-4 sm:p-6">
          <Content className="bg-white rounded-lg shadow-sm p-4 sm:p-6">
            <Outlet />
          </Content>
        </Layout>
      </Layout>
    </Layout>
  );
}
