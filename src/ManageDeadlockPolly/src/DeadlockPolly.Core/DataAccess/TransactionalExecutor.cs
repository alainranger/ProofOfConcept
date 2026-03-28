using System.Data;
using DeadlockPolly.Core.RetryPolicies;

namespace DeadlockPolly.Core.DataAccess;

/// <summary>
/// Implémentation générique de ITransactionalExecutor.
/// Combine une stratégie de retry et un fournisseur de connexion.
/// Utilisable dans n'importe quel projet (API, Worker, Console…).
/// </summary>
public class TransactionalExecutor : ITransactionalExecutor
{
    private readonly IDbConnectionProvider _connectionProvider;
    private readonly IDeadlockRetryPolicy _retryPolicy;

    public TransactionalExecutor(
        IDbConnectionProvider connectionProvider,
        IDeadlockRetryPolicy retryPolicy)
    {
        _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        _retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
    }

    public async Task<T> ExecuteAsync<T>(
        Func<IDbConnection, IDbTransaction, Task<T>> action,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var conn = await _connectionProvider.CreateAndOpenAsync(cancellationToken);
            try
            {
                var tx = await _connectionProvider.BeginTransactionAsync(conn, isolationLevel, cancellationToken);
                try
                {
                    Console.WriteLine($"[TX] Transaction ouverte (Isolation: {isolationLevel})");
                    var result = await action(conn, tx);
                    tx.Commit();
                    Console.WriteLine("[TX] Transaction validée ✓");
                    return result;
                }
                catch
                {
                    tx.Rollback();
                    Console.WriteLine("[TX] Transaction annulée");
                    throw;
                }
                finally
                {
                    tx.Dispose();
                }
            }
            finally
            {
                conn.Dispose();
            }
        }, cancellationToken);
    }

    public async Task ExecuteAsync(
        Func<IDbConnection, IDbTransaction, Task> action,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
    {
        await ExecuteAsync<object?>(async (conn, tx) =>
        {
            await action(conn, tx);
            return null;
        }, isolationLevel, cancellationToken);
    }

    public T Execute<T>(
        Func<IDbConnection, IDbTransaction, T> action,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        return ExecuteAsync(
            (conn, tx) => Task.FromResult(action(conn, tx)),
            isolationLevel
        ).GetAwaiter().GetResult();
    }
}
