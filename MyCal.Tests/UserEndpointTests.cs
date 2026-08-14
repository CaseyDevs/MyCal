using System.Net;
using System.Net.Http.Json;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.AspNetCore.Mvc;
using MyCal.Domain.Enum;
using MyCal.Application.Features.Users;
using MyCal.Application.Features.Users.CreateUser;

namespace MyCal.Tests;

[TestClass]
public sealed class UserEndpointTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
    private static DistributedApplication app = null!;
    private static HttpClient client = null!;

    [ClassInitialize]
    public static async Task InitializeAsync(TestContext _)
    {
        using var cancellationSource = new CancellationTokenSource(DefaultTimeout);
        var cancellationToken = cancellationSource.Token;
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.MyCal_AppHost>(cancellationToken);

        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
            clientBuilder.AddStandardResilienceHandler());

        app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await app.ResourceNotifications
            .WaitForResourceHealthyAsync("apiservice", cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);
        client = app.CreateHttpClient("apiservice");
    }

    [ClassCleanup]
    public static async Task CleanupAsync()
    {
        client?.Dispose();

        if (app is not null)
        {
            await app.DisposeAsync();
        }
    }

    [TestMethod]
    public async Task CreateUser_WithValidRequest_ReturnsCreatedUser()
    {
        var request = CreateValidRequest();
        var response = await client.PostAsJsonAsync("/users", request);

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        Assert.IsNotNull(response.Headers.Location);

        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.IsNotNull(user);
        Assert.IsTrue(user.Id > 0);
        Assert.AreEqual(request.Name, user.Name);
        Assert.AreEqual(request.Email.ToLowerInvariant(), user.Email);
    }

    [TestMethod]
    public async Task CreateUser_WithInvalidRequest_ReturnsValidationProblem()
    {
        var request = new CreateUserCommand(
            "", "", "not-an-email", 0, 0, 0, 0,
            (Gender)999, (ActivityLevel)999);
        var response = await client.PostAsJsonAsync("/users", request);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.IsNotNull(problem);
        CollectionAssert.Contains(problem.Errors.Keys.ToList(), nameof(request.Name));
        CollectionAssert.Contains(problem.Errors.Keys.ToList(), nameof(request.Email));
        CollectionAssert.Contains(problem.Errors.Keys.ToList(), nameof(request.Age));
    }

    [TestMethod]
    public async Task CreateUser_WithExistingIdentityId_ReturnsConflict()
    {
        var identityUserId = Guid.NewGuid().ToString();
        var firstResponse = await client.PostAsJsonAsync(
            "/users",
            CreateValidRequest(identityUserId: identityUserId));
        var secondResponse = await client.PostAsJsonAsync(
            "/users",
            CreateValidRequest(identityUserId: identityUserId));

        Assert.AreEqual(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.AreEqual(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [TestMethod]
    public async Task GetUser_WithExistingId_ReturnsSavedUser()
    {
        var request = CreateValidRequest();
        var createResponse = await client.PostAsJsonAsync("/users", request);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserResponse>();

        Assert.AreEqual(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.IsNotNull(createdUser);

        var response = await client.GetAsync($"/users/{createdUser.Id}");
        var user = await response.Content.ReadFromJsonAsync<UserResponse>();

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsNotNull(user);
        Assert.AreEqual(createdUser.Id, user.Id);
        Assert.AreEqual(request.Email.ToLowerInvariant(), user.Email);
    }

    private static CreateUserCommand CreateValidRequest(
        string? email = null,
        string? identityUserId = null) => new(
        identityUserId ?? Guid.NewGuid().ToString(),
        "Casey",
        email ?? $"casey-{Guid.NewGuid():N}@example.com",
        180,
        75,
        70,
        30,
        Gender.Male, ActivityLevel.ModeratelyActive);
}
