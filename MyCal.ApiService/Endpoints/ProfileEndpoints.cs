using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MyCal.ApiService.Common.Dto.Profile;
using MyCal.ApiService.Common.Enum;
using MyCal.ApiService.Common.Model;
using MyCal.ApiService.Data;

namespace MyCal.ApiService.Endpoints;

public static class ProfileEndpoints
{
    public static WebApplication MapProfileEndpoints(this WebApplication app)
    {
        var profiles = app.MapGroup("/profiles");

        profiles.MapGet("/", async (AppDbContext context) =>
            await context.Users
                .Select(user => new ProfileResponseDto(
                    user.Id,
                    user.Name,
                    user.Email,
                    user.HeightInCm,
                    user.WeightInKg,
                    user.WeightGoal,
                    user.Age,
                    user.Gender,
                    user.ActivityLevel,
                    user.CreatedAt))
                .ToListAsync());

        profiles.MapGet("/{id:int}", async (int id, AppDbContext context) =>
        {
            var user = await context.Users.FindAsync(id);

            return user is null
                ? Results.NotFound()
                : Results.Ok(ToResponse(user));
        });

        profiles.MapPost("/", async (
            ProfileRequestDto request,
            IValidator<ProfileRequestDto> validator,
            AppDbContext context) =>
        {
            var validation = await validator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                return Results.ValidationProblem(validation.ToDictionary());
            }

            // email validation
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var emailIsTaken = await context.Users
                .AnyAsync(user => user.Email == normalizedEmail);

            if (emailIsTaken)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(request.Email)] = ["An account with this email already exists."]
                });
            }

            var user = new User
            {
                Name = request.Name.Trim(),
                Email = normalizedEmail,
                HeightInCm = request.HeightInCm,
                WeightInKg = request.WeightInKg,
                WeightGoal = request.WeightGoal,
                Age = request.Age,
                Gender = request.Gender,
                ActivityLevel = request.ActivityLevel
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            return Results.Created($"/profiles/{user.Id}", ToResponse(user));
        });

        return app;
    }

    private static ProfileResponseDto ToResponse(User user) => new(
        user.Id,
        user.Name,
        user.Email,
        user.HeightInCm,
        user.WeightInKg,
        user.WeightGoal,
        user.Age,
        user.Gender,
        user.ActivityLevel,
        user.CreatedAt);
}
