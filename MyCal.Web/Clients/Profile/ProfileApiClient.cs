using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace MyCal.Web.Clients.Profile;


public sealed class ProfileApiClient(
    HttpClient http,
    AuthenticationStateProvider authenticationStateProvider)
    : IProfileApiClient
{

    public async Task<UserProfileResponse> CreateUserProfileAsync(UserProfileRequest profile, CancellationToken cancellationToken)
    {
        var request = new CreateUserProfileApiRequest(
            IdentityUserId: await GetIdentityUserIdAsync(),
            Name: profile.Name,
            Email: profile.Email,
            HeightInCm: profile.HeightInCm,
            WeightInKg: profile.WeightInKg,
            WeightGoal: profile.WeightGoal,
            Age: profile.Age,
            Gender: profile.Gender,
            ActivityLevel: profile.ActivityLevel);

        var response = await http.PostAsJsonAsync(
            "/users",
            request,
            cancellationToken
        );

        response.EnsureSuccessStatusCode();

        var userProfile = await response.Content.ReadFromJsonAsync<UserProfileResponse>(
        cancellationToken);

        return userProfile
            ?? throw new InvalidOperationException("Api returned an empty profile response.");
    }

    public async Task<UserProfileResponse?> GetUserProfileAsync(
        CancellationToken cancellationToken)
    {
        var identityUserId = await GetIdentityUserIdAsync();

        var response = await http.GetAsync(
            $"/profiles/me?identityUserId={Uri.EscapeDataString(identityUserId)}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<UserProfileResponse>(
            cancellationToken);
    }

    private async Task<string> GetIdentityUserIdAsync()
    {
        var authenticationState =
            await authenticationStateProvider.GetAuthenticationStateAsync();

        return authenticationState.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException(
                "The current user does not have an identity id claim.");
    }

    private sealed record CreateUserProfileApiRequest(
        string IdentityUserId,
        string Name,
        string Email,
        double HeightInCm,
        double WeightInKg,
        double WeightGoal,
        int Age,
        Gender Gender,
        ActivityLevel ActivityLevel);
}
