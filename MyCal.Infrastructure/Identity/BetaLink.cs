namespace MyCal.Infrastructure.Identity;

public sealed class BetaLink
{
    public int Id
    {
        get; set;
    }
    public string TokenHash { get; set; } = "";
    public string Email { get; set; } = "";
    public string? UserId
    {
        get; set;
    }
    public DateTime ExpiresAt
    {
        get; set;
    }
    public DateTime? UsedAt
    {
        get; set;
    }
}
