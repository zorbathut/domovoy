using Microsoft.EntityFrameworkCore;
using Wumpus.Shared.Models;

namespace Wumpus.Database;

public class WumpusDbContext : DbContext
{
    public WumpusDbContext(DbContextOptions<WumpusDbContext> options)
        : base(options)
    {
    }

    public DbSet<Report> Reports => Set<Report>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Error> Errors => Set<Error>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure TPT (Table Per Type) - each type gets its own table
        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            // Use TPT mapping strategy
            entity.UseTptMappingStrategy();
            entity.ToTable("Reports");

            // Indexes on common fields
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.GameVersion);
            entity.HasIndex(e => e.Platform);

            entity.Property(e => e.GameVersion)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Platform)
                .IsRequired()
                .HasMaxLength(50);
        });

        // Configure Event entity - maps to Events table
        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("Events");

            entity.OwnsOne(e => e.Data, owned =>
            {
                owned.Property(d => d.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                owned.Property(d => d.Category)
                    .HasMaxLength(100);

                owned.Property(d => d.Value)
                    .HasPrecision(18, 2);

                owned.Property(d => d.UserId)
                    .HasMaxLength(100);

                // Configure Metadata as JSONB
                owned.Property(d => d.Metadata)
                    .HasColumnType("jsonb");

                // Index for UserId (no filter needed - Events table only has events)
                owned.HasIndex(d => d.UserId);
            });
        });

        // Configure Error entity - maps to Errors table
        modelBuilder.Entity<Error>(entity =>
        {
            entity.ToTable("Errors");

            entity.OwnsOne(e => e.Data, owned =>
            {
                owned.Property(d => d.Severity)
                    .IsRequired()
                    .HasConversion<string>()
                    .HasMaxLength(20);

                owned.Property(d => d.Code)
                    .HasMaxLength(100);

                owned.Property(d => d.Message)
                    .IsRequired()
                    .HasMaxLength(2000);

                owned.Property(d => d.ExceptionType)
                    .HasMaxLength(500);

                owned.Property(d => d.StackTrace)
                    .HasColumnType("text");

                owned.Property(d => d.Context)
                    .HasColumnType("text");

                // Index for Severity (no filter needed - Errors table only has errors)
                owned.HasIndex(d => d.Severity);
            });
        });
    }
}
