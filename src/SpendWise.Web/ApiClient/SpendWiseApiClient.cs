using System.Net.Http.Json;
using System.Text.Json;
using SpendWise.Contracts.Admin;
using SpendWise.Contracts.Auth;
using SpendWise.Contracts.Budgets;
using SpendWise.Contracts.Categories;
using SpendWise.Contracts.Common;
using SpendWise.Contracts.Expenses;
using SpendWise.Contracts.Reports;

namespace SpendWise.Web.ApiClient;

/// <summary>Typed wrapper around the SpendWise REST API (plan §4). The only way the client reaches data.</summary>
public class SpendWiseApiClient(HttpClient http)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // ---- Auth ----
    public Task<AuthResponse> LoginAsync(LoginRequest request) => PostAsync<AuthResponse>("api/auth/login", request);
    public Task<AuthResponse> RegisterAsync(RegisterRequest request) => PostAsync<AuthResponse>("api/auth/register", request);

    // ---- Categories ----
    public Task<List<CategoryDto>> GetCategoriesAsync() => GetAsync<List<CategoryDto>>("api/categories");
    public Task<CategoryDto> CreateCategoryAsync(CategoryRequest request) => PostAsync<CategoryDto>("api/categories", request);
    public Task<CategoryDto> UpdateCategoryAsync(int id, CategoryRequest request) => PutAsync<CategoryDto>($"api/categories/{id}", request);
    public Task DeleteCategoryAsync(int id) => DeleteAsync($"api/categories/{id}");

    // ---- Expenses ----
    public Task<PagedResult<ExpenseDto>> GetExpensesAsync(int year, int month, int? categoryId, int page)
    {
        var url = $"api/expenses?year={year}&month={month}&page={page}";
        if (categoryId is not null)
            url += $"&categoryId={categoryId}";
        return GetAsync<PagedResult<ExpenseDto>>(url);
    }

    public Task<ExpenseDto> GetExpenseAsync(int id) => GetAsync<ExpenseDto>($"api/expenses/{id}");
    public Task<ExpenseDto> CreateExpenseAsync(ExpenseRequest request) => PostAsync<ExpenseDto>("api/expenses", request);
    public Task<ExpenseDto> UpdateExpenseAsync(int id, ExpenseRequest request) => PutAsync<ExpenseDto>($"api/expenses/{id}", request);
    public Task DeleteExpenseAsync(int id) => DeleteAsync($"api/expenses/{id}");

    // ---- Budgets ----
    public Task<List<BudgetDto>> GetBudgetsAsync(int year, int month) => GetAsync<List<BudgetDto>>($"api/budgets?year={year}&month={month}");
    public Task<BudgetDto> SetBudgetAsync(SetBudgetRequest request) => PutAsync<BudgetDto>("api/budgets", request);
    public Task DeleteBudgetAsync(int id) => DeleteAsync($"api/budgets/{id}");
    public Task<CopyBudgetsResult> CopyBudgetsAsync(CopyBudgetsRequest request) => PostAsync<CopyBudgetsResult>("api/budgets/copy-previous", request);

    // ---- Reports ----
    public Task<MonthlySummaryDto> GetMonthlySummaryAsync(int year, int month) =>
        GetAsync<MonthlySummaryDto>($"api/reports/monthly?year={year}&month={month}");

    public Task<List<MonthlyTotalDto>> GetTrendAsync(int year, int month, int months = 6) =>
        GetAsync<List<MonthlyTotalDto>>($"api/reports/trend?year={year}&month={month}&months={months}");

    // ---- Admin ----
    public Task<List<AdminUserDto>> GetUsersAsync() => GetAsync<List<AdminUserDto>>("api/admin/users");

    public async Task SetUserStatusAsync(int id, bool isActive) =>
        await EnsureSuccessAsync(await http.PatchAsJsonAsync($"api/admin/users/{id}/status", new SetUserStatusRequest(isActive), Json));

    // ---- Helpers ----
    private async Task<T> GetAsync<T>(string url) => await ReadAsync<T>(await http.GetAsync(url));
    private async Task<T> PostAsync<T>(string url, object body) => await ReadAsync<T>(await http.PostAsJsonAsync(url, body, Json));
    private async Task<T> PutAsync<T>(string url, object body) => await ReadAsync<T>(await http.PutAsJsonAsync(url, body, Json));
    private async Task DeleteAsync(string url) => await EnsureSuccessAsync(await http.DeleteAsync(url));

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<T>(Json))!;
    }

    /// <summary>Turns an error response into an ApiException carrying the ProblemDetails message (FR-WEB-03).</summary>
    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        ProblemBody? problem = null;
        try
        {
            problem = await response.Content.ReadFromJsonAsync<ProblemBody>(Json);
        }
        catch (Exception)
        {
            // Body was empty or not JSON; fall back to the status text below.
        }

        var message = problem?.Detail ?? problem?.Title ?? response.ReasonPhrase ?? "The request failed.";
        throw new ApiException((int)response.StatusCode, message, problem?.Errors ?? new Dictionary<string, string[]>());
    }

    private sealed record ProblemBody(string? Title, string? Detail, Dictionary<string, string[]>? Errors);
}
