using System.Net.Http.Headers;
using System.Text;
using EplToPdfConverter;
using EplToPdfConverter.EplBuilder;
using EplToPdfConverter.EplToZplConverter;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/label/sample-epl", () =>
{
    string sampleEpl = EplBuilder
        .Create()
        .ClearImageBuffer()
        .Text(40, 30, "PRODUIT DEMO")
        .Text(40, 80, "IMPRESSION WEBUSB", font: 3)
        .Barcode(40, 120, "123456789", barcodeType: EplBarcodeType.Code39, height: 90)
        .Print(1)
        .Build();

    return Results.Ok(new { epl = sampleEpl });
});

app.MapPost("/api/label/convert", (ConvertRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Epl))
    {
        return Results.BadRequest(new { error = "Le contenu EPL est requis." });
    }

    var converter = new EplToZplConverter();
    string zpl = converter.Convert(request.Epl);

    return Results.Ok(new { zpl });
});

app.MapPost("/api/label/build-epl", (BuildEplRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.ProductName)
        || string.IsNullOrWhiteSpace(request.Sku)
        || string.IsNullOrWhiteSpace(request.Lot)
        || string.IsNullOrWhiteSpace(request.ExpirationDate))
    {
        return Results.BadRequest(new { error = "Les champs ProductName, Sku, Lot et ExpirationDate sont requis." });
    }

    int quantity = request.Quantity > 0 ? request.Quantity : 1;

    string epl = EplBuilder
        .Create()
        .ClearImageBuffer()
        .Text(40, 30, request.ProductName.ToUpperInvariant(), font: 4)
        .Text(40, 75, $"SKU: {request.Sku}", font: 3)
        .Text(40, 110, $"LOT: {request.Lot}", font: 3)
        .Text(40, 145, $"EXP: {request.ExpirationDate}", font: 3)
        .Barcode(40, 185, request.Sku, barcodeType: EplBarcodeType.Code39, narrowBarWidth: 2, wideBarWidth: 6, height: 90)
        .Print(quantity)
        .Build();

    var converter = new EplToZplConverter();
    string zpl = converter.Convert(epl);

    return Results.Ok(new { epl, zpl });
});

app.MapPost("/api/label/preview-pdf", async (PreviewPdfRequest request, IHttpClientFactory httpClientFactory) =>
{
    if (string.IsNullOrWhiteSpace(request.Zpl))
    {
        return Results.BadRequest(new { error = "Le contenu ZPL est requis." });
    }

    const string labelaryUrl = "https://api.labelary.com/v1/printers/8dpmm/labels/4x6/0/";
    var client = httpClientFactory.CreateClient();

    using var message = new HttpRequestMessage(HttpMethod.Post, labelaryUrl)
    {
        Content = new StringContent(request.Zpl, Encoding.UTF8, "application/x-www-form-urlencoded")
    };

    message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/pdf"));

    using var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead);
    if (!response.IsSuccessStatusCode)
    {
        string details = await response.Content.ReadAsStringAsync();
        return Results.BadRequest(new { error = $"Labelary a retourne {response.StatusCode}: {details}" });
    }

    string? mediaType = response.Content.Headers.ContentType?.MediaType;
    if (!string.Equals(mediaType, "application/pdf", StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { error = $"Type inattendu retourne par Labelary: {mediaType ?? "inconnu"}." });
    }

    byte[] pdfBytes = await response.Content.ReadAsByteArrayAsync();
    return Results.File(pdfBytes, "application/pdf", "preview.pdf");
});

app.Run();

internal sealed record ConvertRequest(string Epl);
internal sealed record BuildEplRequest(string ProductName, string Sku, string Lot, string ExpirationDate, int Quantity);
internal sealed record PreviewPdfRequest(string Zpl);
