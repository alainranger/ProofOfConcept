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
builder.Services.AddSingleton<IInvoiceToPdf, InvoiceToPdfPlaywright>();

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

app.MapGet("invoice-report", async (InvoiceFactory invoiceFactory, IInvoiceToPdf invoiceToPdf) =>
{
    var invoice = invoiceFactory.Create();

    var pdfData = await invoiceToPdf.GeneratePdfAsync(invoice)
        .ConfigureAwait(false);

    return Results.File(pdfData, "application/pdf", $"invoice-{invoice.Number}.pdf");
});

app.UseHttpsRedirection();

app.Run();