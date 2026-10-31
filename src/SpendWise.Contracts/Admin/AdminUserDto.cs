namespace SpendWise.Contracts.Admin;

public record AdminUserDto(
    int Id,
    string Username,
    string Email,
    string Role,
    bool IsActive,
    DateTime CreatedAt,
    int ExpenseCount);
