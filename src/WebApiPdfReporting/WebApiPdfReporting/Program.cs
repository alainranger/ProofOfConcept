using System.Globalization;
using HandlebarsDotNet;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using Swashbuckle.AspNetCore.SwaggerGen;
using WebApiPdfReporting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<InvoiceFactory>();

Handlebars.RegisterHelper("formatDate", (context, arguments) =>
{
    if (arguments[0] is DateOnly date)
    {
        return date.ToString("dd/MM/yyyy");
    }

    return arguments[0]?.ToString() ?? string.Empty;
});

Handlebars.RegisterHelper("formatCurrency", (context, arguments) =>
{
    if (arguments[0] is decimal value)
    {
        return value.ToString("C", CultureInfo.CreateSpecificCulture("en-US"));
    }

    return arguments[0]?.ToString() ?? string.Empty;
});

Handlebars.RegisterHelper("formatNumber", (context, arguments) =>
{
    if (arguments[0] is int number)
    {
        return number.ToString("N2");
    }

    return arguments[0]?.ToString() ?? string.Empty;
});

Handlebars.RegisterHelper("multiply", (context, arguments) =>
{
    if (arguments.Length >= 2 &&
        arguments[0] is decimal a &&
        arguments[1] is decimal b)
    {
        return a * b;
    }

    return 0m;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("invoice-report", async (InvoiceFactory invoiceFactory) =>
{
    var invoice = invoiceFactory.Create();

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

    var pdfData = await page.PdfDataAsync(new PdfOptions
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

    return Results.File(pdfData, "application/pdf", $"invoice-{invoice.Number}.pdf");
});

app.UseHttpsRedirection();

app.Run();