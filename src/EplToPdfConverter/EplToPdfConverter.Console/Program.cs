using EplToPdfConverter;
using EplToPdfConverter.EplBuilder;
using EplToPdfConverter.EplToZplConverter;

// Utilisation des instructions de niveau supérieur (Top-level statements) de .NET 10
Console.WriteLine("--- Convertisseur EPL -> ZPL -> PDF (.NET 10) ---");

// 1. Flux EPL généré via un builder fluent
string eplCode = EplBuilder
    .Create()
    .ClearImageBuffer()
    .Text(50, 40, "PRODUIT DE TEST .NET 10")
    .Text(50, 90, "CODE-BARRES ARTICLE", font: 3)
    .Barcode(50, 130, "987654321", barcodeType: EplBarcodeType.Code39, narrowBarWidth: 2, wideBarWidth: 6, height: 100)
    .Print(1)
    .Build();

Console.WriteLine("\n[1/3] Flux EPL d'origine :");
Console.WriteLine(eplCode);

// 2. Conversion EPL vers ZPL via le convertisseur optimisé
var converter = new EplToZplConverter();
string zplCode = converter.Convert(eplCode);

Console.WriteLine("[2/3] Flux ZPL généré :");
Console.WriteLine(zplCode);

// 3. Appel au client d'API pour récupérer le PDF
Console.WriteLine("[3/3] Envoi du ZPL à l'API Labelary pour génération du PDF...");
string outputPdfPath = Path.Combine(AppContext.BaseDirectory, "etiquette.pdf");

using var labelaryClient = new LabelaryClient();
bool success = await labelaryClient.DownloadPdfFromZplAsync(zplCode, outputPdfPath);

if (success)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"\nSuccès ! Le fichier PDF a été généré ici :\n-> {outputPdfPath}");
}
else
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("\nÉchec de la génération du PDF.");
}
Console.ResetColor();
