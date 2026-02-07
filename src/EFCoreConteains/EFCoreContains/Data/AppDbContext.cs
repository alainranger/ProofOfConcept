using EFCoreContains.Model;
using Microsoft.EntityFrameworkCore;

namespace EFCoreContains.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var p = modelBuilder.Entity<Product>();

        p.ToTable("Products");
        p.HasKey(x => x.Id);
        p.Property(x => x.Code).HasMaxLength(12).IsRequired();
        p.Property(x => x.Name).HasMaxLength(200).IsRequired();
        p.Property(x => x.Price).HasColumnType("decimal(18,2)");
        p.Property(x => x.CreatedUTC).HasColumnType("datetime2");

        p.HasIndex(x => x.Code).IsUnique();
    }
}