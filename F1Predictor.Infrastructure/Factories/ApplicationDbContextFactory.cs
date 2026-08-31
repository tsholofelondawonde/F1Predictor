using F1Predictor.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace F1Predictor.Infrastructure.Factories;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Determine the environment (default to Development for design-time operations)
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        // Set the base path to the Web API project directory
        // Navigate up from Infrastructure project to the root, then to WebApi project
        var currentDirectory = Directory.GetCurrentDirectory();
        var basePath = Path.Combine(currentDirectory, "F1Predictor.WebApi");

        // If that doesn't exist, assume we're already in the WebApi directory or root
        if (!Directory.Exists(basePath))
        {
            basePath = currentDirectory;
        }

        // Build configuration with environment-specific overrides
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        // Same resolution order the runtime uses, so design-time and runtime cannot disagree
        // about which database they mean.
        var connectionString = configuration.GetConnectionString("ProdDb")
            ?? configuration.GetConnectionString("LocalDb")
            ?? configuration.GetConnectionString("Database");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // "migrations add" only needs the model, never a live server, so a placeholder is
            // enough to let scaffolding proceed on a machine with no connection configured.
            // Anything that does need to connect ("database update", "dbcontext info") will
            // fail loudly against this, which is the correct outcome.
            // Deliberately carries no credentials, so it cannot accidentally connect anywhere.
            connectionString = "Host=localhost;Database=F1Predictor";
        }

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        // Same SSL normalisation the runtime applies (see NpgsqlConnectionStrings.RequireSsl):
        // harmless against the credential-less placeholder above, which never connects.
        optionsBuilder.UseNpgsql(NpgsqlConnectionStrings.RequireSsl(connectionString));

        // Log the configuration details for debugging
        // Pass null for domainEventsDispatcher since it's not available at design time
        return new ApplicationDbContext(optionsBuilder.Options, null);
    }
}

