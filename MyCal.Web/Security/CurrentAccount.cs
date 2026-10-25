using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using MyCal.Infrastructure.Identity;

namespace MyCal.Web.Security;

public sealed class CurrentAccount(AuthenticationStateProvider authentication, IServiceScopeFactory scopes)
{
    public async Task<(string Id, string Email, bool IsAdmin)> GetAsync()
    {
        var principal = (await authentication.GetAuthenticationStateAsync()).User;
        if (principal.Identity?.IsAuthenticated != true)
            throw new UnauthorizedAccessException("Please sign in again.");
        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();
        await using var scope = scopes.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await manager.FindByIdAsync(id);
        var stamp = principal.FindFirstValue("AspNet.Identity.SecurityStamp");
        if (user is null || stamp != await manager.GetSecurityStampAsync(user))
            throw new UnauthorizedAccessException("Your session has expired. Please sign in again.");
        return (user.Id, user.Email!, user.IsAdmin);
    }

    public async Task<string> IdAsync() => (await GetAsync()).Id;

    public async Task<string> IssueLinkAsync(string email, bool recovery)
    {
        var actor = await GetAsync();
        await using var scope = scopes.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<BetaAccounts>().IssueAsync(actor.Id, email, recovery);
    }
}
