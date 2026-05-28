import { useState } from 'react';
import { Alert, Button, Modal, Upload } from 'antd';
import { UploadOutlined } from '@ant-design/icons';
import type { UploadFile } from 'antd/es/upload';
import { transactionsApi } from '../api';
import { useApiError } from '../hooks/useApiError';
import type { ImportTransactionsResult } from '../types';
import { downloadTransactionsTemplate, parseTransactionsExcel } from '../utils/parseTransactionsExcel';

interface Props {
  open: boolean;
  onClose: () => void;
  onSuccess?: () => void;
}

export default function ImportTransactionsModal({ open, onClose, onSuccess }: Props) {
  const { showError, showSuccess, showInfo } = useApiError();
  const [importFile, setImportFile] = useState<UploadFile | null>(null);
  const [importing, setImporting] = useState(false);
  const [importResult, setImportResult] = useState<ImportTransactionsResult | null>(null);

  const handleClose = () => {
    onClose();
    setImportFile(null);
    setImportResult(null);
  };

  const handleImport = async () => {
    const file = importFile?.originFileObj;
    if (!file) {
      showInfo('Выберите файл Excel (.xlsx, .xls)');
      return;
    }

    setImporting(true);
    setImportResult(null);
    try {
      const rows = await parseTransactionsExcel(file);
      if (rows.length === 0) {
        showInfo('Файл не содержит строк для импорта');
        return;
      }

      const response = await transactionsApi.import(rows);
      setImportResult(response.data);
      if (response.data.created > 0) {
        showSuccess(`Импортировано проводок: ${response.data.created}`);
        onSuccess?.();
      }
      if (response.data.created === 0 && response.data.errors.length === 0) {
        showInfo('Нет данных для импорта');
      }
    } catch (error) {
      showError(error, { action: 'import', entity: 'проводок' });
    } finally {
      setImporting(false);
    }
  };

  return (
    <Modal
      title="Импорт проводок из Excel"
      open={open}
      onOk={() => void handleImport()}
      onCancel={handleClose}
      okText="Импортировать"
      cancelText="Отмена"
      confirmLoading={importing}
      destroyOnHidden
    >
      <p className="text-gray-600 mb-3">
        Колонки: <strong>Дата</strong>, <strong>Дебет</strong>, <strong>Кредит</strong>,{' '}
        <strong>Сумма</strong>, <strong>Основание</strong>, ИНН контрагента (опционально).
        Первая строка — заголовки.
      </p>
      <Button type="link" className="p-0 mb-3" onClick={downloadTransactionsTemplate}>
        Скачать шаблон Excel
      </Button>
      <Upload
        accept=".xlsx,.xls,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet,application/vnd.ms-excel"
        maxCount={1}
        beforeUpload={() => false}
        fileList={importFile ? [importFile] : []}
        onChange={({ fileList }) => {
          setImportFile(fileList[0] ?? null);
          setImportResult(null);
        }}
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
              <ul className="m-0 pl-4 max-h-40 overflow-y-auto">
                {importResult.errors.map((e) => (
                  <li key={`${e.line}-${e.message}`}>
                    {e.line > 0 ? `Строка ${e.line}: ` : ''}
                    {e.message}
                  </li>
                ))}
              </ul>
            ) : undefined
          }
        />
      )}
    </Modal>
  );
}
