using System;

namespace DateTimeUtcNowTestable.Services;

/// <summary>
/// Le IDateTimeProvider est "injecté" via le constructeur.
/// Cela nous permet de fournir une vraie ou une fausse implémentation.
/// </summary>
public class RapportServiceTestable(IDateTimeProvider dateTimeProvider)
{
    // Le service dépend maintenant de l'interface, pas d'une classe concrète.
    private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));

    public Report CreerRapport(string titre, string contenu)
    {
        var rapport = new Report
        {
            Title = titre,
            Content = contenu,
            // On utilise notre abstraction ! Le code est maintenant découplé.
            CreatedAtUtc = _dateTimeProvider.UtcNow
        };

        Console.WriteLine($"Rapport '{rapport.Title}' créé le {rapport.CreatedAtUtc}.");

        return rapport;
    }
}
