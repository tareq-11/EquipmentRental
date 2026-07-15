using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace infrastructure.Persistence;

/// <summary>
/// Creates a database context for Entity Framework design-time operations.
/// </summary>
public sealed class EquipmentRentalDbContextFactory : IDesignTimeDbContextFactory<EquipmentRentalDbContext>
{
    /// <summary>
    /// Creates the context using the API user-secrets store or environment variables.
    /// </summary>
    /// <param name="args">Command-line arguments supplied by Entity Framework.</param>
    /// <returns>A configured database context.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the required connection string is unavailable.</exception>
    public EquipmentRentalDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<EquipmentRentalDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();
        var connectionString = PostgreSqlConnectionStringNormalizer.Normalize(configuration.GetConnectionString("EquipmentRental"));

        var options = new DbContextOptionsBuilder<EquipmentRentalDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new EquipmentRentalDbContext(options);
    }
}
