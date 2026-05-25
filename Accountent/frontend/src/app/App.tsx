import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { App as AntdApp, ConfigProvider } from 'antd';
import ruRU from 'antd/locale/ru_RU';
import { AuthProvider } from '../hooks/useAuth';
import { AdminRoute, PrivateRoute, PublicRoute } from '../routes';
import MainLayout from '../layouts/MainLayout';
import Login from '../pages/auth/Login';
import Register from '../pages/auth/Register';
import Dashboard from '../pages/Dashboard';
import ChartOfAccounts from '../pages/chart-of-accounts/ChartOfAccounts';
import Transactions from '../pages/transactions/Transactions';
import TransactionForm from '../pages/transactions/TransactionForm';
import TransactionView from '../pages/transactions/TransactionView';
import Counterparties from '../pages/counterparties/Counterparties';
import OsvReport from '../pages/reports/OsvReport';
import AdminUsers from '../pages/admin/AdminUsers';

export default function App() {
  return (
    <ConfigProvider
      locale={ruRU}
      theme={{
        token: {
          colorPrimary: '#1677ff',
          borderRadius: 6,
          fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif',
        },
      }}
    >
      <AntdApp>
        <AuthProvider>
          <BrowserRouter>
            <Routes>
              <Route
                path="/login"
                element={
                  <PublicRoute>
                    <Login />
                  </PublicRoute>
                }
              />
              <Route
                path="/register"
                element={
                  <PublicRoute>
                    <Register />
                  </PublicRoute>
                }
              />
              <Route
                path="/"
                element={
                  <PrivateRoute>
                    <MainLayout />
                  </PrivateRoute>
                }
              >
                <Route index element={<Navigate to="/dashboard" replace />} />
                <Route path="dashboard" element={<Dashboard />} />
                <Route path="chart-of-accounts" element={<ChartOfAccounts />} />
                <Route path="transactions" element={<Transactions />} />
                <Route path="transactions/new" element={<TransactionForm />} />
                <Route path="transactions/:id" element={<TransactionView />} />
                <Route path="transactions/:id/edit" element={<TransactionForm />} />
                <Route path="counterparties" element={<Counterparties />} />
                <Route path="reports/osv" element={<OsvReport />} />
                <Route
                  path="admin/users"
                  element={
                    <AdminRoute>
                      <AdminUsers />
                    </AdminRoute>
                  }
                />
              </Route>
              <Route path="*" element={<Navigate to="/dashboard" replace />} />
            </Routes>
          </BrowserRouter>
        </AuthProvider>
      </AntdApp>
    </ConfigProvider>
  );
}
