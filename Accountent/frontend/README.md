# Accountent — Frontend

SPA для бухгалтерского учёта малого предприятия / ИП. Общий запуск и Docker — в [корневом README](../README.md).

## Функционал

- **Авторизация**: вход, регистрация, refresh токенов (Axios interceptors)
- **Дашборд**: KPI и графики за период (`@ant-design/plots`)
- **План счетов**: иерархия, CRUD
- **Журнал проводок**: фильтрация, создание/редактирование, импорт из Excel
- **Контрагенты**: справочник
- **ОСВ**: отчёт за период, экспорт в Excel (`xlsx`)
- **Админ** (`/admin/users`): пользователи, аудит, сессии (только роль admin)

## Стек

- React 18, TypeScript ~5.8
- Vite 6, Tailwind CSS 4
- Ant Design 6, React Router 7
- Axios, dayjs

## Запуск

```powershell
npm install
copy .env.example .env
npm run dev
```

Приложение: `http://localhost:5173`. Backend по умолчанию: `http://localhost:5136`.

В `.env` можно оставить `VITE_API_URL` пустым — в dev запросы к `/api` проксируются через Vite.

## Скрипты

| Команда | Назначение |
|---------|------------|
| `npm run dev` | Dev-сервер |
| `npm run build` | Production-сборка |
| `npm run typecheck` | Проверка TypeScript |
| `npm run lint` | ESLint (`src/`) |

## Структура `src/`

```
src/
├── api/              # client.ts, auth, accounts, transactions, …
├── app/App.tsx       # маршруты и Ant Design ConfigProvider (ru_RU)
├── components/       # ImportTransactionsModal
├── config/env.ts     # VITE_* переменные
├── hooks/            # useAuth, useApiError
├── layouts/          # MainLayout
├── pages/
│   ├── auth/         # Login, Register
│   ├── dashboard/    # DashboardCharts, periodUtils
│   ├── chart-of-accounts/
│   ├── transactions/
│   ├── counterparties/
│   ├── reports/      # OsvReport
│   └── admin/        # AdminUsers
├── routes/           # PrivateRoute, AdminRoute, PublicRoute
├── types/
└── utils/            # format, exportOsvExcel, parseTransactionsExcel
```

## Маршруты

| Путь | Доступ |
|------|--------|
| `/login`, `/register` | Гость |
| `/dashboard` | Авторизованный |
| `/chart-of-accounts` | Авторизованный |
| `/transactions`, `/transactions/:id` | Авторизованный |
| `/transactions/new`, `/transactions/:id/edit` | admin, accountant |
| `/counterparties` | Авторизованный |
| `/reports/osv` | Авторизованный |
| `/admin/users` | admin |

## Локализация

- Интерфейс: русский (`antd/locale/ru_RU`)
- Даты: `DD.MM.YYYY`, часовой пояс `VITE_TIMEZONE` (по умолчанию Asia/Krasnoyarsk)
- Суммы: ₽, 2 знака после запятой
