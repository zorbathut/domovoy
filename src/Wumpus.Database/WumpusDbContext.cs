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
            entity.HasIndex(e => e.StackTraceHash);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.Platform);
            entity.HasIndex(e => e.GameVersion);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.GameVersion)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Platform)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.ExceptionType)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.ExceptionMessage)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(e => e.StackTrace)
                .IsRequired();

            entity.Property(e => e.StackTraceHash)
                .IsRequired()
                .HasMaxLength(64);

            entity.Property(e => e.SystemInfo)
                .HasColumnType("jsonb");

            entity.Property(e => e.UserContext)
                .HasColumnType("jsonb");
        });
    }
}
