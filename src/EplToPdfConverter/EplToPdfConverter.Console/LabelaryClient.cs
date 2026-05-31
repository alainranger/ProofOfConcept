namespace EplToPdfConverter;

using System.Net.Http.Headers;
using System.Text;
/// <summary>
/// Client HTTP optimisé utilisant les fonctionnalités modernes de .NET
/// </summary>
public sealed class LabelaryClient : IDisposable
{
    private readonly HttpClient _httpClient = new();
    private const string PdfMediaType = "application/pdf";

    public async Task<bool> DownloadPdfFromZplAsync(string zpl, string outputPath)
    {
        try
        {
            // API Labelary pour un format d'étiquette standard 4x6 pouces (10x15cm)
            const string url = "https://api.labelary.com/v1/printers/8dpmm/labels/4x6/0/";

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(zpl, Encoding.UTF8, "application/x-www-form-urlencoded");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(PdfMediaType));

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                string errorLog = await ReadErrorResponseAsync(response);
                Console.WriteLine($"Erreur API ({response.StatusCode}) : {errorLog}");
                return false;
            }

            string? mediaType = response.Content.Headers.ContentType?.MediaType;
            if (!string.Equals(mediaType, PdfMediaType, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Réponse inattendue du service : type de contenu '{mediaType ?? "inconnu"}'.");
                return false;
            }

            // Lecture et écriture asynchrone native du flux binaire en .NET
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
            await response.Content.CopyToAsync(fileStream);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la requête : {ex.Message}");
            return false;
        }
    }

    private static async Task<string> ReadErrorResponseAsync(HttpResponseMessage response)
    {
        string? mediaType = response.Content.Headers.ContentType?.MediaType;

        if (!string.IsNullOrWhiteSpace(mediaType)
            && !mediaType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(mediaType, "application/json", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(mediaType, "application/problem+json", StringComparison.OrdinalIgnoreCase))
        {
            return $"corps de réponse non textuel reçu ({mediaType})";
        }

        string errorLog = await response.Content.ReadAsStringAsync();
        return string.IsNullOrWhiteSpace(errorLog)
            ? "aucun détail fourni par le service."
            : errorLog;
    }

    public void Dispose() => _httpClient.Dispose();
}
