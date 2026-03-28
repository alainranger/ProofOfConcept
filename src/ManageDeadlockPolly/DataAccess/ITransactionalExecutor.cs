using System.Data;

namespace ManageDeadlockPolly.DataAccess;

/// <summary>
/// Abstraction pour exécuter des opérations transactionnelles avec gestion automatique du retry.
/// Découple l'application métier de Polly et des détails d'implémentation retry.
/// </summary>
public interface ITransactionalExecutor
{
    /// <summary>
    /// Exécute une action transactionnelle avec retry automatique sur deadlock.
    /// La transaction est recréée à chaque retry.
    /// </summary>
    Task<T> ExecuteAsync<T>(
        Func<IDbConnection, IDbTransaction, Task<T>> action,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exécute une action transactionnelle sans valeur de retour avec retry automatique.
    /// </summary>
    Task ExecuteAsync(
        Func<IDbConnection, IDbTransaction, Task> action,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exécute une action transactionnelle synchrone (wrapper).
    /// </summary>
    T Execute<T>(
        Func<IDbConnection, IDbTransaction, T> action,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
}
