using FluentValidation;
using MyCal.ApiService.Abstractions;
using MyCal.ApiService.Features.Users;
using MyCal.ApiService.Features.Users.CreateUser;
using MyCal.ApiService.Features.Users.GetUserById;
using MyCal.ApiService.Features.Users.GetUsers;

namespace MyCal.ApiService.Endpoints;

public static class UserEndpoints
{
    public static WebApplication MapUserEndpoints(this WebApplication app)
    {
        var users = app.MapGroup("/users");

        users.MapGet("/", async (
            IQueryHandler<GetUsersQuery, List<UserResponseDto>> handler,
            CancellationToken cancellationToken) =>
                Results.Ok(await handler.HandleAsync(new GetUsersQuery(), cancellationToken)));

        users.MapGet("/{id:int}", async (
            int id,
            IQueryHandler<GetUserByIdQuery, UserResponseDto?> handler,
            CancellationToken cancellationToken) =>
        {
            var user = await handler.HandleAsync(new GetUserByIdQuery(id), cancellationToken);
            return user is null ? Results.NotFound() : Results.Ok(user);
        });

        users.MapPost("/", async (
            CreateUserCommand command,
            IValidator<CreateUserCommand> validator,
            ICommandHandler<CreateUserCommand, CreateUserResult> handler,
            CancellationToken cancellationToken) =>
        {
            var validation = await validator.ValidateAsync(command, cancellationToken);

            if (!validation.IsValid)
            {
                return Results.ValidationProblem(validation.ToDictionary());
            }

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.EmailAlreadyExists
                ? Results.Conflict(new { message = "An account with this email already exists." })
                : Results.Created($"/users/{result.User!.Id}", result.User);
        });

        return app;
    }
}
