using Microsoft.EntityFrameworkCore;
using Domovoy.Shared.Models;

namespace Domovoy.Database;

public class DomovoyDbContext : DbContext
{
    public DomovoyDbContext(DbContextOptions<DomovoyDbContext> options)
        : base(options)
    {
    }

    public DbSet<Report> Reports => Set<Report>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Error> Errors => Set<Error>();
    public DbSet<Subscriber> Subscribers => Set<Subscriber>();
    public DbSet<Notification> Notifications => Set<Notification>();

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

            // Configure Standard payload as owned type
            entity.OwnsOne(e => e.Standard, owned =>
            {
                owned.Property(s => s.GameVersion)
                    .IsRequired()
                    .HasMaxLength(50);

                owned.Property(s => s.Platform)
                    .IsRequired()
                    .HasMaxLength(50);

                owned.Property(s => s.Environment)
                    .IsRequired()
                    .HasConversion<string>()
                    .HasMaxLength(20);

                owned.Property(s => s.UserId)
                    .IsRequired();

                owned.Property(s => s.ComputerId)
                    .IsRequired();

                owned.Property(s => s.GameId)
                    .IsRequired();

                owned.Property(s => s.SequenceId)
                    .IsRequired();

                // Indexes on standard payload fields
                owned.HasIndex(s => s.GameVersion);
                owned.HasIndex(s => s.Platform);
                owned.HasIndex(s => s.Environment);
                owned.HasIndex(s => s.UserId);
                owned.HasIndex(s => s.ComputerId);
                owned.HasIndex(s => s.GameId);
            });
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

                owned.Property(d => d.Message)
                    .IsRequired()
                    .HasMaxLength(2000);

                owned.Property(d => d.StackTrace)
                    .IsRequired()
                    .HasColumnType("text");

                owned.Property(d => d.Log)
                    .IsRequired()
                    .HasColumnType("text");

                // Index for Severity (no filter needed - Errors table only has errors)
                owned.HasIndex(d => d.Severity);
            });
        });

        // Configure Subscriber entity
        modelBuilder.Entity<Subscriber>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.ToTable("Subscribers");

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.IsActive)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.HeartbeatTimeoutMinutes)
                .IsRequired();

            entity.HasIndex(e => e.LastHeartbeat);
        });

        // Configure Notification entity
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.ToTable("Notifications");

            entity.Property(e => e.ReportId)
                .IsRequired();

            entity.Property(e => e.SubscriberId)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.RetryCount)
                .IsRequired();

            entity.Property(e => e.LockedBy)
                .HasMaxLength(100);

            // Configure relationships
            entity.HasOne(e => e.Report)
                .WithMany()
                .HasForeignKey(e => e.ReportId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Subscriber)
                .WithMany()
                .HasForeignKey(e => e.SubscriberId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes for efficient queries
            entity.HasIndex(e => new { e.SubscriberId, e.CreatedAt });
            entity.HasIndex(e => e.LockedUntil)
                .HasFilter("\"LockedUntil\" IS NOT NULL");
            entity.HasIndex(e => e.ReportId);
        });
    }
}
