using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace infrastructure.Persistence;

/// <summary>
/// Creates a database context for Entity Framework design-time operations.
/// </summary>
public sealed class EquipmentRentalDbContextFactory : IDesignTimeDbContextFactory<EquipmentRentalDbContext>
{
    /// <summary>
    /// Creates the context using the connection string supplied through the environment.
    /// </summary>
    /// <param name="args">Command-line arguments supplied by Entity Framework.</param>
    /// <returns>A configured database context.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the required connection string is unavailable.</exception>
    public EquipmentRentalDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__EquipmentRental");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Set ConnectionStrings__EquipmentRental before running Entity Framework database commands.");
        }

        var options = new DbContextOptionsBuilder<EquipmentRentalDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new EquipmentRentalDbContext(options);
    }
}
