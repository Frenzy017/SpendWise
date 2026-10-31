using Microsoft.EntityFrameworkCore;
using SpendWise.Contracts.Admin;
using SpendWise.Data;
using SpendWise.Services.Exceptions;
using SpendWise.Services.Interfaces;

namespace SpendWise.Services.Implementations;

public class UserAdminService(SpendWiseDbContext db) : IUserAdminService
{
    // Only the expense count is exposed: admins must not see users' spending (constitution, security rules).
    public async Task<IReadOnlyList<AdminUserDto>> GetUsersAsync() =>
        await db.Users
            .OrderBy(u => u.NormalizedUsername)
            .Select(u => new AdminUserDto(
                u.Id, u.Username, u.Email, u.Role, u.IsActive, u.CreatedAt, u.Expenses.Count))
            .ToListAsync();

    public async Task SetStatusAsync(int adminId, int userId, bool isActive)
    {
        if (adminId == userId)
            throw new BusinessRuleException("You cannot change the status of your own account.");

        var user = await db.Users.FindAsync(userId)
            ?? throw new NotFoundException("User not found.");

        user.IsActive = isActive;
        await db.SaveChangesAsync();
    }
}
