namespace DateTimeUtcNowTestable.Services;

// Un simple objet pour représenter notre rapport
public class Report
{
    public string? Title { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? Content { get; set; }
}

// Le service qui crée le rapport
public class ReportService
{
    public Report CreateReport(string title, string content)
    {
        // PROBLÈME : Utilisation directe de DateTime.UtcNow.
        // Comment peut-on tester que la date est bien celle attendue ?
        // C'est impossible, car elle change constamment.
        var report = new Report
        {
            Title = title,
            Content = content,
            CreatedAtUtc = DateTime.UtcNow
        };

        // Logique métier...
        Console.WriteLine($"Rapport '{report.Title}' créé le {report.CreatedAtUtc}.");

        return report;
    }
}