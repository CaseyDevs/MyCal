using Microsoft.AspNetCore.Components.Authorization;
using MyCal.Web.Clients.Food;
using MyCal.Web.Clients.FoodLog;
using MyCal.Web.Clients.Profile;
using MyCal.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.AddRedisOutputCache("cache");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddHttpClient<IProfileApiClient, ProfileApiClient>(client =>
{
    client.BaseAddress = new Uri("https+http://apiservice");
});

builder.Services.AddHttpClient<IFoodApiClient, FoodApiClient>(client =>
{
    client.BaseAddress = new Uri("https+http://apiservice");
});

builder.Services.AddHttpClient<IFoodLogClient, FoodLogClient>(client =>
{
    client.BaseAddress = new Uri("https+http://apiservice");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}


app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
