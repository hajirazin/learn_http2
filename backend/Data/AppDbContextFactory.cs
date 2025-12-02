using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Http2Streaming.Api.Data;

/// <summary>
/// Design-time factory for creating DbContext instances during migrations.
/// This allows EF Core tools to create the DbContext without running the application.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        
        // Default connection string for design-time (migrations)
        // This will be overridden by appsettings.json at runtime
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=http2streaming;Username=postgres;Password=postgres");

        return new AppDbContext(optionsBuilder.Options);
    }
}

