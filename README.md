# Accountent

Веб-приложение бухгалтерского учёта: план счетов, проводки (двойная запись), контрагенты, ОСВ, дашборд с KPI и графиками, управление пользователями (админ).

**Стек:** ASP.NET Core 8, PostgreSQL, EF Core, React 18 + Vite 6 + TypeScript, Ant Design 6, JWT (access + refresh).

Часовой пояс в примерах дат: **Asia/Krasnoyarsk (UTC+7)**.

---

## Структура репозитория

```
Accountent/
├── backend/                 # REST API (ASP.NET Core 8)
│   ├── Controllers/         # Auth, ChartOfAccounts, Transactions, Counterparties,
│   │                        # Reports, Dashboard, Users, Saldo (legacy redirect)
│   ├── Services/            # Бизнес-логика (Implementations + Common)
│   ├── Abstractions/        # Интерфейсы сервисов и общих компонентов
│   ├── Data/                # DbContext, миграции, seed (план счетов, демо-данные)
│   ├── Models/              # Сущности, DTO, ServiceResult
│   └── Migrations/          # EF Core (PostgreSQL)
├── backend.Tests/           # xUnit: unit + integration (SQLite in-memory)
│   ├── Unit/                # DoubleEntry, Saldo, ServiceResult
│   ├── Integration/         # Auth, роли, ОСВ, проводки, redirect /api/saldo
│   └── Infrastructure/      # WebApplicationFactory, тестовый seeder
├── frontend/                # SPA (React + Vite)
│   └── src/
│       ├── api/             # Axios-клиент и endpoints
│       ├── pages/           # dashboard, transactions, reports, admin, auth
│       ├── components/      # ImportTransactionsModal и др.
│       └── utils/           # форматирование, Excel (ОСВ, импорт проводок)
├── docs/                    # Вспомогательные материалы (скрипты)
├── docker-compose.yml       # postgres + api + frontend (nginx)
├── .env.example             # Переменные для backend и Docker
└── Accountent.slnx          # Решение Visual Studio (backend + frontend)
```

---

## Возможности

| Модуль | Описание |
|--------|----------|
| **Авторизация** | Регистрация, вход, refresh/logout, JWT Bearer |
| **План счетов** | Иерархия счетов, CRUD (admin, accountant) |
| **Проводки** | Журнал, простые и сложные проводки, импорт из Excel |
| **Контрагенты** | Справочник, CRUD |
| **ОСВ** | Оборотно-сальдовая ведомость за период, экспорт в Excel |
| **Дашборд** | KPI и графики за выбранный период |
| **Админ** | Пользователи, роли, аудит действий, активные сессии, импорт пользователей из CSV |

---

## Требования

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/) (для фронтенда)
- PostgreSQL 16 (локально или через Docker)
- Опционально: Docker Compose

---

## Быстрый старт

### 1. Переменные окружения

В корне проекта:

```powershell
copy .env.example .env
```

Заполните как минимум `POSTGRES_PASSWORD` и `Jwt__Secret` (не короче 32 символов).

Backend при запуске читает `.env` из текущей папки, `backend/` или корня репозитория (см. `Program.cs`).

### 2. База данных

**Docker:**

```powershell
docker compose up -d postgres
```

**Или** установленный PostgreSQL с параметрами из `.env` (`POSTGRES_HOST`, `POSTGRES_PORT`, `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD`).

### 3. Backend

```powershell
cd backend
dotnet run
```

| Параметр | Значение по умолчанию |
|----------|------------------------|
| API | `http://localhost:5136` |
| Swagger (Development) | `http://localhost:5136/swagger` |
| Health | `GET /health` |

При первом запуске выполняются миграции EF Core и загрузка демо-данных (`DatabaseSeed`).

### 4. Frontend

```powershell
cd frontend
npm install
copy .env.example .env
npm run dev
```

| Параметр | Значение по умолчанию |
|----------|------------------------|
| SPA | `http://localhost:5173` |
| Прокси API | `/api` → `VITE_API_URL` или `http://localhost:5136` (`vite.config.ts`) |

Для dev можно оставить `VITE_API_URL` пустым — запросы идут через прокси Vite без CORS.

Сборка production: `npm run build`. Проверка типов: `npm run typecheck`. Линт: `npm run lint`.

---

## Демо-пользователи

Пароль для всех учётных записей из seed: **`Password1!`**

| Email | Роль |
|-------|------|
| `admin@accountent.local` | admin |
| `accountant@accountent.local` | accountant |
| `accountant2@accountent.local` | accountant |
| `observer@accountent.local` | observer |
| `observer2@accountent.local` | observer |

---

## Роли и доступ

| Роль | Описание |
|------|----------|
| **admin** | Полный доступ, `/admin/users`, аудит, сессии, импорт пользователей |
| **accountant** | Проводки, план счетов, контрагенты, отчёты, дашборд |
| **observer** | Только просмотр (отчёты, дашборд, справочники) |

Регистрация через API по умолчанию создаёт **observer**; роли `admin` и `accountant` назначает только администратор.

### Маршруты SPA

| Путь | Описание | Роли (запись) |
|------|----------|----------------|
| `/login`, `/register` | Вход / регистрация | гость |
| `/dashboard` | Дашборд | все авторизованные |
| `/chart-of-accounts` | План счетов | просмотр — все; CRUD — admin, accountant |
| `/transactions`, `/transactions/:id` | Журнал, просмотр | все |
| `/transactions/new`, `/transactions/:id/edit` | Создание / правка | admin, accountant |
| `/counterparties` | Контрагенты | просмотр — все; CRUD — admin, accountant |
| `/reports/osv` | ОСВ | все |
| `/admin/users` | Управление пользователями | admin |

---

## API (основные endpoints)

| Метод | Путь | Назначение |
|-------|------|------------|
| POST | `/api/auth/register` | Регистрация |
| POST | `/api/auth/login` | Вход |
| POST | `/api/auth/refresh` | Обновление токена |
| POST | `/api/auth/logout` | Выход (JWT) |
| GET/POST/PUT/DELETE | `/api/chart_of_accounts` | План счетов |
| GET/POST/PUT/DELETE | `/api/transactions` | Проводки |
| POST | `/api/transactions/import` | Импорт проводок |
| GET/POST/PUT/DELETE | `/api/counterparties` | Контрагенты |
| GET | `/api/reports/OSV` | ОСВ (параметры периода в query) |
| GET | `/api/dashboard`, `/api/dashboard/kpi` | Дашборд |
| GET/POST/PUT/DELETE | `/api/users` | Пользователи (admin) |
| GET | `/api/users/audit-logs` | Журнал аудита (admin) |
| GET | `/api/users/sessions` | Активные сессии (admin) |
| POST | `/api/users/import` | Импорт пользователей CSV (admin) |
| GET | `/api/saldo` | Устаревший redirect → `/api/reports/OSV` |
| GET | `/health` | Проверка работоспособности |

Подробнее по контрактам — Swagger в режиме Development.

---

## Переменные окружения

### Backend / Docker (корень `.env`)

| Переменная | Назначение |
|------------|------------|
| `POSTGRES_HOST`, `POSTGRES_PORT`, `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD` | Подключение к PostgreSQL |
| `Jwt__Secret` | Секрет подписи JWT (≥ 32 символа) |
| `Jwt__Issuer`, `Jwt__Audience` | Issuer / Audience токена |
| `Jwt__AccessTokenMinutes` | Время жизни access-токена (мин.) |
| `Jwt__RefreshTokenDays` | Время жизни refresh-токена (дн.) |
| `Cors__Origins` | Разрешённые origin для SPA (`;` — разделитель) |
| `ASPNETCORE_URLS` | URL прослушивания API |
| `API_PORT`, `FRONTEND_PORT`, `CORS_ORIGINS` | Порты и CORS для Docker Compose |
| `VITE_APP_TITLE`, `VITE_TIMEZONE` | Сборка frontend в Docker |

Строка `ConnectionStrings:DefaultConnection` собирается из `POSTGRES_*` автоматически (см. `ConfigurationExtensions`).

### Frontend (`frontend/.env`)

| Переменная | Назначение |
|------------|------------|
| `VITE_API_URL` | URL API; пусто — прокси Vite `/api` |
| `VITE_APP_TITLE` | Заголовок вкладки |
| `VITE_TIMEZONE` | Часовой пояс отображения дат |

Полные примеры — в [.env.example](.env.example) и [frontend/.env.example](frontend/.env.example).

---

## Тесты

Из корня репозитория:

```powershell
dotnet test backend.Tests\backend.Tests.csproj
```

| Каталог | Содержание |
|---------|------------|
| `Unit/` | Валидация двойной записи, расчёт сальдо, маппинг ошибок |
| `Integration/` | Auth, refresh, роли, ОСВ, создание проводок, redirect saldo |
| `Infrastructure/` | `AccountingTestWebApplicationFactory`, SQLite in-memory |

Интеграционные тесты поднимают API с `Testing:UseSqlite=true` и `Testing:SkipDatabaseBootstrap=true`; БД — SQLite in-memory с общим кэшем соединения.

---

## Docker (полный стек)

Подготовка (один раз):

```powershell
copy .env.example .env
# Заполните POSTGRES_PASSWORD и Jwt__Secret (≥ 32 символа)
```

Запуск PostgreSQL, API и SPA (nginx + прокси `/api` → backend):

```powershell
docker compose up -d --build
```

| Сервис | URL по умолчанию |
|--------|------------------|
| Frontend (SPA) | http://localhost:8081 |
| API | http://localhost:8080 |
| PostgreSQL | localhost:5432 |

Демо-вход: `accountant@accountent.local` / `Password1!`

Только БД и API (без фронта в контейнере):

```powershell
docker compose up -d postgres api
```

Остановка:

```powershell
docker compose down
```

Логи:

```powershell
docker compose logs -f api
docker compose logs -f frontend
```

---

## Архитектура backend

- **Controllers** — HTTP, авторизация по ролям, маппинг `ServiceResult` в коды ответа.
- **Services/Implementations** — сценарии (Auth, Transactions, Reports, Users, Dashboard и др.).
- **Services/Common** — двойная запись, сальдо, refresh-токены, маппинг сущностей.
- **Abstractions** — интерфейсы для DI и тестируемости.
- **Data** — `ApplicationDbContext`, фабрика контекста, seed плана счетов РФ и демо-операций.

Миграции EF Core: `dotnet ef migrations add <Name> --project backend` (из корня, при установленном `dotnet-ef`).

---

## Документация

В каталоге [docs/](docs/) — вспомогательные скрипты (например, генерация пояснительной записки). Отдельный UI/README для дипломных материалов при необходимости добавляется в `docs/`.

Краткое описание фронтенда: [frontend/README.md](frontend/README.md).

---

## Безопасность

- Пароли пользователей хранятся как **BCrypt-хеш**.
- Refresh-токены в БД тоже хранятся как **BCrypt-хеш**; клиенту отдаётся только сырой токен при login/register/refresh.
- Access-токен проверяется middleware JWT Bearer.
- Административные действия пишутся в **AdminAuditLog**.
