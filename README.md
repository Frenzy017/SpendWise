# SpendWise: Monthly Expense Tracker

SpendWise shows people where their money goes each month. Users record expenses by category (Food, Clothes, Going out, Subscriptions, Gifts…), set a monthly budget per category, and get a dashboard with the month's total, the split by category, the change from last month, a warning for every category that went over budget, and a 6-month trend.

The project was built for the course **Distributed Applications with C#** using **Spec-Driven Development**. The specification came first and was approved before any code was written. It lives in [`specs/`](specs/):

| Document | Contents |
|----------|----------|
| [constitution.md](specs/constitution.md) | Ground rules, architecture rules, technical decisions, coding standards |
| [spec.md](specs/spec.md) | Problem, roles, user stories with acceptance criteria, requirements, out of scope |
| [plan.md](specs/plan.md) | Architecture, data model, full API contract, DTOs |
| [tasks.md](specs/tasks.md) | The ordered implementation checklist (all ticked) |

## Architecture

```
 Browser ──▶ SpendWise.Web (ASP.NET Core MVC, :5090)
                 │  HTTP + JSON, JWT Bearer token
                 ▼
             SpendWise.Api (ASP.NET Core Web API, :5080, Swagger at /swagger)
                 │
                 ▼
             SpendWise.Services (business rules)  ──▶  SpendWise.Data (EF Core)  ──▶  spendwise.db (SQLite)

 SpendWise.Contracts (shared DTOs) is used by Web, Api and Services.
```

- The **API** and the **web client** are two separate programs. The client references only `SpendWise.Contracts` and gets all data over HTTP.
- After login, the client stores the JWT in an encrypted, HttpOnly cookie and sends it as `Authorization: Bearer …` with every API call.
- Two roles: **User** (own data only) and **Admin** (also manages global categories and user accounts, but cannot see other users' expenses).

| Project | Purpose |
|---------|---------|
| `src/SpendWise.Contracts` | DTOs (records) and shared constants |
| `src/SpendWise.Data` | Entities, EF Core `DbContext`, migrations, seed data |
| `src/SpendWise.Services` | Business logic, JWT creation, entity → DTO mapping |
| `src/SpendWise.Api` | REST controllers, JWT validation, error handling, Swagger |
| `src/SpendWise.Web` | MVC client: typed API client, cookie login, Bootstrap 5 views, Chart.js charts |
| `tests/SpendWise.Services.Tests` | 78 xUnit tests for the service layer (in-memory SQLite) |

## How to run

**Prerequisite:** the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0). Nothing else is needed: the SQLite database is created, migrated and filled with demo data automatically the first time the API starts.

Open two terminals in the project root.

**1. Start the API** (http://localhost:5080, Swagger opens at http://localhost:5080/swagger):

```bash
dotnet run --project src/SpendWise.Api
```

**2. Start the web client** (http://localhost:5090):

```bash
dotnet run --project src/SpendWise.Web
```

Then open http://localhost:5090 and log in with one of the test accounts below.

**Run the tests:**

```bash
dotnet test
```

**Start over with fresh demo data:** stop the API, delete `src/SpendWise.Api/spendwise.db`, and start it again.

### Test accounts

| Username | Password | Role | What to look at |
|----------|----------|------|-----------------|
| `admin` | `Admin123!` | Admin | Admin › Users, editing global categories |
| `maria` | `Maria123!` | User | Six months of data, budgets, "Going out" over budget this month, personal "Gym" category |
| `ivan` | `Ivan1234!` | User | A few expenses; can't see maria's data |
| `blocked` | `Blocked123!` | User (deactivated) | Login is refused with "This account has been deactivated" |

These are development-only demo credentials. The JWT signing key in `appsettings.Development.json` is development-only as well.

### Trying the API in Swagger

1. Open http://localhost:5080/swagger.
2. Call `POST /api/auth/login` with `{ "username": "maria", "password": "Maria123!" }` and copy the `token`.
3. Click **Authorize**, paste the token, and call any endpoint.

## API overview

All endpoints except register and login need a JWT. Errors use the standard `ProblemDetails` format. The full contract, with request and response DTOs, is in [plan.md §4](specs/plan.md).

| Area | Endpoints |
|------|-----------|
| Auth | `POST /api/auth/register`, `POST /api/auth/login`, `GET /api/auth/me` |
| Categories | `GET/POST /api/categories`, `PUT/DELETE /api/categories/{id}` |
| Expenses | `GET/POST /api/expenses`, `GET/PUT/DELETE /api/expenses/{id}` |
| Budgets | `GET/PUT /api/budgets`, `DELETE /api/budgets/{id}`, `POST /api/budgets/copy-previous` |
| Reports | `GET /api/reports/monthly`, `GET /api/reports/trend` |
| Admin | `GET /api/admin/users`, `PATCH /api/admin/users/{id}/status` |

## Traceability

This section shows, for every requirement in [spec.md](specs/spec.md), **what it means in practice**, **where the code for it lives**, and **how we know it works**.

- File paths start from `src/`. All tests are in `tests/SpendWise.Services.Tests/`.
- **"Checked by hand"** means it was tested in the browser or by calling the running API (tasks T4.6 and T5.10).

### Functional requirements

#### Accounts and login

| ID | What it means | Where it lives | How it's checked |
|----|---------------|----------------|------------------|
| FR-AUTH-01 | Anyone can sign up. New accounts are always ordinary users, never admins. | `Services/Implementations/AuthService.cs`, the Register page in `Web/Controllers/AccountController.cs` | `AuthServiceTests`: a valid sign-up creates a User with a hashed password; a password with no letter or no digit is rejected. |
| FR-AUTH-02 | Two people can't share a username or email, even if the capitals differ ("Maria" = "maria"). | `AuthService.cs`, plus unique indexes in `Data/Configurations/UserConfiguration.cs` | `AuthServiceTests`: a taken username or email in different case is rejected. |
| FR-AUTH-03 | Logging in gives you a signed token that's valid for 2 hours and says who you are and what your role is. | `Services/Implementations/TokenService.cs`; the API checks the token in `Api/Program.cs` | `TokenServiceTests`; `AuthServiceTests`: login works whatever case you type the username in. |
| FR-AUTH-04 | A wrong password and an unknown user give the same message, so attackers can't guess usernames. A deactivated account gets its own message. | `AuthService.cs`, `Api/Infrastructure/GlobalExceptionHandler.cs` | `AuthServiceTests`: wrong password and unknown user give the same error; a deactivated user is refused. |
| FR-AUTH-05 | Every API endpoint needs you to be logged in, except sign-up and login. | Default "must be logged in" rule in `Api/Program.cs` | Checked by hand: a request without a token gets 401. |

#### Categories

| ID | What it means | Where it lives | How it's checked |
|----|---------------|----------------|------------------|
| FR-CAT-01 | You see the shared categories (Food, Clothes…) plus the ones you made yourself, never anyone else's. | `CategoryService.cs`, `Services/Internal/CategoryQueries.cs` | `CategoryServiceTests`: you get shared + own categories, not other users', sorted by name. |
| FR-CAT-02 | You can make your own categories, e.g. "Gym". | `CategoryService.cs`, `Web/Controllers/CategoriesController.cs` | `CategoryServiceTests`: a user creates a personal category. |
| FR-CAT-03 | You can't have two categories with the same name (ignoring capitals). Another user may use the same name. | `CategoryService.cs` | `CategoryServiceTests`: clashes with a shared or own name are rejected, a name another user has is allowed, and renaming works. |
| FR-CAT-04 | You can only edit or delete your own categories, not the shared ones or other people's. | `CategoryService.cs` | `CategoryServiceTests`: editing your own works; shared ones are forbidden; another user's look like they don't exist. |
| FR-CAT-05 | A category that still has expenses or budgets can't be deleted, so no data is lost. | `CategoryService.cs`, plus delete rules in the database configuration | `CategoryServiceTests`: a category with expenses or a budget can't be deleted; an unused one can. |
| FR-CAT-06 | Admins manage the shared category list. | `CategoryService.cs` | `CategoryServiceTests`: an admin creates and edits shared categories but can't delete one that's in use. |

#### Expenses

| ID | What it means | Where it lives | How it's checked |
|----|---------------|----------------|------------------|
| FR-EXP-01 | You record an expense: amount, date, category and an optional note. | `ExpenseService.cs`, the form in `Web/Views/Expenses/Form.cshtml` | `ExpenseServiceTests`: a valid expense is saved; an amount out of range is rejected; an empty note is stored as empty. |
| FR-EXP-02 | You can't enter an expense dated in the future, or put it in a category you can't see. | `ExpenseService.cs` | `ExpenseServiceTests`: a future date is rejected and today is allowed; another user's category is rejected. |
| FR-EXP-03 | You browse one month at a time, optionally one category, newest first, a page at a time. | `ExpenseService.cs`, `Web/Views/Expenses/Index.cshtml` | `ExpenseServiceTests`: defaults to this month; category filter, paging and invalid months behave correctly. |
| FR-EXP-04 | You can fix a wrong expense. | `ExpenseService.cs` | `ExpenseServiceTests`: editing updates all fields. |
| FR-EXP-05 | You can delete an expense. | `ExpenseService.cs` | `ExpenseServiceTests`: deleting removes it. |
| FR-EXP-06 | Other people's expenses are invisible to you: the app answers "not found" rather than "forbidden", so it doesn't even reveal they exist. | `ExpenseService.cs` | `ExpenseServiceTests`: viewing, editing, deleting or listing another user's expenses never works. |

#### Budgets

| ID | What it means | Where it lives | How it's checked |
|----|---------------|----------------|------------------|
| FR-BUD-01 | You set a monthly limit per category (e.g. Going out: €150). Setting it again changes the limit instead of adding a second one. | `BudgetService.cs`, unique index in the database | `BudgetServiceTests`: a new budget is created; setting it again updates it; invalid values are rejected. |
| FR-BUD-02 | You see your budgets for a month and can delete them. | `BudgetService.cs`, `Web/Views/Budgets/Index.cshtml` | `BudgetServiceTests`: you only see your own budgets for that month and can only delete your own. |
| FR-BUD-03 | One click copies last month's budgets into this month, skipping any you've already set. | `BudgetService.cs` | `BudgetServiceTests`: copies missing ones and skips existing ones; January correctly copies from last December. |

#### Dashboard and reports

| ID | What it means | Where it lives | How it's checked |
|----|---------------|----------------|------------------|
| FR-REP-01 | The dashboard shows the month's total and how it compares with last month. | `ReportService.cs`, `Web/Views/Home/Index.cshtml` | `ReportServiceTests`: totals and % change are right; there's no % if last month was zero; an empty month shows zeros; other users' data is ignored. |
| FR-REP-02 | Each category shows how much you spent, its share of the total, your budget, and a warning if you went over. | `ReportService.cs` | `ReportServiceTests`: going over the limit is flagged, exactly hitting it is not; categories with no budget are never flagged; sorted by highest spending. |
| FR-REP-03 | A bar chart shows your total for each of the last few months, including months where you spent nothing. | `ReportService.cs`, chart in `Web/Views/Home/Index.cshtml` | `ReportServiceTests`: works across a new year and fills empty months with zero. |

#### Admin

| ID | What it means | Where it lives | How it's checked |
|----|---------------|----------------|------------------|
| FR-ADM-01 | Admins see a list of all users and how many expenses each has (but not the expenses themselves). | `UserAdminService.cs`, `Web/Views/Admin/Users.cshtml` | `UserAdminServiceTests`: all users are listed with their counts. |
| FR-ADM-02 | Admins can block or unblock accounts, but not their own. | `UserAdminService.cs` | `UserAdminServiceTests`: block, unblock, refusing to block yourself, and an unknown user. |
| FR-ADM-03 | Ordinary users can't reach admin features. | Admin-only rule on both `AdminController.cs` files (API and web) | Checked by hand: `maria` gets "403 Forbidden" from the API and "Access denied" in the browser. |

#### Web client

| ID | What it means | Where it lives | How it's checked |
|----|---------------|----------------|------------------|
| FR-WEB-01 | The website has all the pages: Login, Register, Dashboard, Expenses, Categories, Budgets and Admin › Users. | `Web/Controllers/`, `Web/Views/` | Checked by hand: went through every user story (US-01 to US-12) in the browser. |
| FR-WEB-02 | The website only talks to the API over HTTP. Your login token is kept in an encrypted cookie that page scripts can't read. | `Web/ApiClient/`, cookie setup in `Web/Program.cs` | Checked by hand; the web project doesn't reference the database or business-logic projects at all. |
| FR-WEB-03 | Errors from the API appear as friendly messages, and an expired login sends you back to the login page. | `Web/Infrastructure/ApiExceptionFilter.cs` | Checked by hand: validation, duplicate-name and "in use" errors appear in the forms. |
| FR-WEB-04 | Only admins see the Admin menu. | `Web/Views/Shared/_Layout.cshtml` | Checked by hand: the menu differs for `maria` and `admin`. |

### Non-functional requirements

These are about *how well* the system works rather than *what* it does.

| ID | What it means | How the project meets it |
|----|---------------|--------------------------|
| NFR-01 | **Distributed.** The API and the website are two separate programs that talk over the network. | Two projects you start separately (ports 5080 and 5090). The website calls the API through `SpendWiseApiClient`. |
| NFR-02 | **Clean layers.** Each part of the code has one job and only depends on the layer below it. | Data → Services → Api, plus a shared Contracts project for the data sent between them. Database objects are converted to DTOs in `Services/Mapping/MappingExtensions.cs` and never leave the Services layer. |
| NFR-03 | **Passwords are safe.** They're never stored as plain text. | Passwords are hashed with ASP.NET Core's `PasswordHasher`. A test checks that the stored value isn't the password. |
| NFR-04 | **Your data is private.** The protection lives in the business logic, not just in the screens, so it can't be bypassed by calling the API directly. | Every service filters by the logged-in user's ID. Tests try to reach other users' data and confirm it fails. |
| NFR-05 | **The token signing key is secure.** It's long enough and isn't hard-coded. | The key is read from `appsettings*.json`, and the API refuses to start if it's shorter than 256 bits. |
| NFR-06 | **Errors are clear and safe.** Errors come in a standard format with the right status code, and never expose internal details. | `Api/Infrastructure/GlobalExceptionHandler.cs` converts every error to the standard `ProblemDetails` format. |
| NFR-07 | **Money and data are stored correctly.** No rounding errors, and the database itself blocks invalid data. | Amounts use `decimal(18,2)`. Unique keys and foreign keys are defined in `Data/Configurations/`. |
| NFR-08 | **Everything is tested.** Every business rule has at least one automated test. | 78 xUnit tests, all passing (see the tables above). |
| NFR-09 | **Works on a phone.** | Bootstrap 5 responsive layout, checked at a 375 px screen width. |
| NFR-10 | **Fast.** Pages load quickly, even with a lot of data. | The expense list is paginated, and the database has an index on user and date. |
| NFR-11 | **Easy to run.** Nothing to install except .NET. | On first start the API creates the SQLite database and fills it with demo data automatically. |
| NFR-12 | **Documented.** | The API can be explored in Swagger at `/swagger`, and this README explains how to run everything. |
