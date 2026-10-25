using Microsoft.AspNetCore.Identity;

namespace MyCal.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public bool IsAdmin
    {
        get; set;
    }
}
