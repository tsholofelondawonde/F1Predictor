using Microsoft.Extensions.Configuration;

namespace F1Predictor.Infrastructure.Database;

internal enum DatabaseProvider
{
    PostgreSql
}

internal static class DatabaseProviderResolver
{
    public static DatabaseProvider Resolve(IConfiguration configuration) =>
        DatabaseProvider.PostgreSql;
}