using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MyCal.ApiService.Data;
using MyCal.ApiService.Endpoints;
using MyCal.ApiService.Features.Users.CreateUser;
using MyCal.ApiService.Features.Users.GetUserById;
using MyCal.ApiService.Features.Users.GetUsers;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserRequestValidator>();
builder.Services.AddScoped<CreateUserHandler>();
builder.Services.AddScoped<GetUserByIdHandler>();
builder.Services.AddScoped<GetUsersHandler>();


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
app.MapUserEndpoints();

app.MapDefaultEndpoints();

app.Run();
