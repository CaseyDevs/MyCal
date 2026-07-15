using FluentValidation;
using MyCal.ApiService.Features.Users.CreateUser;
using MyCal.ApiService.Features.Users.GetUserById;
using MyCal.ApiService.Features.Users.GetUsers;

namespace MyCal.ApiService.Endpoints;

public static class UserEndpoints
{
    public static RouteGroupBuilder MapUserEndpoints(this RouteGroupBuilder app)
    {
        var users = app.MapGroup("/users");

        users.MapGet("/", async (GetUsersHandler handler, CancellationToken cancellationToken) =>
            Results.Ok(await handler.HandleAsync(cancellationToken)));

        users.MapGet("/{id:int}", async (
            int id,
            GetUserByIdHandler handler,
            CancellationToken cancellationToken) =>
        {
            var user = await handler.HandleAsync(new GetUserByIdRequest(id), cancellationToken);
            return user is null ? Results.NotFound() : Results.Ok(user);
        });

        users.MapPost("/", async (
            CreateUserRequest request,
            IValidator<CreateUserRequest> validator,
            CreateUserHandler handler,
            CancellationToken cancellationToken) =>
        {
            var validation = await validator.ValidateAsync(request, cancellationToken);

            if (!validation.IsValid)
            {
                return Results.ValidationProblem(validation.ToDictionary());
            }

            var result = await handler.HandleAsync(request, cancellationToken);

            return result.EmailAlreadyExists
                ? Results.Conflict(new { message = "An account with this email already exists." })
                : Results.Created($"/users/{result.User!.Id}", result.User);
        });

        return app;
    }
}
