# Accountent - Бухгалтерская система

Одностраничное веб-приложение (SPA) для работы бухгалтерии малого предприятия / ИП.

## Функционал

- **Авторизация**: вход, регистрация, обновление токенов
- **План счетов**: иерархическое отображение, CRUD операции
- **Журнал проводок**: создание простых и сложных проводок, фильтрация
- **Контрагенты**: управление справочником контрагентов
- **Отчёт ОСВ**: оборотно-сальдовая ведомость за период

## Технологический стек

- **React 18+** с TypeScript
- **Vite** для сборки
- **Ant Design 5** для UI компонентов
- **React Router 6** для маршрутизации
- **Axios** для HTTP запросов
- **dayjs** для работы с датами
- **Tailwind CSS** для стилизации

## Установка и запуск

1. Установите зависимости:
```bash
pnpm install
```

2. Скопируйте `.env.example` в `.env` и настройте переменные окружения:
```bash
cp .env.example .env
```

3. Убедитесь, что backend API запущен на `http://localhost:5136` (или измените `VITE_API_URL` в `.env`)

4. Запустите dev-сервер (уже запущен в Figma Make):
```bash
pnpm dev
```

## Структура проекта

```
src/
├── api/              # axios клиент, endpoints
├── app/              # главный App.tsx
├── components/       # переиспользуемые компоненты
├── layouts/          # MainLayout
├── pages/            # страницы по модулям
│   ├── auth/         # Login, Register
│   ├── chart-of-accounts/
│   ├── transactions/
│   ├── counterparties/
│   └── reports/
├── hooks/            # useAuth
├── types/            # TypeScript типы
├── utils/            # форматирование дат/сумм
└── routes/           # защищённые маршруты
```

## Роли пользователей

- **admin**: полный доступ
- **accountant**: создание/редактирование операций и справочников
- **observer**: только просмотр данных

## Особенности

- Русский интерфейс
- Формат дат: DD.MM.YYYY
- Часовой пояс: Asia/Krasnoyarsk (UTC+7)
- Формат сумм: российский рубль (₽) с 2 знаками после запятой

## Маршруты

| Путь | Описание | Доступ |
|------|----------|--------|
| `/login` | Вход | Гость |
| `/register` | Регистрация | Гость |
| `/chart-of-accounts` | План счетов | Авторизованный |
| `/transactions` | Журнал проводок | Авторизованный |
| `/transactions/new` | Новая проводка | admin, accountant |
| `/transactions/:id` | Просмотр | Авторизованный |
| `/transactions/:id/edit` | Редактирование | admin, accountant |
| `/counterparties` | Контрагенты | Авторизованный |
| `/reports/osv` | ОСВ отчёт | Авторизованный |

## API Backend

Backend должен предоставлять следующие endpoints:

- `POST /api/auth/register` - регистрация
- `POST /api/auth/login` - вход
- `POST /api/auth/refresh` - обновление токена
- `POST /api/auth/logout` - выход
- `GET/POST/PUT/DELETE /api/chart_of_accounts` - план счетов
- `GET/POST/PUT/DELETE /api/transactions` - проводки
- `GET/POST/PUT/DELETE /api/counterparties` - контрагенты
- `GET /api/reports/OSV` - отчёт ОСВ

## Разработка

Проект использует:
- TypeScript для типобезопасности
- Ant Design для единого UI/UX
- Axios interceptors для автоматического обновления токенов
- React Context для управления состоянием авторизации

## Версия

1.0 - 19.05.2026
