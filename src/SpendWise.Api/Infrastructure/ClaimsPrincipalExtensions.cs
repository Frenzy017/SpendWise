using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using SpendWise.Contracts;

namespace SpendWise.Api.Infrastructure;

public static class ClaimsPrincipalExtensions
{
    /// <summary>The user ID from the JWT "sub" claim. Only call on authenticated requests.</summary>
    public static int GetUserId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? throw new InvalidOperationException("The token has no subject claim."));

    public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole(Roles.Admin);
}
