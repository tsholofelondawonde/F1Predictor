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

var webApi = builder.AddProject<Projects.F1Predictor_WebApi>("F1Predictor-webapi")
    .WithReference(database)
    .WithHttpEndpoint(port: 5286, name: "http");

builder.AddJavaScriptApp("f1predictor-web", "../F1Predictor.Web")
    .WithHttpEndpoint(port: 3000, env: "PORT")
    .WithEnvironment("NEXT_PUBLIC_API_URL", webApi.GetEndpoint("http"))
    .WithExternalHttpEndpoints();

await builder.Build().RunAsync();
