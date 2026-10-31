using System.Net.Http.Headers;
using SpendWise.Web.Infrastructure;

namespace SpendWise.Web.ApiClient;

/// <summary>Adds the logged-in user's JWT (stored in the auth cookie) to outgoing API requests.</summary>
public class BearerTokenHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = httpContextAccessor.HttpContext?.User.FindFirst(AuthClaims.AccessToken)?.Value;
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return base.SendAsync(request, cancellationToken);
    }
}
