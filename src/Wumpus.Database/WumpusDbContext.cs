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

        // Configure TPH (Table Per Hierarchy) with discriminator
        modelBuilder.Entity<Report>()
            .HasDiscriminator<ReportType>(r => r.ReportType)
            .HasValue<Event>(ReportType.Event)
            .HasValue<Error>(ReportType.Error);

        // Configure base Report entity
        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            // Indexes on common fields
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.ReportType);
            entity.HasIndex(e => new { e.ReportType, e.Timestamp });
            entity.HasIndex(e => e.GameVersion);
            entity.HasIndex(e => e.Platform);

            entity.Property(e => e.GameVersion)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Platform)
                .IsRequired()
                .HasMaxLength(50);
        });

        // Configure Event entity with owned EventData
        modelBuilder.Entity<Event>()
            .OwnsOne(e => e.Data, owned =>
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

                // Partial index for UserId (only for Event type)
                owned.HasIndex(d => d.UserId)
                    .HasFilter("\"ReportType\" = 1"); // ReportType.Event = 1
            });

        // Configure Error entity with owned ErrorData
        modelBuilder.Entity<Error>()
            .OwnsOne(e => e.Data, owned =>
            {
                owned.Property(d => d.Severity)
                    .IsRequired()
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

                // Partial index for Severity (only for Error type)
                owned.HasIndex(d => d.Severity)
                    .HasFilter("\"ReportType\" = 2"); // ReportType.Error = 2
            });
    }
}
