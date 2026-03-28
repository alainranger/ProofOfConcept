using System.Data;

namespace DeadlockPolly.Core.DataAccess;

/// <summary>
/// Abstraction pour exécuter des opérations transactionnelles avec retry automatique.
/// La transaction est recréée à chaque tentative.
/// </summary>
public interface ITransactionalExecutor
{
    /// <summary>Exécute une action transactionnelle avec retry sur deadlock, retourne un résultat.</summary>
    Task<T> ExecuteAsync<T>(
        Func<IDbConnection, IDbTransaction, Task<T>> action,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default);

    /// <summary>Exécute une action transactionnelle sans valeur de retour avec retry.</summary>
    Task ExecuteAsync(
        Func<IDbConnection, IDbTransaction, Task> action,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default);

    /// <summary>Exécute une action transactionnelle synchrone (wrapper).</summary>
    T Execute<T>(
        Func<IDbConnection, IDbTransaction, T> action,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
}
