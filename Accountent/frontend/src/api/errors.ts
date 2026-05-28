import { AxiosError, isAxiosError } from 'axios';

interface ApiErrorBody {
  message?: string;
  title?: string;
  errors?: Record<string, string[]>;
}

export type ErrorAction =
  | 'load'
  | 'create'
  | 'update'
  | 'delete'
  | 'save'
  | 'auth'
  | 'report'
  | 'export'
  | 'import';

export interface ErrorContext {
  action: ErrorAction;
  entity?: string;
  fallback?: string;
}

const STATUS_MESSAGES: Record<number, string> = {
  400: 'Проверьте правильность введённых данных.',
  401: 'Требуется авторизация. Войдите в систему.',
  403: 'Недостаточно прав для выполнения операции.',
  404: 'Запись не найдена. Возможно, она была удалена.',
  409: 'Операция невозможна из‑за конфликта данных.',
  422: 'Данные не прошли проверку.',
  500: 'Внутренняя ошибка сервера. Попробуйте позже.',
  502: 'Сервер временно недоступен.',
  503: 'База данных недоступна. Запустите PostgreSQL и перезапустите backend.',
};

const TECHNICAL_PATTERNS = [
  /^Request failed/i,
  /axios/i,
  /ECONNREFUSED/i,
  /ENOTFOUND/i,
  /Network Error/i,
  /timeout exceeded/i,
  /Unexpected token/i,
  /SyntaxError/i,
  /\bat\s+[\w.]+\(/,
  /Exception/i,
  /StackTrace/i,
];

function isUserFriendlyMessage(text: string): boolean {
  const trimmed = text.trim();
  if (!trimmed || trimmed.length > 400) return false;
  return !TECHNICAL_PATTERNS.some((pattern) => pattern.test(trimmed));
}

function extractServerMessage(data: unknown): string | null {
  if (!data || typeof data !== 'object') return null;

  const body = data as ApiErrorBody;

  if (typeof body.message === 'string' && isUserFriendlyMessage(body.message)) {
    return body.message.trim();
  }

  if (body.errors && typeof body.errors === 'object') {
    const parts = Object.entries(body.errors).flatMap(([field, messages]) => {
      if (!Array.isArray(messages)) return [];
      return messages
        .filter((m) => typeof m === 'string' && isUserFriendlyMessage(m))
        .map((m) => (field ? `${field}: ${m}` : m));
    });
    if (parts.length > 0) {
      return parts.join(' ');
    }
  }

  return null;
}

function buildContextFallback(context?: ErrorContext | string): string {
  if (typeof context === 'string') return context;
  if (!context) return 'Произошла ошибка. Попробуйте ещё раз.';

  if (context.fallback) return context.fallback;

  const entity = context.entity ? ` ${context.entity}` : '';

  switch (context.action) {
    case 'load':
      return `Не удалось загрузить${entity}.`;
    case 'create':
      return `Не удалось создать${entity}.`;
    case 'update':
      return `Не удалось обновить${entity}.`;
    case 'delete':
      return `Не удалось удалить${entity}.`;
    case 'save':
      return `Не удалось сохранить${entity}.`;
    case 'auth':
      return 'Ошибка авторизации.';
    case 'report':
      return 'Не удалось сформировать отчёт.';
    case 'export':
      return `Не удалось экспортировать${entity}.`;
    case 'import':
      return `Не удалось импортировать${entity}.`;
    default:
      return 'Произошла ошибка. Попробуйте ещё раз.';
  }
}

function resolveNetworkMessage(error: AxiosError): string {
  if (error.code === 'ECONNABORTED') {
    return 'Превышено время ожидания ответа сервера. Попробуйте ещё раз.';
  }
  if (!error.response) {
    return 'Не удалось связаться с сервером. Проверьте, что backend запущен, и повторите попытку.';
  }
  return 'Ошибка сети. Проверьте подключение к интернету.';
}

/** Возвращает понятное пользователю сообщение без технических деталей. */
export function resolveApiError(error: unknown, context?: ErrorContext | string): string {
  const fallback = buildContextFallback(context);

  if (isAxiosError(error)) {
    const serverMessage = extractServerMessage(error.response?.data);
    if (serverMessage) return serverMessage;

    const status = error.response?.status;
    if (status && STATUS_MESSAGES[status]) {
      if (context && typeof context === 'object' && context.action === 'auth' && status === 401) {
        return 'Неверный email или пароль.';
      }
      return STATUS_MESSAGES[status];
    }

    if (!error.response || error.code === 'ECONNABORTED') {
      return resolveNetworkMessage(error);
    }

    return fallback;
  }

  if (error instanceof Error && isUserFriendlyMessage(error.message)) {
    return error.message.trim();
  }

  return fallback;
}

/** @deprecated Используйте resolveApiError */
export const getApiErrorMessage = resolveApiError;
