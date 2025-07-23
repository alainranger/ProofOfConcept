using System;
using DateTimeUtcNowTestable;
using DateTimeUtcNowTestable.Services;
using Moq;
using Xunit;

public class RapportServiceTests
{
    [Fact]
    public void CreerRapport_DoitAssignerLaDateFournieParLeProvider()
    {
        // ARRANGE (Préparation)

        // 1. Définir une date et une heure fixes que nous contrôlerons.
        var dateDeTestPrevisible = new DateTime(2025, 07, 23, 13, 30, 00, DateTimeKind.Utc);

        // 2. Créer un "mock" (un faux objet) de notre IDateTimeProvider.
        var mockDateTimeProvider = new Mock<IDateTimeProvider>();

        // 3. Configurer le mock : quand la propriété "UtcNow" sera appelée,
        //    elle devra retourner notre date de test prévisible.
        mockDateTimeProvider.Setup(p => p.UtcNow).Returns(dateDeTestPrevisible);

        // 4. Créer une instance du service à tester en lui injectant notre faux provider.
        //    Le service pensera qu'il obtient l'heure réelle, mais en fait, c'est nous qui la contrôlons.
        var service = new RapportServiceTestable(mockDateTimeProvider.Object);


        // ACT (Action)
        // On exécute la méthode que l'on souhaite tester.
        var resultat = service.CreerRapport("Rapport Mensuel", "Contenu du rapport...");


        // ASSERT (Vérification)
        // On vérifie que le résultat est celui attendu.
        Assert.NotNull(resultat);
        Assert.Equal("Rapport Mensuel", resultat.Title);

        // La vérification cruciale : on peut maintenant affirmer que la date
        // est EXACTEMENT celle que nous avons définie. Le test est fiable.
        Assert.Equal(dateDeTestPrevisible, resultat.CreatedAtUtc);
    }
}