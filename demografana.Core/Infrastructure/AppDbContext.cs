using Microsoft.EntityFrameworkCore;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<OrderEventRecord> OrderEvents => Set<OrderEventRecord>();
    public DbSet<OrderProjection> Orders => Set<OrderProjection>(); // Read Model

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderEventRecord>(e =>
        {
            e.ToTable("order_events");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.AggregateId, x.Version }).IsUnique();
        });

        modelBuilder.Entity<OrderProjection>(o =>
        {
            o.ToTable("orders");
            o.HasKey(x => x.Id);
            o.OwnsMany(x => x.Items, items => items.ToJson());
        });
    }
}
