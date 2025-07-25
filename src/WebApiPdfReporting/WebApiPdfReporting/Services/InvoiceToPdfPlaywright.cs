using HandlebarsDotNet;
using Microsoft.Playwright;
using WebApiPdfReporting.Models;

internal class InvoiceToPdfPlaywright : IInvoiceToPdf
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

        var exitCode = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        if (exitCode != 0)
        {
            throw new Exception($"Playwright exited with code {exitCode}");
        }

        using var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });



        var page = await browser.NewPageAsync();
        await page.SetContentAsync(html);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var pdfData = await page.PdfAsync(new PagePdfOptions
        {
            Format = PaperFormat.A4,
            PrintBackground = true,
            Margin = new Margin
            {
                Top = "50px",
                Right = "20px",
                Bottom = "50px",
                Left = "20px"
            }
        });

        await browser.CloseAsync();

        return pdfData;
    }
}