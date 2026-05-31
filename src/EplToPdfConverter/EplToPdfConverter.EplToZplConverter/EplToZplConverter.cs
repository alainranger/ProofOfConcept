namespace EplToPdfConverter.EplToZplConverter;

using System.Text;
using System.Text.RegularExpressions;
/// <summary>
/// Convertisseur utilisant les Source Generators de Regex introduits dans les versions récentes de .NET
/// </summary>
public partial class EplToZplConverter
{
    // Génération de la Regex à la compilation pour des performances maximales (Source Generators)
    [GeneratedRegex(@"^(\d+),(\d+),(\d+),(\d+),(\d+),(\d+),([N|R]),""(.*)""")]
    private static partial Regex TextRegex();

    [GeneratedRegex(@"^(\d+),(\d+),(\d+),(\d+),(\d+),(\d+),(\d+),([B|N]),""(.*)""")]
    private static partial Regex BarcodeRegex();

    public string Convert(string epl)
    {
        var zpl = new StringBuilder();
        zpl.AppendLine("^XA"); // Début du format ZPL

        string[] lines = epl.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in lines)
        {
            string trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            char command = trimmed[0];
            string arguments = trimmed[1..]; // Utilisation des indices/ranges modernes de C#

            // Utilisation du Switch Expression moderne de C#
            _ = command switch
            {
                'A' => ParseText(arguments, zpl),
                'B' => ParseBarcode(arguments, zpl),
                _ => false // Ignore 'N', 'P' ou les commandes non supportées
            };
        }

        zpl.AppendLine("^XZ"); // Fin du format ZPL
        return zpl.ToString();
    }

    private static bool ParseText(string arguments, StringBuilder zpl)
    {
        var match = TextRegex().Match(arguments);
        if (!match.Success) return false;

        int x = int.Parse(match.Groups[1].ValueSpan); // Optimisation .NET : utilisation de ValueSpan pour éviter les allocations
        int y = int.Parse(match.Groups[2].ValueSpan);
        string textData = match.Groups[8].Value;

        zpl.AppendLine($"^FO{x},{y}^A0N,24,24^FD{textData}^FS");
        return true;
    }

    private static bool ParseBarcode(string arguments, StringBuilder zpl)
    {
        var match = BarcodeRegex().Match(arguments);
        if (!match.Success) return false;

        int x = int.Parse(match.Groups[1].ValueSpan);
        int y = int.Parse(match.Groups[2].ValueSpan);
        int width = int.Parse(match.Groups[4].ValueSpan);
        int height = int.Parse(match.Groups[7].ValueSpan);
        string barcodeData = match.Groups[9].Value;

        zpl.AppendLine($"^BY{width}");
        zpl.AppendLine($"^FO{x},{y}^BCN,{height},Y,N,N^FD{barcodeData}^FS");
        return true;
    }
}