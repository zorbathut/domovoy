using Microsoft.EntityFrameworkCore;
using Wumpus.Shared.Models;
using System.Text.Json;

namespace Wumpus.Database;

public class WumpusDbContext : DbContext
{
    public WumpusDbContext(DbContextOptions<WumpusDbContext> options)
        : base(options)
    {
    }

    public DbSet<CrashReport> CrashReports => Set<CrashReport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CrashReport>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Timestamp);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            // Configure Core as owned entity (value object)
            entity.OwnsOne(e => e.Core, ownedBuilder =>
            {
                ownedBuilder.Property(c => c.GameVersion)
                    .IsRequired()
                    .HasMaxLength(50);

                ownedBuilder.Property(c => c.Platform)
                    .IsRequired()
                    .HasMaxLength(50);

                ownedBuilder.Property(c => c.ExceptionType)
                    .IsRequired()
                    .HasMaxLength(500);

                ownedBuilder.Property(c => c.ExceptionMessage)
                    .IsRequired()
                    .HasMaxLength(2000);

                ownedBuilder.Property(c => c.StackTrace)
                    .IsRequired();

                // Create indexes on owned entity properties
                ownedBuilder.HasIndex(c => c.Platform);
                ownedBuilder.HasIndex(c => c.GameVersion);
            });
        });
    }
}
