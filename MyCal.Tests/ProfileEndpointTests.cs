using System.Net;
using System.Net.Http.Json;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.AspNetCore.Mvc;
using MyCal.ApiService.Common.Dto.Profile;
using MyCal.ApiService.Common.Enum;

namespace MyCal.Tests;

[TestClass]
public sealed class ProfileEndpointTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
    private static DistributedApplication app = null!;
    private static HttpClient client = null!;

    [ClassInitialize]
    public static async Task InitializeAsync(TestContext _)
    {
        using var cancellationSource = new CancellationTokenSource(DefaultTimeout);
        var cancellationToken = cancellationSource.Token;

        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.MyCal_AppHost>(cancellationToken);

        appHost.Services.ConfigureHttpClientDefaults(clientBuilder => clientBuilder.AddStandardResilienceHandler());

        app = await appHost.BuildAsync(cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);
        await app.StartAsync(cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);
        await app.ResourceNotifications
            .WaitForResourceHealthyAsync("apiservice", cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        client = app.CreateHttpClient("apiservice");
    }

    [ClassCleanup]
    public static async Task CleanupAsync()
    {
        client.Dispose();
        await app.DisposeAsync();
    }

    [TestMethod]
    public async Task CreateProfile_WithValidRequest_ReturnsCreatedProfile()
    {
        var request = CreateValidRequest();

        var response = await client.PostAsJsonAsync("/profiles", request);

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        Assert.IsNotNull(response.Headers.Location);

        var profile = await response.Content.ReadFromJsonAsync<ProfileResponseDto>();
        Assert.IsNotNull(profile);
        Assert.IsTrue(profile.Id > 0);
        Assert.AreEqual(request.Name, profile.Name);
        Assert.AreEqual(request.Email.ToLowerInvariant(), profile.Email);
    }

    [TestMethod]
    public async Task CreateProfile_WithInvalidRequest_ReturnsValidationProblem()
    {
        var request = new ProfileRequestDto(
            Name: string.Empty,
            Email: "not-an-email",
            HeightInCm: 0,
            WeightInKg: 0,
            WeightGoal: 0,
            Age: 0,
            Gender: (Gender)999,
            ActivityLevel: (ActivityLevel)999);

        var response = await client.PostAsJsonAsync("/profiles", request);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.IsNotNull(problem);
        CollectionAssert.Contains(problem.Errors.Keys.ToList(), nameof(request.Name));
        CollectionAssert.Contains(problem.Errors.Keys.ToList(), nameof(request.Email));
        CollectionAssert.Contains(problem.Errors.Keys.ToList(), nameof(request.Age));
    }

    [TestMethod]
    public async Task CreateProfile_WithExistingEmail_ReturnsBadRequest()
    {
        var email = $"duplicate-{Guid.NewGuid():N}@example.com";
        var firstResponse = await client.PostAsJsonAsync("/profiles", CreateValidRequest(email));
        var secondResponse = await client.PostAsJsonAsync("/profiles", CreateValidRequest(email.ToUpperInvariant()));

        Assert.AreEqual(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, secondResponse.StatusCode);
    }

    [TestMethod]
    public async Task GetProfile_WithExistingId_ReturnsSavedProfile()
    {
        var request = CreateValidRequest();
        var createResponse = await client.PostAsJsonAsync("/profiles", request);
        var createdProfile = await createResponse.Content.ReadFromJsonAsync<ProfileResponseDto>();

        Assert.AreEqual(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.IsNotNull(createdProfile);

        var response = await client.GetAsync($"/profiles/{createdProfile.Id}");
        var profile = await response.Content.ReadFromJsonAsync<ProfileResponseDto>();

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsNotNull(profile);
        Assert.AreEqual(createdProfile.Id, profile.Id);
        Assert.AreEqual(request.Email.ToLowerInvariant(), profile.Email);
    }

    private static ProfileRequestDto CreateValidRequest(string? email = null) => new(
        Name: "Casey",
        Email: email ?? $"casey-{Guid.NewGuid():N}@example.com",
        HeightInCm: 180,
        WeightInKg: 75,
        WeightGoal: 70,
        Age: 30,
        Gender: Gender.Male,
        ActivityLevel: ActivityLevel.ModeratelyActive);
}
