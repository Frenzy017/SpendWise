using SpendWise.Data.Entities;
using SpendWise.Services.Models;

namespace SpendWise.Services.Interfaces;

public interface ITokenService
{
    /// <summary>Creates a signed JWT holding the user's ID, username and role (FR-AUTH-03).</summary>
    AccessToken CreateToken(User user);
}
