using HandlebarsDotNet;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using WebApiPdfReporting.Models;

internal class InvoiceToPdfPuppeteer : IInvoiceToPdf
{
    public async Task<byte[]> GeneratePdfAsync(Invoice invoice)
    {
        var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Views", "SampleTemplate.hbs");
        var templateContent = await File.ReadAllTextAsync(templatePath);

        var template = Handlebars.Compile(templateContent);

        var date = new
        {
            invoice.Number,
            invoice.IssuedDate,
            invoice.DueDate,
            invoice.SellerAddress,
            invoice.CustomerAddress,
            invoice.LineItems,
            Total = invoice.LineItems.Sum(li => li.Price * li.Quantity)
        };

        var html = template(date);

        var browserFetcher = new BrowserFetcher();
        await browserFetcher.DownloadAsync();

        using var browser = Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true
        }).GetAwaiter().GetResult();

        using var page = await browser.NewPageAsync();

        await page.SetContentAsync(html);

        await page.EvaluateExpressionAsync("document.fonts.ready");

        return await page.PdfDataAsync(new PdfOptions
        {
            Format = PuppeteerSharp.Media.PaperFormat.A4,
            PrintBackground = true,
            MarginOptions = new MarginOptions
            {
                Top = "50px",
                Right = "20px",
                Bottom = "50px",
                Left = "20px"
            }
        });
    }
}
