using Http2Streaming.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Http2Streaming.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<RecordEntity> Records => Set<RecordEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<RecordEntity>(entity =>
        {
            entity.ToTable("Records");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever(); // We'll set IDs manually for seeding
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Value).HasPrecision(18, 2);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            // Index on Id for efficient ordering (it's also the primary key)
            entity.HasIndex(e => e.Id);
        });
    }
}

