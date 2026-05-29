using System.Text;

namespace EplToPdfConverter.EplBuilder;

public sealed class EplBuilder
{
    private readonly StringBuilder _content = new();

    private EplBuilder()
    {
    }

    public static EplBuilder Create() => new();

    public EplBuilder ClearImageBuffer()
    {
        _content.AppendLine("N");
        return this;
    }

    public EplBuilder Text(
        int x,
        int y,
        string text,
        EplRotation rotation = EplRotation.Normal,
        int font = 4,
        int horizontalMultiplier = 1,
        int verticalMultiplier = 1,
        bool reverse = false)
    {
        string sanitizedText = SanitizeText(text);
        char reverseFlag = reverse ? 'R' : 'N';

        _content.Append('A')
            .Append(x)
            .Append(',')
            .Append(y)
            .Append(',')
            .Append((int)rotation)
            .Append(',')
            .Append(font)
            .Append(',')
            .Append(horizontalMultiplier)
            .Append(',')
            .Append(verticalMultiplier)
            .Append(',')
            .Append(reverseFlag)
            .Append(',')
            .Append('"')
            .Append(sanitizedText)
            .AppendLine("\"");

        return this;
    }

    public EplBuilder Barcode(
        int x,
        int y,
        string data,
        EplRotation rotation = EplRotation.Normal,
        EplBarcodeType barcodeType = EplBarcodeType.Code39,
        int narrowBarWidth = 2,
        int wideBarWidth = 6,
        int height = 100,
        bool showHumanReadableText = true)
    {
        string sanitizedData = SanitizeText(data);
        char humanReadableFlag = showHumanReadableText ? 'B' : 'N';

        _content.Append('B')
            .Append(x)
            .Append(',')
            .Append(y)
            .Append(',')
            .Append((int)rotation)
            .Append(',')
            .Append((int)barcodeType)
            .Append(',')
            .Append(narrowBarWidth)
            .Append(',')
            .Append(wideBarWidth)
            .Append(',')
            .Append(height)
            .Append(',')
            .Append(humanReadableFlag)
            .Append(',')
            .Append('"')
            .Append(sanitizedData)
            .AppendLine("\"");

        return this;
    }

    public EplBuilder Print(int quantity = 1)
    {
        _content.Append('P').AppendLine(quantity.ToString());
        return this;
    }

    public string Build() => _content.ToString();

    private static string SanitizeText(string value) => value.Replace("\"", "'");
}