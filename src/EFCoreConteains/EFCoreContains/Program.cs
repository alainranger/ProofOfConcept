using System.Diagnostics.CodeAnalysis;
using EFCore.BulkExtensions;
using EFCoreContains.Data;
using EFCoreContains.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var connectionString = config.GetConnectionString("SqlServer");

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlServer(connectionString)
    .Options;

await using var db = new AppDbContext(options);

await db.Database.EnsureDeletedAsync();
await db.Database.EnsureCreatedAsync();

var products = new List<Product>(5000);
var now = DateTime.UtcNow;

for (var i = 1; i <= 5000; i++)
{
    products.Add(new Product
    {
        Code = $"P{i:00000000}",
        Name = $"Product {i:00000000}",
        Price = Random.Shared.Next(1, 999),
        CreatedUTC = now
    });
}

db.BulkInsert(products);

var codes = Enumerable.Range(1, 2500).Select(i => $"P{i:00000000}").ToList();

await db.Database.EnsureDeletedAsync();