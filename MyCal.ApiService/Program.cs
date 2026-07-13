using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MyCal.ApiService.Data;
using MyCal.ApiService.Endpoints;
using MyCal.ApiService.Validator;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddValidatorsFromAssemblyContaining<ProfileRequestValidator>();


builder.AddNpgsqlDbContext<AppDbContext>("postgresdb");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.MapGet("/", () => "API service is running.");
app.MapProfileEndpoints();

app.MapDefaultEndpoints();

app.Run();
