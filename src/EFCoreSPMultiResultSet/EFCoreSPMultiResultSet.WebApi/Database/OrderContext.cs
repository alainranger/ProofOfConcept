using EFCoreSPMultiResultSet.WebApi.Model;
using Microsoft.EntityFrameworkCore;

public class OrderContext(DbContextOptions<OrderContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // modelBuilder.Entity<Order>()
        //     .HasKey(o => o.OrderID);

        // modelBuilder.Entity<OrderItem>()
        //     .HasKey(oi => oi.OrderItemID);

        // modelBuilder.Entity<Order>()
        //     .HasMany(o => o.OrderItems)
        //     .WithOne(oi => oi.Order)
        //     .HasForeignKey(oi => oi.OrderID);
    }
}