using Microsoft.EntityFrameworkCore;
using EngineerOfRecord.Domain;

namespace EngineerOfRecord.Persistence;

/// <summary>
/// The EF Core database context for the Engineer of Record application.
///
/// Manages two entity sets:
/// <list type="bullet">
///   <item><see cref="Employees"/> — local copy of Vantagepoint employee data, kept in sync by a background service.</item>
///   <item><see cref="Engineers"/> — EOR domain data (discipline, licensing, states), linked to Employees via FK.</item>
/// </list>
///
/// Entity configuration is inline in <see cref="OnModelCreating"/> for clarity.
/// As the project grows, these can be extracted into separate
/// <see cref="IEntityTypeConfiguration{TEntity}"/> classes.
/// </summary>
public class EorDbContext(DbContextOptions<EorDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Local copy of all active employees from Vantagepoint.
    /// Synced by the background polling service.
    /// </summary>
    public DbSet<Employee> Employees { get; set; }

    /// <summary>
    /// Engineers of Record — licensed professionals who stamp project deliverables.
    /// </summary>
    public DbSet<Domain.EngineerOfRecord> Engineers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ── Employee (synced from Vantagepoint) ──────────────────

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("Employee");

            // PK is the Vantagepoint employee ID, not an auto-generated key.
            // This is a mirror of VP data — their ID is our ID.
            entity.HasKey(e => e.VantagepointEmployeeId);

            entity.Property(e => e.VantagepointEmployeeId)
                .HasMaxLength(6);

            entity.Property(e => e.FirstName).HasMaxLength(50);
            entity.Property(e => e.LastName).HasMaxLength(50);
            entity.Property(e => e.PreferredName).HasMaxLength(50);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Title).HasMaxLength(100);

            entity.Ignore(e => e.DisplayName);
        });

        // ── EngineerOfRecord ─────────────────────────────────────

        modelBuilder.Entity<Domain.EngineerOfRecord>(entity =>
        {
            entity.ToTable("EngineerOfRecord");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedNever();

            // FK to Employee — one EOR per employee, one employee can be one EOR.
            entity.Property(e => e.VantagepointEmployeeId)
                .HasMaxLength(6)
                .IsRequired();

            entity.HasOne(e => e.Employee)
                .WithOne()
                .HasForeignKey<Domain.EngineerOfRecord>(e => e.VantagepointEmployeeId);

            entity.HasIndex(e => e.VantagepointEmployeeId)
                .IsUnique();

            // Store the enum as a string ("Electrical", "Mechanical", "Multi")
            entity.Property(e => e.Discipline)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.LicenseExpiration)
                .IsRequired();

            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // Licensed states stored as a JSON array: ["CA","NV","AZ"]
            entity.PrimitiveCollection(e => e.LicensedStates)
                .ElementType()
                .HasConversion<string>();

            entity.Ignore(e => e.IsLicenseExpired);
        });
    }
}
