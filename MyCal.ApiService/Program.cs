using Microsoft.EntityFrameworkCore;
using MyCal.Application;
using MyCal.Application.Data;
using MyCal.Application.Integrations;
using MyCal.ApiService.Endpoints;
using MyCal.ApiService.Integrations.USDA;
using MyCal.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddApplication();

builder.Services.AddHttpClient<IFoodCatalogClient, UsdaFoodCatalogClient>(client =>
{
    client.BaseAddress = new Uri("https://api.nal.usda.gov/fdc/v1/");
});

builder.AddNpgsqlDbContext<AppDbContext>(
    "postgresdb",
    configureDbContextOptions: options => options.UseNpgsql(npgsql =>
        npgsql.MigrationsAssembly(typeof(InfrastructureAssemblyMaker).Assembly
            .FullName))); // save migrations to infrastructure assembly

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "API service is running.");
app.MapUserEndpoints();
app.MapFoodEndpoints();
app.MapProfileEndpoints();
app.MapFoodLogEndpoints();

app.MapDefaultEndpoints();

app.Run();