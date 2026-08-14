using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using MyCal.Domain.Abstractions;
using MyCal.Application.Features.Profiles;
using MyCal.Application.Features.Users;

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
                [FromQuery] string? identityUserId,
                IQueryHandler<GetProfileByIdentityIdQuery, UserResponse?> handler,
                CancellationToken cancellationToken) =>
            {
                identityUserId ??= principal.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (identityUserId is null)
                {
                    return Results.Unauthorized();
                }
                
                var result = await handler.HandleAsync(
                    new GetProfileByIdentityIdQuery(identityUserId), 
                    cancellationToken);

                return result is null
                    ? Results.NotFound()
                    : Results.Ok(result);
            });

        return app;
    }
}
