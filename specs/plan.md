# SpendWise: Technical Plan

How we'll build what [spec.md](spec.md) describes, following the [constitution](constitution.md).

## 1. Architecture

```
 Browser
    │
    ▼
 SpendWise.Web  (MVC client, port 5090)
    │  HTTP + JSON, JWT in the Authorization header
    ▼
 SpendWise.Api  (Web API, port 5080, Swagger at /swagger)
    │
    ▼
 SpendWise.Services  (business rules)
    │
    ▼
 SpendWise.Data  (EF Core)  ──▶  spendwise.db (SQLite)

 SpendWise.Contracts (DTOs) is shared by Web, Api and Services.
```

**How a request flows**, for example adding an expense:

1. The user submits the form in the browser.
2. The **Web** app checks the form and calls `POST /api/expenses` through its API client. It adds the user's JWT automatically.
3. The **API** checks the JWT, and the controller calls `ExpenseService.CreateAsync(userId, request)`.
4. The **service** checks the business rules and saves through EF Core.
5. The API returns `201` with the new expense, or an error. The Web app redirects or shows the error.

**Login in the client:** after a successful login, the Web app stores the JWT inside its encrypted login cookie. A small handler adds the token to every API call. If the API answers 401, the user is logged out and sent to the login page.

## 2. Projects

| Project | Type | Depends on | Contains |
|---------|------|------------|----------|
| `SpendWise.Contracts` | class library | — | DTOs, `Roles`, validation limits |
| `SpendWise.Data` | class library | EF Core | entities, `DbContext`, migrations, seeder |
| `SpendWise.Services` | class library | Data, Contracts | services, exceptions, entity → DTO mapping, JWT creation |
| `SpendWise.Api` | Web API | Services, Contracts | controllers, error handler, startup |
| `SpendWise.Web` | MVC | **Contracts only** | API client, controllers, views |
| `SpendWise.Services.Tests` | xUnit | Services, Data | service tests |

Folder layout: `src/` for the five app projects, `tests/` for the test project, `specs/` for these documents, and `README.md` at the root.

**Main NuGet packages:** EF Core SQLite + Design, `Microsoft.Extensions.Identity.Core` (password hashing), JwtBearer, Swashbuckle (Swagger), xUnit, `Microsoft.Extensions.TimeProvider.Testing`.

## 3. Data model

```
User 1───* Expense *───1 Category
User 1───* Budget  *───1 Category
User 1───* Category        (personal categories; UserId = null means global)
```

| Entity | Fields | Rules |
|--------|--------|-------|
| **User** | Id, Username, Email, PasswordHash, Role, IsActive, CreatedAt | Username and email unique, ignoring case (stored in upper-case "normalized" columns with unique indexes) |
| **Category** | Id, Name (40), Color (`#RRGGBB`), UserId?, CreatedAt | `UserId = null` means global. Name uniqueness is checked in the service, because it spans global and personal categories |
| **Expense** | Id, UserId, CategoryId, Amount, Date, Description (200)?, CreatedAt | Amount `decimal(18,2)`. Index on (UserId, Date). A category with expenses can't be deleted |
| **Budget** | Id, UserId, CategoryId, Year, Month, Limit | Unique on (UserId, CategoryId, Year, Month). A category with budgets can't be deleted |

Deleting a user deletes their data. Deleting a category that is still in use is blocked.

*SQLite note:* SQLite can't `SUM` decimals inside the database, so report totals are added up in C# after loading one month of data. That's a small amount of data, so it's fast enough.

### Seed data (added in Group 6)

- **Accounts:** `admin` (Admin), `maria` and `ivan` (Users), `blocked` (a deactivated User). Passwords are listed in the README.
- **Global categories:** Food, Clothes, Going out, Subscriptions, Gifts, Transport, Health, Other.
- **maria:** a "Gym" category, about 55 expenses over 6 months, and budgets for this month and last month, with "Going out" over budget this month.
- **ivan:** a few expenses, to show that users can't see each other's data.

## 4. API

Base URL `http://localhost:5080`. JSON in camelCase. Dates as `yyyy-MM-dd`. Every endpoint except register and login needs `Authorization: Bearer <token>`.

### Errors

All errors use the standard `ProblemDetails` JSON (`title`, `status`, `detail`, plus `errors` for validation). Services throw exceptions, and one global handler turns them into status codes:

| Situation | Status |
|-----------|--------|
| Invalid input (attribute validation) or broken business rule | 400 |
| Not logged in or bad login | 401 |
| Logged in but not allowed (wrong role, or editing a global category as a User) | 403 |
| Doesn't exist, or belongs to another user | 404 |
| Duplicate, or still in use | 409 |
| Unexpected error | 500 (no details leaked) |

### Endpoints

| Method | URL | Who | Body → Response | Errors | Req. |
|--------|-----|-----|-----------------|--------|------|
| **Auth** |||||
| POST | `/api/auth/register` | anyone | `RegisterRequest` → **201** `AuthResponse` | 400, 409 | AUTH-01..03 |
| POST | `/api/auth/login` | anyone | `LoginRequest` → **200** `AuthResponse` | 400, 401, 403 deactivated | AUTH-03, 04 |
| GET | `/api/auth/me` | logged in | → **200** `UserDto` | 401 | AUTH-05 |
| **Categories** |||||
| GET | `/api/categories` | logged in | → **200** `CategoryDto[]` | | CAT-01 |
| POST | `/api/categories` | logged in | `CategoryRequest` → **201** `CategoryDto` (global if the caller is an Admin) | 400, 409 | CAT-02, 03, 06 |
| PUT | `/api/categories/{id}` | logged in | `CategoryRequest` → **200** `CategoryDto` | 400, 403, 404, 409 | CAT-03, 04, 06 |
| DELETE | `/api/categories/{id}` | logged in | → **204** | 403, 404, 409 in use | CAT-04, 05, 06 |
| **Expenses** |||||
| GET | `/api/expenses?year&month&categoryId&page&pageSize` | logged in | → **200** `PagedResult<ExpenseDto>` | 400 | EXP-03, 06 |
| GET | `/api/expenses/{id}` | logged in | → **200** `ExpenseDto` | 404 | EXP-06 |
| POST | `/api/expenses` | logged in | `ExpenseRequest` → **201** `ExpenseDto` | 400 | EXP-01, 02 |
| PUT | `/api/expenses/{id}` | logged in | `ExpenseRequest` → **200** `ExpenseDto` | 400, 404 | EXP-04, 06 |
| DELETE | `/api/expenses/{id}` | logged in | → **204** | 404 | EXP-05, 06 |
| **Budgets** |||||
| GET | `/api/budgets?year&month` | logged in | → **200** `BudgetDto[]` | 400 | BUD-02 |
| PUT | `/api/budgets` | logged in | `SetBudgetRequest` → **200** `BudgetDto` (create or update) | 400 | BUD-01 |
| DELETE | `/api/budgets/{id}` | logged in | → **204** | 404 | BUD-02 |
| POST | `/api/budgets/copy-previous` | logged in | `{ year, month }` → **200** `CopyBudgetsResult` | 400 | BUD-03 |
| **Reports** |||||
| GET | `/api/reports/monthly?year&month` | logged in | → **200** `MonthlySummaryDto` | 400 | REP-01, 02 |
| GET | `/api/reports/trend?year&month&months=6` | logged in | → **200** `MonthlyTotalDto[]` (oldest first) | 400 | REP-03 |
| **Admin** |||||
| GET | `/api/admin/users` | Admin | → **200** `AdminUserDto[]` | 401, 403 | ADM-01, 03 |
| PATCH | `/api/admin/users/{id}/status` | Admin | `{ isActive }` → **204** | 400 own account, 403, 404 | ADM-02, 03 |

If `year` and `month` are left out, they default to the current month.

### DTOs

All DTOs are C# `record`s in `SpendWise.Contracts`. Limits are taken from the spec.

| DTO | Fields |
|-----|--------|
| `RegisterRequest` | username, email, password |
| `LoginRequest` | username, password |
| `AuthResponse` | token, expiresAt, user (`UserDto`) |
| `UserDto` | id, username, email, role |
| `CategoryRequest` | name, color |
| `CategoryDto` | id, name, color, isGlobal |
| `ExpenseRequest` | amount, date, categoryId, description? |
| `ExpenseDto` | id, amount, date, description?, categoryId, categoryName, categoryColor |
| `ExpenseQuery` (query string) | year?, month?, categoryId?, page = 1, pageSize = 20 |
| `PagedResult<T>` | items, page, pageSize, totalCount, totalPages |
| `SetBudgetRequest` | categoryId, year, month, limit |
| `BudgetDto` | id, categoryId, categoryName, categoryColor, year, month, limit |
| `CopyBudgetsRequest` | year, month (the target month) |
| `CopyBudgetsResult` | copiedCount, skippedCount |
| `MonthlySummaryDto` | year, month, total, expenseCount, previousMonthTotal, changePercent?, categories (`CategorySummaryDto[]`) |
| `CategorySummaryDto` | categoryId, categoryName, categoryColor, spent, percentOfTotal, budgetLimit?, remaining?, isOverBudget |
| `MonthlyTotalDto` | year, month, total |
| `AdminUserDto` | id, username, email, role, isActive, createdAt, expenseCount |
| `SetUserStatusRequest` | isActive |

Shared constants: `Roles` (`User`, `Admin`) and `ValidationLimits` (all the limits from the spec, in one place).

**JWT contents:** user ID, username and role. Valid for 2 hours. Issuer `SpendWise.Api`, audience `SpendWise.Web`.

**Known trade-off:** deactivating a user stops their *next* login. A token they already have keeps working until it expires (at most 2 hours). Revoking tokens would need refresh tokens, which are out of scope.

## 5. Services

Controllers read the user's ID and role from the JWT and pass them in. Services never touch `HttpContext`, so they're easy to test.

| Service | Does |
|---------|------|
| `AuthService` | Register (unique name and email, password strength, hashing), log in (checks the password and that the account is active), get the current user |
| `TokenService` | Creates the JWT |
| `CategoryService` | List visible categories. Create, edit and delete with the ownership, unique-name and "in use" rules |
| `ExpenseService` | List (month, category, paging), get, create, edit, delete. Rejects future dates, invisible categories and other users' items |
| `BudgetService` | List, create or update, delete, copy from the previous month |
| `ReportService` | Monthly summary with budgets and over-budget flags, and the N-month trend |
| `UserAdminService` | List users with expense counts, activate or deactivate (not yourself) |

## 6. Web client pages

| Page | Uses |
|------|------|
| Login, Register, Logout | auth endpoints |
| Dashboard (`/`) with a month picker | monthly report and trend: summary cards, category table with over-budget rows in red, doughnut and bar charts (Chart.js) |
| Expenses: list, add, edit, delete | expenses + categories |
| Categories: list, add, edit, delete | categories |
| Budgets: list, set, delete, copy last month | budgets + categories |
| Admin › Users (admins only) | admin endpoints |

Bootstrap 5 for the layout. API errors appear next to form fields or as alert messages.

## 7. Testing

- xUnit tests for every service. Each test gets a fresh in-memory SQLite database, and "today" is fixed to 2026-10-15.
- One test class per service, covering the main path and each rule in the spec: duplicates, ownership/404, future dates, in-use deletes, upsert, copy counts, percentages, over budget, empty months, zero-filled trend across a year boundary, can't deactivate yourself.
- After the API and the client are built: a manual run-through of all user stories in the browser, plus a Swagger check.

## 8. Configuration

- **API** `appsettings.json`: connection string `Data Source=spendwise.db`; `Jwt` section (Issuer, Audience, Key, ExpiryMinutes = 120); `Seed:Enabled`. The development JWT key goes in `appsettings.Development.json`.
- **Web** `appsettings.json`: `Api:BaseUrl = http://localhost:5080/`.

## Change log

| Date | Change | Why |
|------|--------|-----|
| 2026-10-31 | First version, then shortened for readability | — |
| 2026-10-31 | Added `ExpenseQuery`, `CopyBudgetsRequest` and `SetUserStatusRequest` to the DTO table | Found during Group 1: these request types were used in §4 but not listed |
| 2026-10-31 | Seed data: maria has about 55 expenses (was "about 40") | Found during Group 5: the generated demo data has 54 |
