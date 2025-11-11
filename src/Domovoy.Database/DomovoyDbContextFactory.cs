using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Domovoy.Database;

public class DomovoyDbContextFactory : IDesignTimeDbContextFactory<DomovoyDbContext>
{
    public DomovoyDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DomovoyDbContext>();

        // Default connection string for migrations
        // This will be overridden at runtime by appsettings.json
        optionsBuilder.UseNpgsql("Host=localhost;Database=domovoy;Username=domovoy;Password=domovoy");

        return new DomovoyDbContext(optionsBuilder.Options);
    }
}
