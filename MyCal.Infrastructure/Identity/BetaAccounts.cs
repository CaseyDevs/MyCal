using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace MyCal.Infrastructure.Identity;

public sealed class BetaAccounts(ApplicationDbContext db, UserManager<ApplicationUser> users, TimeProvider clock)
{
    public async Task<string> IssueAsync(string actorId, string email, bool recovery, CancellationToken ct = default)
    {
        var actor = await users.FindByIdAsync(actorId);
        if (actor?.IsAdmin != true)
            throw new UnauthorizedAccessException("Administrator access is required.");
        email = email.Trim();
        if (email.Length > 256 || !new EmailAddressAttribute().IsValid(email))
            throw new ValidationException("Enter a valid email address.");
        var recipient = await users.FindByEmailAsync(email);
        if (recovery && recipient is null)
            throw new ValidationException("No account exists for that email.");
        if (!recovery && recipient is not null)
            throw new ValidationException("That account already exists. Use a recovery link instead.");
        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        db.BetaLinks.Add(new BetaLink
        {
            TokenHash = Hash(token),
            Email = email,
            UserId = recipient?.Id,
            ExpiresAt = clock.GetUtcNow().UtcDateTime.Add(recovery ? TimeSpan.FromMinutes(30) : TimeSpan.FromHours(72))
        });
        await db.SaveChangesAsync(ct);
        return token;
    }

    public async Task RedeemAsync(string token, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 100)
            throw new ValidationException("This link is invalid or expired.");
        await using var transaction = db.Database.IsNpgsql() ? await db.Database.BeginTransactionAsync(ct) : null;
        var hash = Hash(token);
        // The row lock makes single use atomic even when two requests redeem together.
        var link = db.Database.IsNpgsql()
            ? await db.BetaLinks.FromSqlInterpolated($"SELECT * FROM \"BetaLinks\" WHERE \"TokenHash\" = {hash} FOR UPDATE").SingleOrDefaultAsync(ct)
            : await db.BetaLinks.SingleOrDefaultAsync(x => x.TokenHash == hash, ct);
        if (link is null || link.UsedAt is not null || link.ExpiresAt <= clock.GetUtcNow().UtcDateTime)
            throw new ValidationException("This link is invalid, expired, or has already been used.");
        IdentityResult result;
        if (link.UserId is null)
        {
            var user = new ApplicationUser { UserName = link.Email, Email = link.Email, EmailConfirmed = true };
            result = await users.CreateAsync(user, password);
        }
        else
        {
            var user = await users.FindByIdAsync(link.UserId) ?? throw new ValidationException("This account is no longer available.");
            var reset = await users.GeneratePasswordResetTokenAsync(user);
            result = await users.ResetPasswordAsync(user, reset, password);
            if (result.Succeeded && !user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                result = await users.UpdateAsync(user);
            }
        }
        if (!result.Succeeded)
            throw new ValidationException(string.Join(" ", result.Errors.Select(x => x.Description)));
        link.UsedAt = clock.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);
        if (transaction is not null)
            await transaction.CommitAsync(ct);
    }

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
