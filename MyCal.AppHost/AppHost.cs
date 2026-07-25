var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");
var postgresPassword = builder.AddParameter(
    "postgres-password",
    secret: true
);
var postgres = builder
    .AddPostgres(name: "postgres", password: postgresPassword)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgAdmin();

var postgresdb = postgres.AddDatabase("postgresdb");
var identitydb = postgres.AddDatabase("identitydb");

var apiService = builder.AddProject<Projects.MyCal_ApiService>("apiservice")
    .WithReference(postgresdb)
    .WaitFor(postgresdb)
    .WithHttpHealthCheck("/health");

var webFrontend = builder.AddProject<Projects.MyCal_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(identitydb)
    .WaitFor(identitydb)
    .WithReference(apiService)
    .WaitFor(apiService);
  
builder.Build().Run();
