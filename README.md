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

Each requirement from [spec.md](specs/spec.md) mapped to where it's implemented and where it's tested. Paths are relative to `src/` (code) and `tests/SpendWise.Services.Tests/` (tests). "Manual" means it was checked in the browser or against the running API (task T4.6 / T5.10).

### Functional requirements

| ID | Requirement (short) | Implemented in | Tested by |
|----|---------------------|----------------|-----------|
| FR-AUTH-01 | Register, new accounts are Users | `Services/Implementations/AuthService.cs` (`RegisterAsync`), `Api/Controllers/AuthController.cs`, `Web/Controllers/AccountController.cs` | `AuthServiceTests`: `RegisterAsync_ValidRequest_CreatesUserRoleWithHashedPassword`, `RegisterAsync_PasswordWithoutLetterOrDigit_ThrowsBusinessRule` |
| FR-AUTH-02 | Unique username and email, ignoring case | `AuthService.RegisterAsync`, unique indexes in `Data/Configurations/UserConfiguration.cs` | `AuthServiceTests`: `RegisterAsync_UsernameTakenWithDifferentCase_ThrowsConflict`, `RegisterAsync_EmailTakenWithDifferentCase_ThrowsConflict` |
| FR-AUTH-03 | Signed JWT (2 h) with ID, name, role | `Services/Implementations/TokenService.cs`, JWT validation in `Api/Program.cs` | `TokenServiceTests` (both tests), `AuthServiceTests.LoginAsync_CorrectCredentialsAnyUsernameCase_ReturnsTokenAndUser` |
| FR-AUTH-04 | Generic login error; own error for deactivated | `AuthService.LoginAsync`, `Api/Infrastructure/GlobalExceptionHandler.cs` | `AuthServiceTests`: `LoginAsync_WrongPassword_…`, `LoginAsync_UnknownUser_ThrowsSameGenericError`, `LoginAsync_DeactivatedUser_ThrowsForbidden` |
| FR-AUTH-05 | JWT required everywhere except register/login | Fallback authorization policy in `Api/Program.cs`, `[AllowAnonymous]` in `AuthController` | Manual: request without a token returns 401 |
| FR-CAT-01 | See global + own categories | `CategoryService.GetVisibleAsync`, `Internal/CategoryQueries.cs` | `CategoryServiceTests.GetVisibleAsync_ReturnsGlobalAndOwnButNotOthers_SortedByName` |
| FR-CAT-02 | Create personal categories | `CategoryService.CreateAsync`, `Web/Controllers/CategoriesController.cs` | `CategoryServiceTests.CreateAsync_AsUser_CreatesPersonalCategory` |
| FR-CAT-03 | Unique visible name, ignoring case | `CategoryService.EnsureNameIsAvailableAsync` | `CategoryServiceTests`: `CreateAsync_NameMatchesGlobalIgnoringCase_…`, `CreateAsync_NameMatchesOwnCategory_…`, `CreateAsync_NameMatchesAnotherUsersCategory_Succeeds`, `UpdateAsync_RenameToExistingName_…`, `UpdateAsync_SameNameDifferentCase_Succeeds` |
| FR-CAT-04 | Edit/delete only own categories | `CategoryService.GetEditableAsync` | `CategoryServiceTests`: `UpdateAsync_OwnCategory_…`, `UpdateAsync_GlobalCategoryAsUser_ThrowsForbidden`, `UpdateAsync_AnotherUsersCategory_ThrowsNotFound`, `DeleteAsync_GlobalCategoryAsUser_ThrowsForbidden` |
| FR-CAT-05 | Category in use can't be deleted | `CategoryService.DeleteAsync`, `Restrict` delete rules in `ExpenseConfiguration` / `BudgetConfiguration` | `CategoryServiceTests`: `DeleteAsync_CategoryWithExpenses_…`, `DeleteAsync_CategoryWithBudget_…`, `DeleteAsync_UnusedOwnCategory_RemovesIt` |
| FR-CAT-06 | Admins manage global categories | `CategoryService.CreateAsync/UpdateAsync/DeleteAsync` (`isAdmin`) | `CategoryServiceTests`: `CreateAsync_AsAdmin_CreatesGlobalCategory`, `UpdateAsync_GlobalCategoryAsAdmin_Updates`, `DeleteAsync_GlobalCategoryUsedByAnyUser_AsAdmin_ThrowsConflict` |
| FR-EXP-01 | Create expense (amount, date, category, note) | `ExpenseService.CreateAsync`, `Api/Controllers/ExpensesController.cs`, `Web/Views/Expenses/Form.cshtml` | `ExpenseServiceTests`: `CreateAsync_ValidRequest_…`, `CreateAsync_AmountOutOfRange_…`, `CreateAsync_BlankDescription_StoredAsNull` |
| FR-EXP-02 | No future dates; category must be visible | `ExpenseService.ValidateAsync` | `ExpenseServiceTests`: `CreateAsync_FutureDate_…`, `CreateAsync_DateToday_Succeeds`, `CreateAsync_AnotherUsersCategory_…`, `UpdateAsync_FutureDate_…` |
| FR-EXP-03 | List by month and category, newest first, paginated | `ExpenseService.GetPagedAsync`, `Web/Views/Expenses/Index.cshtml` | `ExpenseServiceTests`: `GetPagedAsync_NoMonthGiven_…`, `GetPagedAsync_CategoryFilter_…`, `GetPagedAsync_SecondPage_…`, `GetPagedAsync_InvalidMonth_…` |
| FR-EXP-04 | Edit own expense | `ExpenseService.UpdateAsync` | `ExpenseServiceTests.UpdateAsync_OwnExpense_UpdatesFields` |
| FR-EXP-05 | Delete own expense | `ExpenseService.DeleteAsync` | `ExpenseServiceTests.DeleteAsync_OwnExpense_RemovesIt` |
| FR-EXP-06 | Other users' expenses → 404 | `ExpenseService.GetOwnAsync` | `ExpenseServiceTests`: `GetByIdAsync_AnotherUsersExpense_…`, `UpdateAsync_AnotherUsersExpense_…`, `DeleteAsync_AnotherUsersExpense_…`, `GetPagedAsync_OtherUsersExpenses_AreNotReturned` |
| FR-BUD-01 | Set monthly limit (upsert, one per month) | `BudgetService.SetAsync`, unique index in `BudgetConfiguration` | `BudgetServiceTests`: `SetAsync_NoExistingBudget_…`, `SetAsync_ExistingBudget_UpdatesLimitWithoutDuplicate`, `SetAsync_InvisibleCategory_…`, `SetAsync_InvalidMonthOrLimit_…` |
| FR-BUD-02 | List and delete budgets | `BudgetService.GetForMonthAsync/DeleteAsync`, `Web/Views/Budgets/Index.cshtml` | `BudgetServiceTests`: `GetForMonthAsync_ReturnsOnlyOwnBudgetsForThatMonth`, `DeleteAsync_OwnBudget_…`, `DeleteAsync_AnotherUsersBudget_…` |
| FR-BUD-03 | Copy last month's budgets | `BudgetService.CopyFromPreviousMonthAsync` | `BudgetServiceTests`: `CopyFromPreviousMonthAsync_CopiesMissingAndSkipsExisting`, `CopyFromPreviousMonthAsync_January_…` |
| FR-REP-01 | Monthly summary with previous-month change | `ReportService.GetMonthlySummaryAsync`, `Web/Views/Home/Index.cshtml` | `ReportServiceTests`: `…ComputesTotalCountAndPercentages`, `…ComparesWithPreviousMonth`, `…NoSpendingPreviousMonth_ChangePercentIsNull`, `…EmptyMonth_ReturnsZeros`, `…IgnoresOtherUsersData` |
| FR-REP-02 | Per-category lines with budget and over-budget flag | `ReportService.GetMonthlySummaryAsync` | `ReportServiceTests`: `…PercentagesRoundedToOneDecimal`, `…SpentOverLimit_…`, `…SpentEqualsLimit_…`, `…NoBudget_NeverOverBudget`, `…BudgetWithoutSpending_AppearsWithZero`, `…SortsCategoriesBySpentDescending` |
| FR-REP-03 | N-month trend, zero-filled | `ReportService.GetTrendAsync`, bar chart in `Home/Index.cshtml` | `ReportServiceTests`: `GetTrendAsync_SpansYearBoundaryAndZeroFillsMissingMonths`, `GetTrendAsync_MonthsOutOfRange_…` |
| FR-ADM-01 | List users with expense counts | `UserAdminService.GetUsersAsync`, `Web/Views/Admin/Users.cshtml` | `UserAdminServiceTests.GetUsersAsync_ReturnsAllUsersWithExpenseCountsSortedByUsername` |
| FR-ADM-02 | Activate/deactivate, not yourself | `UserAdminService.SetStatusAsync` | `UserAdminServiceTests`: `SetStatusAsync_Deactivate_…`, `SetStatusAsync_Reactivate_…`, `SetStatusAsync_OwnAccount_…`, `SetStatusAsync_UnknownUser_…` |
| FR-ADM-03 | Non-admins get 403 | `[Authorize(Roles = Roles.Admin)]` on `Api/Controllers/AdminController.cs` and `Web/Controllers/AdminController.cs` | Manual: `maria` gets 403 from `/api/admin/users` and "Access denied" in the web client |
| FR-WEB-01 | All client pages | `Web/Controllers/*`, `Web/Views/*` | Manual browser run-through of US-01…US-12 (T5.10) |
| FR-WEB-02 | HTTP only; JWT in HttpOnly cookie | `Web/ApiClient/SpendWiseApiClient.cs`, `Web/ApiClient/BearerTokenHandler.cs`, cookie setup in `Web/Program.cs`; Web references only Contracts | Manual + project references (`SpendWise.Web.csproj`) |
| FR-WEB-03 | Friendly errors; 401 → login | `Web/Infrastructure/ApiExceptionFilter.cs`, `Web/Infrastructure/ModelStateExtensions.cs` | Manual: validation, duplicate and in-use errors shown in forms and alerts |
| FR-WEB-04 | Admin links only for admins | `Web/Views/Shared/_Layout.cshtml` | Manual: navbar as `maria` vs `admin` |

### Non-functional requirements

| ID | Requirement (short) | Where |
|----|---------------------|-------|
| NFR-01 | Separate API and client over HTTP | Two runnable projects, `SpendWiseApiClient` |
| NFR-02 | Layered design with shared DTOs | Project references (plan §2), `Services/Mapping/MappingExtensions.cs` |
| NFR-03 | Hashed passwords | `PasswordHasher<User>` in `AuthService` (test: `RegisterAsync_ValidRequest_…` checks the hash isn't the password) |
| NFR-04 | Data isolation in the Services layer | `userId` filters in every service; tests named `…AnotherUsers…` / `…OtherUsers…` |
| NFR-05 | JWT key from config, ≥ 256 bits | `appsettings*.json`, length check in `Api/Program.cs` |
| NFR-06 | `ProblemDetails` errors, no leaks | `Api/Infrastructure/GlobalExceptionHandler.cs`, `UseStatusCodePages` |
| NFR-07 | `decimal(18,2)`, DB constraints | `Data/Configurations/*`, `Data/Migrations/*_InitialCreate.cs` |
| NFR-08 | Every business requirement tested | 78 xUnit tests (table above) |
| NFR-09 | Works on a phone screen | Bootstrap 5 responsive layout (checked at 375 px width) |
| NFR-10 | Fast, paginated lists | Paginated `/api/expenses`, index on (UserId, Date) |
| NFR-11 | Only the .NET SDK needed; auto setup | `Database.Migrate()` + `DbSeeder` on API startup |
| NFR-12 | Swagger + README | Swagger UI at `/swagger`, this README |
