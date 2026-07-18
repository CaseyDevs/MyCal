using FluentValidation;
using MyCal.ApiService.Abstractions;
using MyCal.ApiService.Common.Result;
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
            ICommandHandler<CreateUserCommand, Result<UserResponseDto>> handler,
            CancellationToken cancellationToken) =>
        {
            var validation = await validator.ValidateAsync(command, cancellationToken);

            if (!validation.IsValid)
            {
                return Results.ValidationProblem(validation.ToDictionary());
            }

            var result = await handler.HandleAsync(command, cancellationToken);

            if (!result.IsSuccess)
            {
                return result.ErrorCode switch
                {
                    "EmailAlreadyExists" => Results.Conflict(new
                    {
                        messsage = result.ErrorMessage
                    }),

                    _ => Results.BadRequest(new
                    {
                        message = result.ErrorMessage
                    })
                };
            }

            return Results.Created($"/users/{result.Data!.Id}", result.Data);
        });

        return app;
    }
}