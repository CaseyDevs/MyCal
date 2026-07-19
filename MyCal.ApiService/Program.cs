using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MyCal.ApiService.Abstractions;
using MyCal.ApiService.Common.Result;
using MyCal.ApiService.Data;
using MyCal.ApiService.Endpoints;
using MyCal.ApiService.Features.Foods;
using MyCal.ApiService.Features.Users;
using MyCal.ApiService.Features.Users.CreateUser;
using MyCal.ApiService.Features.Users.DeleteUser;
using MyCal.ApiService.Features.Users.GetUserById;
using MyCal.ApiService.Features.Users.GetUsers;
using MyCal.ApiService.Features.Users.UpdateUser;
using MyCal.ApiService.Integrations;
using MyCal.ApiService.Integrations.USDA;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserCommandValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<UpdateUserCommandValidator>();

builder.Services.AddHttpClient<IFoodCatalogClient, USDAClient>(
    client =>
    {
        client.BaseAddress= new Uri("https://api.nal.usda.gov/fdc/v1/");
    });

builder.Services.AddScoped<
    ICommandHandler<CreateUserCommand, Result<UserResponse>>, 
    CreateUserHandler>();

builder.Services.AddScoped<
    IQueryHandler<GetUserByIdQuery, UserResponse?>,
    GetUserByIdHandler>();

builder.Services.AddScoped<
    IQueryHandler<GetUsersQuery, List<UserResponse>>,
    GetUsersHandler>();

builder.Services.AddScoped<
    IQueryHandler<SearchFoodByTextQuery, IReadOnlyList<FoodSearchResult>>, 
    SearchFoodByTextHandler>();

builder.Services.AddScoped<
    ICommandHandler<UpdateUserCommand, Result<UserResponse>>,
    UpdateUserCommandHandler>();

builder.Services.AddScoped<
    ICommandHandler<DeleteUserCommand, Result>,
    DeleteUserCommandHandler>();


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
app.MapFoodEndpoints();

app.MapDefaultEndpoints();

app.Run();
