# SpendWise: Constitution

These are the ground rules for the project. If another document or the code disagrees with this one, this one wins.

## How we work

1. **Spec before code.** A feature is built only after it is in `spec.md`, `plan.md` and `tasks.md`.
2. **Keep specs and code in sync.** If the code needs to differ from the spec, update the spec first.
3. **Everything is traceable.** Each requirement has an ID (e.g. `FR-EXP-01`). Each task names the IDs it covers. The README maps each ID to its code and tests.
4. **Small steps.** Build one task group at a time. A group is done only when the solution builds, all tests pass and its tasks are ticked.
5. **No extras.** Build only what the spec asks for.

## Architecture rules

- The **API** and the **web client** are two separate programs. The client talks to the API over HTTP only. It never touches the database or the business logic directly.
- Layers depend in one direction only: **Web → (HTTP) → Api → Services → Data**. A shared **Contracts** project holds the DTOs that the API and the client both use.
- All business rules live in **Services**. Controllers only receive the request, call a service and return the result.
- Database entities never leave the Services layer. Everything outside it uses DTOs.

## Technical decisions

| Decision | Why |
|----------|-----|
| .NET 10 (LTS) | Current long-term-support version, installed on this machine. |
| ASP.NET Core Web API | Standard way to build a REST API in C#. |
| ASP.NET Core MVC client | Simple server-rendered pages. The login token stays on the server in a secure cookie, never in the browser's JavaScript. |
| EF Core + SQLite | No database server to install. The database is a single file, created automatically. |
| JWT login with roles `User` and `Admin` | Works well when the client and server are separate programs. |
| Password hashing with `PasswordHasher<T>` | Safe, standard hashing without the full Identity framework. |
| Services throw exceptions for rule violations | One central handler turns them into the right HTTP status code, which keeps controllers short and tests simple. |
| `TimeProvider` for "today" | Tests can fix the date, so date rules are testable. |
| xUnit + in-memory SQLite for tests | Tests run against a real database engine, so unique keys and foreign keys behave as they do in production. |
| Swagger | The API can be tried out on its own, without the client. |
| HTTP (not HTTPS) on localhost | No certificate setup needed to run the project. |
| Money as `decimal`, one currency (EUR) | `decimal` avoids rounding errors. |

## Coding standards

- Code and comments in English.
- Standard C# naming: `PascalCase` for types and methods, `camelCase` for variables, `_camelCase` for private fields, `I` prefix for interfaces, `Async` suffix for async methods.
- Nullable reference types on. Async for all database and HTTP calls.
- DTOs are `record` types. Simple input checks use attributes (`[Required]`, `[Range]`). Business rules go in services.
- Comments explain *why*, not *what*.
- Tests are named `Method_Scenario_Result` and follow Arrange / Act / Assert.

## Security rules

- Passwords are stored only as hashes.
- Users can see and change only their own data. Asking for someone else's item returns **404**, so its existence isn't revealed.
- Admins manage categories and accounts but **cannot see other users' expenses**.
- The JWT signing key lives in configuration, not in code.
- Every endpoint needs a login except register and login.

## Definition of done (per task group)

- `dotnet build` has 0 errors
- `dotnet test` is all green
- Each new business rule has a test
- Tasks are ticked in `tasks.md`

## Change log

| Date | Change |
|------|--------|
| 2026-10-31 | First version |
| 2026-10-31 | Shortened and reworded for readability, no content changes |
