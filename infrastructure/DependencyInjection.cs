using infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Hybrid;
using Services.Abstractions;
using Services.Foundation;

namespace infrastructure;

/// <summary>
/// Registers infrastructure services used by the API host.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds PostgreSQL persistence services.
    /// </summary>
    /// <param name="services">The application service collection.</param>
    /// <param name="configuration">The host configuration.</param>
    /// <returns>The configured service collection.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the database connection string is unavailable.</exception>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("EquipmentRental");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:EquipmentRental must be configured.");
        }

        services.AddDbContext<EquipmentRentalDbContext>(options => options.UseNpgsql(connectionString));
        services.AddHybridCache(); // Development uses its in-process implementation; Redis is optional and never authoritative.
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IIdempotencyCoordinator, IdempotencyCoordinator>();
        services.AddScoped<IFoundationProbeStore, FoundationProbeStore>();
        return services;
    }
}
