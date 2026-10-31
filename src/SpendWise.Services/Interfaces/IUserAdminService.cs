using SpendWise.Contracts.Admin;

namespace SpendWise.Services.Interfaces;

public interface IUserAdminService
{
    Task<IReadOnlyList<AdminUserDto>> GetUsersAsync();

    /// <summary>Activates or deactivates a user. An admin cannot change their own account.</summary>
    Task SetStatusAsync(int adminId, int userId, bool isActive);
}
