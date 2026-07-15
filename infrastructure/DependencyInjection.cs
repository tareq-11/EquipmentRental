using infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Hybrid;
using Services.Abstractions;
using Services.Foundation;
using Services.Identity;
using infrastructure.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

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
        var connectionString = PostgreSqlConnectionStringNormalizer.Normalize(configuration.GetConnectionString("EquipmentRental"));

        services.AddDbContext<EquipmentRentalDbContext>(options => options.UseNpgsql(connectionString));
        services.AddHybridCache(); // Development uses its in-process implementation; Redis is optional and never authoritative.
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IIdempotencyCoordinator, IdempotencyCoordinator>();
        services.AddScoped<IFoundationProbeStore, FoundationProbeStore>();
        services.AddOptions<JwtOptions>().Bind(configuration.GetSection(JwtOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<IdentityOptions>().Bind(configuration.GetSection(IdentityOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
        services.AddSingleton<IValidateOptions<MailOptions>, MailOptionsValidator>();
        services.AddOptions<MailOptions>().Bind(configuration.GetSection(MailOptions.SectionName)).ValidateOnStart();
        services.Configure<BootstrapOptions>(configuration.GetSection(BootstrapOptions.SectionName));
        services.AddDataProtection();
        services.AddScoped<IDataProtector>(provider => provider.GetRequiredService<IDataProtectionProvider>().CreateProtector("account-email-v1"));
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddHostedService<OutboxEmailWorker>();
        services.AddHostedService<BootstrapWorker>();
        return services;
    }
}
