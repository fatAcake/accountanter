# Accountent

Веб-приложение бухгалтерского учёта: план счетов, проводки (двойная запись), контрагенты, ОСВ, дашборд и управление пользователями.

**Стек:** ASP.NET Core 8, PostgreSQL, React + Vite + TypeScript, JWT.

Часовой пояс в примерах дат: **Asia/Krasnoyarsk (UTC+7)**.

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

Backend при запуске читает `.env` из корня или из `backend/`.

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

По умолчанию API: `http://localhost:5136`  
Swagger (Development): `http://localhost:5136/swagger`

При первом запуске выполняются миграции EF Core и загрузка демо-данных (`DatabaseSeed`).

### 4. Frontend

```powershell
cd frontend
npm install
copy .env.example .env
npm run dev
```

SPA: `http://localhost:5173` (прокси на API настраивается в `vite.config` / `.env`).

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
| **admin** | Полный доступ, управление пользователями, аудит, сессии |
| **accountant** | Проводки, план счетов, контрагенты, отчёты |
| **observer** | Только просмотр (отчёты, дашборд, справочники) |

Регистрация через API по умолчанию создаёт **observer**; роли `admin` и `accountant` назначает только администратор.

---

## Переменные окружения

| Переменная | Назначение |
|------------|------------|
| `POSTGRES_HOST`, `POSTGRES_PORT`, `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD` | Подключение к PostgreSQL |
| `Jwt__Secret` | Секрет подписи JWT (≥ 32 символа) |
| `Jwt__Issuer`, `Jwt__Audience` | Issuer / Audience токена |
| `Jwt__AccessTokenMinutes` | Время жизни access-токена (мин.) |
| `Jwt__RefreshTokenDays` | Время жизни refresh-токена (дн.) |
| `Cors__Origins` | Разрешённые origin для SPA (`;` — разделитель) |
| `ASPNETCORE_URLS` | URL прослушивания API |

Полный пример — в [.env.example](.env.example).

---

## Тесты

```powershell
dotnet test backend.Tests\backend.Tests.csproj
```

Интеграционные тесты используют SQLite in-memory (`Testing:UseSqlite`).

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

## Безопасность

- Пароли пользователей хранятся как **BCrypt-хеш**.
- Refresh-токены в БД тоже хранятся как **BCrypt-хеш**; клиенту отдаётся только сырой токен при login/register/refresh.
- Access-токен проверяется middleware JWT Bearer.
