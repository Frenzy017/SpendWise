# SpendWise: Specification

## 1. The problem

Most people, students especially, don't know where their money goes each month. They know they spent "too much", but not how much went on food, going out, clothes, subscriptions or gifts.

**SpendWise** lets users record expenses by category, set a monthly budget for each category, and see a clear monthly overview: the total, the split by category, the change from last month, and a warning for any category that went over budget.

**Goals:** adding an expense takes under 15 seconds · spending by category is visible at a glance · going over budget is obvious · each user's data stays private.

## 2. Roles

| Role | Can do |
|------|--------|
| **Visitor** | Register or log in. |
| **User** | Manage their own expenses, personal categories and budgets. See their own reports. Every new account gets this role. |
| **Admin** | Everything a User can do with their own data, plus manage the shared (global) categories and activate or deactivate accounts. Cannot see other users' expenses. Admin accounts come from seed data only. |

## 3. User stories

Each story lists its acceptance criteria and the requirements (§4) it covers.

### Accounts

**US-01 Register.** As a visitor, I want to create an account so I can start tracking. *(FR-AUTH-01, 02)*
- A valid, unused username and email plus a strong password create a **User** account and log me in.
- A username or email that is already taken shows "Username or email is already in use".
- Invalid fields show a message next to the field.

**US-02 Log in and out.** As a user, I want to log in and out so only I can reach my data. *(FR-AUTH-03, 04, 05)*
- Correct details take me to the dashboard.
- Wrong details show "Invalid username or password", without saying which one was wrong.
- A deactivated account shows "This account has been deactivated".
- After logging out, or when my session expires, protected pages send me to the login page.

### Categories

**US-03 Ready-made categories.** As a user, I want default categories (Food, Clothes, Going out, Subscriptions, Gifts…) so I can start right away. *(FR-CAT-01)*
- I see all global categories plus my own, sorted by name. Global ones are marked.

**US-04 My own categories.** As a user, I want to add, rename and delete my own categories (e.g. "Gym"). *(FR-CAT-02 to 05)*
- A new name must not match any category I can already see (case doesn't matter).
- I can edit only my own categories, not global ones.
- I can't delete a category that has expenses or budgets: "Category is in use".

**US-05 Global categories.** As an admin, I want to manage the shared category list. *(FR-CAT-06)*
- Categories an admin creates are global and visible to everyone.
- The admin can edit and delete them. The same "in use" rule applies, checked across all users.

### Expenses

**US-06 Add an expense.** As a user, I want to record an amount, date, category and optional note. *(FR-EXP-01, 02)*
- The amount must be 0.01–1,000,000. The date can't be in the future and defaults to today.
- The category must be one I can see.

**US-07 Browse expenses.** As a user, I want to see one month's expenses, optionally for one category. *(FR-EXP-03)*
- Shows the current month by default, newest first, 20 per page.
- I can change the month and filter by category. I never see other users' expenses.

**US-08 Edit or delete.** As a user, I want to fix or remove a wrong entry. *(FR-EXP-04, 05, 06)*
- Editing uses the same rules as adding. Deleting asks for confirmation.
- Opening another user's expense shows "not found".

### Budgets

**US-09 Monthly budgets.** As a user, I want a monthly limit per category (e.g. Going out: €150). *(FR-BUD-01 to 03)*
- Setting a limit again for the same category and month updates it instead of creating a duplicate.
- I can remove a budget.
- One click copies last month's budgets into this month, skipping categories that already have one.

### Reports

**US-10 Monthly dashboard.** As a user, I want to see where my money went in a month. *(FR-REP-01, 02)*
- Shows the total, the number of expenses, last month's total and the change in %.
- For each category: amount spent, % of the total, budget, what's left, and a red **over budget** marker.
- A doughnut chart shows spending by category.
- Categories with a budget but no spending still appear. An empty month shows zeros, not errors.

**US-11 Trend.** As a user, I want to see my totals for the last 6 months. *(FR-REP-03)*
- A bar chart shows each month. Months with no spending show 0.

### Administration

**US-12 Manage users.** As an admin, I want to see all accounts and deactivate some. *(FR-ADM-01 to 03)*
- The list shows username, email, role, status, sign-up date and the **number** of expenses (no amounts).
- The admin can deactivate or reactivate anyone except themselves.
- Non-admins get "403 Forbidden" on admin pages and endpoints.

## 4. Functional requirements

**Accounts**
- **FR-AUTH-01** Register with a username (3–32 letters, digits, `_` or `.`), an email, and a password (8–100 characters with at least one letter and one digit). New accounts are Users.
- **FR-AUTH-02** Usernames and emails are unique, ignoring case.
- **FR-AUTH-03** Register and login return a signed JWT, valid for 2 hours, holding the user's ID, name and role.
- **FR-AUTH-04** Wrong credentials give a generic error. A deactivated account gives its own error.
- **FR-AUTH-05** All endpoints except register and login require a valid JWT.

**Categories**
- **FR-CAT-01** Users see global categories plus their own.
- **FR-CAT-02** Users can create personal categories (name 1–40 characters, colour `#RRGGBB`).
- **FR-CAT-03** A name must be unique, ignoring case, among the categories its owner can see.
- **FR-CAT-04** Users can edit and delete only their own categories.
- **FR-CAT-05** A category used by any expense or budget can't be deleted.
- **FR-CAT-06** Admins create, edit and delete global categories.

**Expenses**
- **FR-EXP-01** Create an expense: amount 0.01–1,000,000, date, category, optional description (up to 200 characters).
- **FR-EXP-02** The date can't be after today, and the category must be visible to the user.
- **FR-EXP-03** List your own expenses by month, optionally by category, newest first, paginated (default 20, max 100 per page).
- **FR-EXP-04** Edit your own expense (same rules as creating).
- **FR-EXP-05** Delete your own expense.
- **FR-EXP-06** Another user's expense behaves as if it doesn't exist (404).

**Budgets**
- **FR-BUD-01** Set a monthly limit (0.01–1,000,000) for a category. One budget per user, category and month: setting it again updates it.
- **FR-BUD-02** List and delete your budgets for a month.
- **FR-BUD-03** Copy the previous month's budgets into a month, skipping existing ones, and report how many were copied and skipped.

**Reports**
- **FR-REP-01** Monthly summary: total, count, previous month's total, % change (empty if the previous month was 0), and a line per category.
- **FR-REP-02** Each category line: spent, % of the total, budget, remaining, over budget yes/no. Includes categories with spending or a budget, sorted by spent, highest first.
- **FR-REP-03** Monthly totals for the last N months (1–12, default 6), including months with zero.

**Admin**
- **FR-ADM-01** Admins see all users with their expense counts.
- **FR-ADM-02** Admins can activate or deactivate any user except themselves.
- **FR-ADM-03** Non-admins get 403 on admin endpoints.

**Web client**
- **FR-WEB-01** Pages: Login, Register, Dashboard, Expenses, Categories, Budgets, Admin › Users.
- **FR-WEB-02** The client calls the API only over HTTP. It keeps the JWT in an encrypted, HttpOnly cookie.
- **FR-WEB-03** API errors are shown as friendly messages. A 401 sends the user to the login page.
- **FR-WEB-04** Admin links are shown only to admins.

## 5. Non-functional requirements

| ID | Requirement |
|----|-------------|
| NFR-01 | The API and client are separate programs that talk over HTTP/JSON. |
| NFR-02 | Layered design: Data → Services → Api, plus a shared Contracts (DTO) project. |
| NFR-03 | Passwords are stored only as salted hashes. |
| NFR-04 | The Services layer enforces that users see only their own data, not just the UI. |
| NFR-05 | The JWT key comes from configuration and is at least 256 bits. |
| NFR-06 | Errors use the standard `ProblemDetails` format with correct status codes. No stack traces leak. |
| NFR-07 | Money is `decimal(18,2)`. The database enforces unique keys and foreign keys. |
| NFR-08 | Every business requirement has at least one xUnit test. |
| NFR-09 | The UI works on a phone-sized screen (Bootstrap 5). |
| NFR-10 | Typical requests take under 300 ms locally. Lists are paginated. |
| NFR-11 | Only the .NET 10 SDK is needed. The database is created and seeded automatically. |
| NFR-12 | The API is documented in Swagger. The README explains how to run everything. |

## 6. Out of scope

Multiple currencies · income and savings · recurring expenses · import or export · receipt photos · shared budgets · password reset, email confirmation, 2FA, refresh tokens · creating admins or changing roles through the app · over-budget notifications (the dashboard warning only) · HTTPS and hosting · languages other than English.

## 7. Assumptions

- "Today" means the server's date.
- Months are calendar months, based on the expense date.
- Budgets are optional. Without a budget a category is never "over budget".

## Change log

| Date | Change |
|------|--------|
| 2026-10-31 | First version |
| 2026-10-31 | Shortened and reworded for readability, no requirement changes |
