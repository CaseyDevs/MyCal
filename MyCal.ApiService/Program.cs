using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MyCal.ApiService.Abstractions;
using MyCal.ApiService.Data;
using MyCal.ApiService.Endpoints;
using MyCal.ApiService.Features.Users;
using MyCal.ApiService.Features.Users.CreateUser;
using MyCal.ApiService.Features.Users.GetUserById;
using MyCal.ApiService.Features.Users.GetUsers;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserCommandValidator>();

builder.Services.AddScoped<
    ICommandHandler<CreateUserCommand, CreateUserResult>, 
    CreateUserHandler>();

builder.Services.AddScoped<
    IQueryHandler<GetUserByIdQuery, UserResponseDto?>,
    GetUserByIdHandler>();

builder.Services.AddScoped<
    IQueryHandler<GetUsersQuery, List<UserResponseDto>>,
    GetUsersHandler>();


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
