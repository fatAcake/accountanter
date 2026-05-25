import { useCallback } from 'react';
import { App } from 'antd';
import { resolveApiError, type ErrorContext } from '../api/errors';

export function useApiError() {
  const { message } = App.useApp();

  const showError = useCallback(
    (error: unknown, context?: ErrorContext | string) => {
      message.error(resolveApiError(error, context));
    },
    [message],
  );

  const showSuccess = useCallback(
    (text: string) => {
      message.success(text);
    },
    [message],
  );

  const showInfo = useCallback(
    (text: string) => {
      message.info(text);
    },
    [message],
  );

  return {
    showError,
    showSuccess,
    showInfo,
    resolveError: resolveApiError,
  };
}
