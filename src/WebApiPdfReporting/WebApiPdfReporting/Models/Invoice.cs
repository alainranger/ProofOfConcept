namespace WebApiPdfReporting.Models;

internal class Invoice
{
    public string Number { get; set; } = string.Empty;
    public DateOnly IssuedDate { get; set; }
    public DateOnly DueDate { get; set; }
    public Address? SellerAddress { get; set; }
    public Address? CustomerAddress { get; set; }
    public List<LineItem> LineItems { get; set; } = [];
}
