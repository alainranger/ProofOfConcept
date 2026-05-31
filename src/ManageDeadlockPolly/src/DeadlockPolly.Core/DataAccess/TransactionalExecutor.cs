using System.Data;
using DeadlockPolly.Core.RetryPolicies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
    private readonly ILogger<TransactionalExecutor> _logger;

    public TransactionalExecutor(
        IDbConnectionProvider connectionProvider,
        IDeadlockRetryPolicy retryPolicy,
        ILogger<TransactionalExecutor>? logger = null)
    {
        _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        _retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
        _logger = logger ?? NullLogger<TransactionalExecutor>.Instance;
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
                    _logger.LogDebug("Transaction ouverte (Isolation: {IsolationLevel})", isolationLevel);
                    var result = await action(conn, tx);
                    tx.Commit();
                    _logger.LogDebug("Transaction validée");
                    return result;
                }
                catch
                {
                    tx.Rollback();
                    _logger.LogWarning("Transaction annulée suite à une erreur");
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
