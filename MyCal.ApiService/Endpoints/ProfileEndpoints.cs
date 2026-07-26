using System.Security.Claims;
using MyCal.ApiService.Abstractions;
using MyCal.ApiService.Features.Profiles;
using MyCal.ApiService.Features.Users;

namespace MyCal.ApiService.Endpoints;

public static class ProfileEndpoints
{
    public static WebApplication MapProfileEndpoints(
        this WebApplication app)
    {
        var profiles = app.MapGroup("/profiles");

        profiles.MapGet(
            "/me", async (
                ClaimsPrincipal principal,
                IQueryHandler<GetProfileByIdentityIdQuery, UserResponse?> handler,
                CancellationToken cancellationToken) =>
            {
                var identityUserId = principal.FindFirstValue(
                    ClaimTypes.NameIdentifier
                );

                if (identityUserId is null)
                {
                    return Results.Unauthorized();
                }

                var query = new GetProfileByIdentityIdQuery(identityUserId);
                var result = await handler.HandleAsync(query, cancellationToken);

                return result is null
                    ? Results.NotFound()
                    : Results.Ok(result);
            });

        return app;
    }
}
