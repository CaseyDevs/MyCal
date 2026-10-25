var builder = DistributedApplication.CreateBuilder(args);
var password = builder.AddParameter("postgres-password", secret: true);
var postgres = builder.AddPostgres("postgres", password: password)
    .WithHostPort(51885).WithDataVolume().WithLifetime(ContainerLifetime.Persistent);
var appDb = postgres.AddDatabase("postgresdb");
var identityDb = postgres.AddDatabase("identitydb");
builder.AddProject<Projects.MyCal_Web>("webfrontend")
    .WithExternalHttpEndpoints().WithReference(appDb).WithReference(identityDb)
    .WithEnvironment("Database__MigrateOnStartup", "true")
    .WaitFor(appDb).WaitFor(identityDb).WithHttpHealthCheck("/health");
builder.Build().Run();
