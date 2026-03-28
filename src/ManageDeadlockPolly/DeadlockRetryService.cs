using System.Data;
using Dapper;
using ManageDeadlockPolly.DataAccess;
using ManageDeadlockPolly.RetryPolicies;

namespace ManageDeadlockPolly;

/// <summary>
/// Service legacy pour gérer les deadlocks avec Polly + Dapper.
/// ⚠️ DÉPRÉCIÉ: Utiliser ITransactionalExecutor + DI à la place.
/// 
/// Ce service est conservé pour la compatibilité rétroactive avec le code existant.
/// Nouveaux projets: voir docs/INTEGRATION_GUIDE.md pour les patterns recommandés.
/// </summary>
[Obsolete("Utiliser ITransactionalExecutor avec les services DI. Voir docs/INTEGRATION_GUIDE.md")]
public class DeadlockRetryService
{
    private readonly ITransactionalExecutor _executor;

    public DeadlockRetryService(string connectionString, int maxRetries = 5)
    {
        var retryPolicy = new PollyDeadlockRetryPolicy(new DeadlockRetryPolicyOptions
        {
            MaxRetries = maxRetries,
            OnRetry = (retryCount, delay) =>
                Console.WriteLine($"[RETRY {retryCount}] Deadlock détecté, retry dans {delay.TotalMilliseconds:F0}ms")
        });

        var connectionProvider = new SqlServerConnectionProvider(connectionString);
        _executor = new TransactionalExecutor(connectionProvider, retryPolicy);
    }

    /// <summary>
    /// Exécute une opération Dapper transactionnelle avec retry automatique sur deadlock.
    /// La transaction est rouverte à chaque retry.
    /// </summary>
    public async Task<T> ExecuteWithDeadlockRetryAsync<T>(
        Func<IDbConnection, IDbTransaction, Task<T>> action,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        return await _executor.ExecuteAsync(action, isolationLevel);
    }

    /// <summary>
    /// Exécute une opération Dapper transactionnelle asynchrone sans valeur de retour.
    /// </summary>
    public async Task ExecuteWithDeadlockRetryAsync(
        Func<IDbConnection, IDbTransaction, Task> action,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        await _executor.ExecuteAsync(action, isolationLevel);
    }

    /// <summary>
    /// Exécute une opération Dapper transactionnelle synchrone avec retry.
    /// </summary>
    public T ExecuteWithDeadlockRetry<T>(
        Func<IDbConnection, IDbTransaction, T> action,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        return _executor.Execute(action, isolationLevel);
    }
}

/// <summary>
/// Modèle de données pour le test.
/// </summary>
public class DeadlockTestRecord
{
    public int Id { get; set; }
    public int Value { get; set; }
}

/// <summary>
/// Repository Dapper pour les opérations test.
/// </summary>
public class DeadlockTestRepository
{
    private const string IncrementSql = "UPDATE dbo.DeadlockTest SET Value = Value + 1 WHERE Id = @Id";
    private readonly ITransactionalExecutor _executor;

    public DeadlockTestRepository(ITransactionalExecutor executor)
    {
        _executor = executor;
    }

    /// <summary>
    /// Incrémente les deux lignes dans l'ordre 1 puis 2.
    /// </summary>
    public Task<int> IncrementBothValuesAsync()
    {
        return IncrementValuesAsync(1, 2);
    }

    /// <summary>
    /// Incrémente les deux lignes dans l'ordre inverse 2 puis 1.
    /// Utilisé pour provoquer un deadlock en concurrence.
    /// </summary>
    public Task<int> IncrementBothValuesReverseOrderAsync()
    {
        return IncrementValuesAsync(2, 1);
    }

    private async Task<int> IncrementValuesAsync(int firstId, int secondId)
    {
        return await _executor.ExecuteAsync(async (conn, tx) =>
        {
            var updated1 = await conn.ExecuteAsync(
                IncrementSql,
                new { Id = firstId },
                transaction: tx);

            await Task.Delay(500);

            var updated2 = await conn.ExecuteAsync(
                IncrementSql,
                new { Id = secondId },
                transaction: tx);

            return updated1 + updated2;
        });
    }

    /// <summary>
    /// Lit les valeurs actuelles.
    /// </summary>
    public async Task<List<DeadlockTestRecord>> GetValuesAsync()
    {
        return await _executor.ExecuteAsync(async (conn, tx) =>
        {
            var query = "SELECT Id, Value FROM dbo.DeadlockTest ORDER BY Id";
            var result = await conn.QueryAsync<DeadlockTestRecord>(query, transaction: tx);
            return result.ToList();
        });
    }

    /// <summary>
    /// Reset des valeurs pour les tests.
    /// </summary>
    public async Task ResetAsync()
    {
        await _executor.ExecuteAsync(
            (conn, tx) => conn.ExecuteAsync("UPDATE dbo.DeadlockTest SET Value = 0", transaction: tx));
    }
}
