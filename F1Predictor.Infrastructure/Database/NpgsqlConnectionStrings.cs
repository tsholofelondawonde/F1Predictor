using Npgsql;

namespace F1Predictor.Infrastructure.Database;

/// <summary>
/// Connection-string normalisation applied at every point where a Postgres connection string is
/// handed to Npgsql.
/// </summary>
internal static class NpgsqlConnectionStrings
{
    /// <summary>
    /// Upgrades <c>SslMode</c> to <see cref="SslMode.Require"/> when it is weaker than that.
    /// </summary>
    /// <remarks>
    /// Neon (and any managed Postgres) requires TLS. Npgsql's default <c>SslMode=Prefer</c>
    /// attempts a GSSAPI/Kerberos encryption negotiation first, which dlopens
    /// <c>libgssapi_krb5.so.2</c> — absent from the runtime image — and fails the connection.
    /// Forcing <c>Require</c> both enforces TLS and skips that probe. <c>Require</c> encrypts
    /// without validating the CA chain (Npgsql 10 semantics); an explicit
    /// <see cref="SslMode.VerifyCA"/>/<see cref="SslMode.VerifyFull"/> is stronger and left alone.
    /// </remarks>
    public static string RequireSsl(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);

        if (builder.SslMode is SslMode.Disable or SslMode.Allow or SslMode.Prefer)
        {
            builder.SslMode = SslMode.Require;
        }

        return builder.ConnectionString;
    }
}
