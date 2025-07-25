using WebApiPdfReporting.Models;

internal interface IInvoiceToPdf
{
    Task<byte[]> GeneratePdfAsync(Invoice invoice);
}