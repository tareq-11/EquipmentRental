using Microsoft.EntityFrameworkCore;

namespace infrastructure.Persistence;

/// <summary>
/// Represents the persistence boundary for the Equipment Rental application.
/// Domain entities are introduced in subsequent milestones.
/// </summary>
public sealed class EquipmentRentalDbContext(DbContextOptions<EquipmentRentalDbContext> options) : DbContext(options)
{
}
