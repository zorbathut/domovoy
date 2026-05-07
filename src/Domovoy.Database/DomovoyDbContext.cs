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
    public DbSet<Attachment> Attachments => Set<Attachment>();

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

            // Common fields
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.GeneratedAt);

            entity.Property(e => e.Version)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Platform)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Environment)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.ComputerId).IsRequired();
            entity.Property(e => e.CampaignId).IsRequired();
            entity.Property(e => e.CampaignSequenceIds).IsRequired();
            entity.Property(e => e.ProcessId).IsRequired();

            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb");

            entity.HasIndex(e => e.Version);
            entity.HasIndex(e => e.Platform);
            entity.HasIndex(e => e.Environment);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ComputerId);
            entity.HasIndex(e => e.CampaignId);
        });

        // Configure Event entity - maps to Events table
        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("Events");

            entity.Property(e => e.Category)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Data)
                .HasColumnType("jsonb");
        });

        // Configure Error entity - maps to Errors table
        modelBuilder.Entity<Error>(entity =>
        {
            entity.ToTable("Errors");

            entity.Property(e => e.Severity)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(e => e.Message)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(e => e.StackTrace)
                .IsRequired()
                .HasColumnType("text");

            entity.Property(e => e.Log)
                .IsRequired()
                .HasColumnType("text");

            entity.HasIndex(e => e.Severity);
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

        // Configure Attachment entity
        modelBuilder.Entity<Attachment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.ToTable("Attachments");

            entity.Property(e => e.Filename)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.ContentType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.SizeBytes)
                .IsRequired();

            entity.Property(e => e.StorageKey)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.HasOne(e => e.Report)
                .WithMany()
                .HasForeignKey(e => e.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ReportId);
        });
    }
}
