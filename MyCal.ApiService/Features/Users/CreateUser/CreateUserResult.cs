using MyCal.ApiService.Features.Users;

namespace MyCal.ApiService.Features.Users.CreateUser;

// The handler returns an application result, not an HTTP response.
public sealed record CreateUserResult(UserResponseDto? User, bool EmailAlreadyExists)
{
    public static CreateUserResult Success(UserResponseDto user) => new(user, false);

    public static CreateUserResult DuplicateEmail() => new(null, true);
}
