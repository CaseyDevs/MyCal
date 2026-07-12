using MyCal.ApiService.Data;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();


builder.AddNpgsqlDbContext<AppDbContext>("postgresdb");

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.MapGet("/", () => "API service is running.");

app.MapDefaultEndpoints();

app.Run();
