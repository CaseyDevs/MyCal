using Microsoft.AspNetCore.Components;

namespace MyCal.Web.Components.Account;

internal sealed class IdentityRedirectManager(
    NavigationManager navigationManager)
{
    public void RedirectTo(string? uri)
    {
        uri ??= "";

        // Prevent redirects to external websites.
        if (!Uri.IsWellFormedUriString(uri, UriKind.Relative))
        {
            uri = navigationManager.ToBaseRelativePath(uri);
        }

        navigationManager.NavigateTo(uri);
    }
}