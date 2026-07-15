using Npgsql;

namespace infrastructure.Persistence;

/// <summary>
/// Converts supported PostgreSQL connection URL values into Npgsql connection strings.
/// </summary>
public static class PostgreSqlConnectionStringNormalizer
{
    /// <summary>
    /// Validates an Npgsql connection string or converts a PostgreSQL URL to one.
    /// </summary>
    /// <param name="connectionString">The configured connection string value.</param>
    /// <returns>A validated Npgsql key/value connection string.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the configured value is missing or invalid.</exception>
    public static string Normalize(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:EquipmentRental must be configured.");
        }

        if (connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) ||
            connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
        {
            return NormalizeUrl(connectionString);
        }

        try
        {
            return new NpgsqlConnectionStringBuilder(connectionString).ConnectionString;
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException("ConnectionStrings:EquipmentRental must be a valid Npgsql key/value connection string or PostgreSQL URL.", exception);
        }
    }

    private static string NormalizeUrl(string connectionUrl)
    {
        if (!Uri.TryCreate(connectionUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme is not "postgresql" and not "postgres") ||
            string.IsNullOrWhiteSpace(uri.Host) ||
            uri.UserInfo.Length == 0 ||
            uri.AbsolutePath is "" or "/" ||
            uri.Fragment.Length != 0)
        {
            throw InvalidUrl();
        }

        var credentials = uri.GetComponents(UriComponents.UserInfo, UriFormat.Unescaped).Split(':', 2);
        if (string.IsNullOrWhiteSpace(credentials[0]))
        {
            throw InvalidUrl();
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Database = uri.GetComponents(UriComponents.Path, UriFormat.Unescaped).TrimStart('/'),
            Username = credentials[0]
        };

        if (credentials.Length == 2)
        {
            builder.Password = credentials[1];
        }

        if (!uri.IsDefaultPort)
        {
            builder.Port = uri.Port;
        }

        var configuredOptions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var queryPart in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = queryPart.Split('=', 2);
            var key = Uri.UnescapeDataString(parts[0]);
            var value = parts.Length == 2 ? Uri.UnescapeDataString(parts[1]) : string.Empty;

            if (!configuredOptions.Add(key))
            {
                throw InvalidUrl();
            }

            try
            {
                switch (key.ToLowerInvariant())
                {
                    case "sslmode":
                        builder["SSL Mode"] = value;
                        break;
                    case "channel_binding":
                        builder["Channel Binding"] = value;
                        break;
                    default:
                        throw InvalidUrl();
                }
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException("ConnectionStrings:EquipmentRental contains an invalid PostgreSQL URL option.", exception);
            }
        }

        return builder.ConnectionString;
    }

    private static InvalidOperationException InvalidUrl() => new("ConnectionStrings:EquipmentRental must be a valid PostgreSQL URL with host, database, user credentials, and only supported sslmode or channel_binding options.");
}
