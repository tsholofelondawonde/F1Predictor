var builder = DistributedApplication.CreateBuilder(args);

// Postgres lives on Neon rather than in a container: this machine has no container runtime,
// and a hosted database survives restarts of the app host, which matters when the data was
// migrated in rather than re-fetched.
//
// The value comes from AppHost configuration, and belongs in user secrets rather than
// appsettings.json — it carries a password:
//   dotnet user-secrets set "ConnectionStrings:LocalDb" "<neon connection string>" --project F1Predictor.AppHost
//
// "LocalDb" is the key the Infrastructure layer already resolves for development.
var database = builder.AddConnectionString("LocalDb");

builder.AddProject<Projects.F1Predictor_WebApi>("F1Predictor-webapi")
    .WithReference(database);

await builder.Build().RunAsync();
