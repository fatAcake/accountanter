import { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { UserRole } from '../types';
import { authApi } from '../api/auth';
import { App } from 'antd';
import { resolveApiError } from '../api/errors';

interface AuthContextType {
  isAuthenticated: boolean;
  user: {
    id: number;
    nickname: string;
    role: UserRole;
  } | null;
  login: (email: string, password: string) => Promise<void>;
  register: (nickname: string, email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  canEdit: () => boolean;
  isAdmin: () => boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const { message } = App.useApp();
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [user, setUser] = useState<{ id: number; nickname: string; role: UserRole } | null>(
    null,
  );

  useEffect(() => {
    const token = sessionStorage.getItem('access_token');
    const nickname = sessionStorage.getItem('nickname');
    const role = sessionStorage.getItem('role') as UserRole;
    const idRaw = sessionStorage.getItem('user_id');

    if (token && nickname && role && idRaw) {
      setIsAuthenticated(true);
      setUser({ id: Number(idRaw), nickname, role });
    }
  }, []);

  const login = async (email: string, password: string) => {
    try {
      const response = await authApi.login(email, password);

      const { jwt_token, refresh_token, nickname, role, id } = response.data;

      sessionStorage.setItem('access_token', jwt_token);
      sessionStorage.setItem('refresh_token', refresh_token);
      sessionStorage.setItem('nickname', nickname);
      sessionStorage.setItem('role', role);
      sessionStorage.setItem('user_id', String(id));

      setIsAuthenticated(true);
      setUser({ id, nickname, role });

      message.success('Вход выполнен успешно');
    } catch (error) {
      message.error(resolveApiError(error, { action: 'auth', fallback: 'Не удалось войти в систему' }));
      throw error;
    }
  };

  const register = async (nickname: string, email: string, password: string) => {
    try {
      await authApi.register(nickname, email, password);

      message.success('Регистрация успешна. Теперь войдите в систему');
    } catch (error) {
      message.error(
        resolveApiError(error, { action: 'auth', fallback: 'Не удалось зарегистрироваться' }),
      );
      throw error;
    }
  };

  const logout = async () => {
    try {
      await authApi.logout();
    } catch {
      // выход локально даже при ошибке сети
    } finally {
      sessionStorage.clear();
      setIsAuthenticated(false);
      setUser(null);
      message.info('Вы вышли из системы');
    }
  };

  const canEdit = (): boolean => {
    return user?.role === 'admin' || user?.role === 'accountant';
  };

  const isAdmin = (): boolean => user?.role === 'admin';

  return (
    <AuthContext.Provider
      value={{ isAuthenticated, user, login, register, logout, canEdit, isAdmin }}
    >
      {children}
    </AuthContext.Provider>
  );
};

// eslint-disable-next-line react-refresh/only-export-components -- hook + provider в одном модуле
export const useAuth = (): AuthContextType => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }
  return context;
};
