namespace EFCoreContains.Model;

public sealed class Product
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedUTC { get; set; }
}