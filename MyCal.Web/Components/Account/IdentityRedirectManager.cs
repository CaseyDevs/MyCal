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

    public void RedirectTo(
        string uri,
        Dictionary<string, object?> queryParameters)
    {
        var path = navigationManager
            .ToAbsoluteUri(uri)
            .GetLeftPart(UriPartial.Path);

        var destination = navigationManager.GetUriWithQueryParameters(
            path,
            queryParameters);

        RedirectTo(destination);
    }
}