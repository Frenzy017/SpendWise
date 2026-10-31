using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SpendWise.Data.Entities;
using SpendWise.Services.Implementations;
using SpendWise.Services.Interfaces;

namespace SpendWise.Services;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers all SpendWise business services. The caller registers the DbContext and JwtOptions.</summary>
    public static IServiceCollection AddSpendWiseServices(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<IBudgetService, BudgetService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        return services;
    }
}
