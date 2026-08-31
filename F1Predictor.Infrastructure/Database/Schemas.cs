namespace F1Predictor.Infrastructure.Database;

internal static class Schemas
{
    public const string Default = "public";

    public static string GetDefaultSchema(string? providerName) => Default;
}