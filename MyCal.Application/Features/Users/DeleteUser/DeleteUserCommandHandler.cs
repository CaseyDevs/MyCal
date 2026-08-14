using Microsoft.EntityFrameworkCore;
using MyCal.Domain.Abstractions;
using MyCal.Application.Common.Result;
using MyCal.Application.Data;

namespace MyCal.Application.Features.Users.DeleteUser;

public sealed class DeleteUserCommandHandler(
    AppDbContext context
) : ICommandHandler<DeleteUserCommand, Result>
{
    public async Task<Result> HandleAsync(
        DeleteUserCommand command, 
        CancellationToken cancellationToken)
    {
        var user = await context.Users
            .SingleOrDefaultAsync(
                user => user.Id == command.Id, 
                cancellationToken: cancellationToken);

        if (user is null)
        {
            return Result.Fail(
                "This user does not exist, or was already deleted."
            );
        }

        context.Users.Remove(user);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success("User deleted successfully.");
    }
}