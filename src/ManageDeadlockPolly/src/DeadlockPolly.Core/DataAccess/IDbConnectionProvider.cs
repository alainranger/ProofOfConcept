using System.Data;

namespace DeadlockPolly.Core.DataAccess;

/// <summary>
/// Abstraction pour créer et gérer les connexions aux bases de données.
/// Découple le retry service des détails de connexion spécifiques.
/// </summary>
public interface IDbConnectionProvider
{
    /// <summary>Crée une nouvelle connexion ouverte vers la base de données.</summary>
    Task<IDbConnection> CreateAndOpenAsync(CancellationToken cancellationToken = default);

    /// <summary>Commence une nouvelle transaction avec le niveau d'isolation spécifié.</summary>
    Task<IDbTransaction> BeginTransactionAsync(
        IDbConnection connection,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default);
}
