# SpendWise: Tasks

Work through the groups in order. After each group: build → run the tests → tick the boxes → write a short summary.
If something in the spec turns out to be wrong, fix the spec first, then the code.

## Group 0: Setup (~20 min)

- [x] **T0.1** Create the solution and the six projects from plan §2, targeting .NET 10. *(NFR-01, 02)*
- [x] **T0.2** Add the project references (Web → Contracts only) and NuGet packages. *(NFR-02)*
- [x] **T0.3** Set the ports (API 5080, Web 5090) and add a `.gitignore`. *(NFR-11)*
- [x] **T0.4** ✔ The empty solution builds.

## Group 1: DTOs (~25 min)

- [x] **T1.1** `Roles`, validation limits and `PagedResult<T>`. *(NFR-02)*
- [x] **T1.2** Auth DTOs. *(FR-AUTH-01, 03)*
- [x] **T1.3** Category and expense DTOs. *(FR-CAT-02, FR-EXP-01, 03)*
- [x] **T1.4** Budget, report and admin DTOs. *(FR-BUD-01, 03, FR-REP-01..03, FR-ADM-01, 02)*
- [x] **T1.5** ✔ Build.

## Group 2: Database (~35 min)

- [x] **T2.1** Entities: User, Category, Expense, Budget. *(NFR-07)*
- [x] **T2.2** EF configuration: lengths, decimals, unique indexes, delete rules. *(FR-AUTH-02, FR-BUD-01, FR-CAT-05, NFR-07)*
- [x] **T2.3** `SpendWiseDbContext` and the first migration. *(NFR-11)*
- [x] **T2.4** ✔ Build.

## Group 3: Services and tests (~2 h)

- [x] **T3.1** Exceptions, entity → DTO mapping, DI registration. *(NFR-06)*
- [x] **T3.2** Test setup: in-memory SQLite database, fixed date. *(NFR-08)*
- [x] **T3.3** `TokenService` and `AuthService` + tests. *(FR-AUTH-01..04, NFR-03)*
- [x] **T3.4** `CategoryService` + tests. *(FR-CAT-01..06)*
- [x] **T3.5** `ExpenseService` + tests. *(FR-EXP-01..06, NFR-04)*
- [x] **T3.6** `BudgetService` + tests. *(FR-BUD-01..03)*
- [x] **T3.7** `ReportService` + tests. *(FR-REP-01..03)*
- [x] **T3.8** `UserAdminService` + tests. *(FR-ADM-01, 02)*
- [x] **T3.9** ✔ Build, all tests green.

## Group 4: API (~1 h)

- [x] **T4.1** Startup: database, services, JWT auth, JSON settings, migrate on start. *(FR-AUTH-05, NFR-05, 11)*
- [x] **T4.2** Global error handler and helpers that read the user ID and role from the token. *(NFR-06)*
- [x] **T4.3** Swagger with an "Authorize" button for the JWT. *(NFR-12)*
- [x] **T4.4** Auth, Categories and Expenses controllers. *(FR-AUTH, FR-CAT, FR-EXP)*
- [x] **T4.5** Budgets, Reports and Admin controllers. *(FR-BUD, FR-REP, FR-ADM)*
- [x] **T4.6** ✔ Build + tests. Swagger check: register → add an expense → see the summary, and a non-admin gets 403 on admin endpoints.

## Group 5: Web client (~2 h)

- [x] **T5.1** Startup: login cookie, typed API client that adds the JWT. *(FR-WEB-02, NFR-01)*
- [x] **T5.2** API client for all endpoints, error parsing, 401 → login page. *(FR-WEB-02, 03)*
- [x] **T5.3** Layout: navbar (admin links only for admins), alerts, month picker. *(FR-WEB-01, 04, NFR-09)*
- [x] **T5.4** Login, Register, Logout. *(US-01, 02)*
- [x] **T5.5** Dashboard with summary, category table and charts. *(US-10, 11)*
- [x] **T5.6** Expenses pages. *(US-06, 07, 08)*
- [x] **T5.7** Categories pages. *(US-03, 04, 05)*
- [x] **T5.8** Budgets pages. *(US-09)*
- [x] **T5.9** Admin › Users page. *(US-12)*
- [x] **T5.10** ✔ Build + tests. Click through every user story with both apps running.

## Group 6: Seed data and delivery (~45 min)

- [x] **T6.1** Seeder (plan §3), runs only on an empty database, dates relative to today. *(NFR-11)*
- [x] **T6.2** README: what it is, how to run each part, test accounts, and a table mapping each requirement to its code and tests. *(NFR-12)*
- [x] **T6.3** Final check: specs match the code, and a fresh build and test run passes.

## Progress

| Group | Status |
|-------|--------|
| 0 Setup | ✅ |
| 1 DTOs | ✅ |
| 2 Database | ✅ |
| 3 Services + tests | ✅ |
| 4 API | ✅ |
| 5 Web client | ✅ |
| 6 Seed + delivery | ✅ |

Total estimate: about 7 hours.
