using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Wumpus.Database;

public class WumpusDbContextFactory : IDesignTimeDbContextFactory<WumpusDbContext>
{
    public WumpusDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<WumpusDbContext>();

        // Default connection string for migrations
        // This will be overridden at runtime by appsettings.json
        optionsBuilder.UseNpgsql("Host=localhost;Database=wumpus;Username=wumpus;Password=wumpus");

        return new WumpusDbContext(optionsBuilder.Options);
    }
}
