using Dapper;
using DeadlockPolly.Core.DataAccess;

namespace DeadlockPolly.Core.Repositories;

/// <summary>
/// Repository Dapper pour piloter les scénarios de deadlock.
/// Les méthodes acquièrent les ressources dans des ordres opposés
/// pour déclencher un deadlock SQL Server détectable par Polly.
/// </summary>
public class DeadlockTestRepository
{
    private const string IncrementSql = "UPDATE dbo.DeadlockTest SET Value = Value + 1 WHERE Id = @Id";
    private readonly ITransactionalExecutor _executor;

    public DeadlockTestRepository(ITransactionalExecutor executor)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    }

    /// <summary>Incrémente les deux lignes dans l'ordre 1 → 2.</summary>
    public Task<int> IncrementBothValuesAsync() => IncrementValuesAsync(1, 2);

    /// <summary>
    /// Incrémente les deux lignes dans l'ordre inverse 2 → 1.
    /// En concurrence avec <see cref="IncrementBothValuesAsync"/>, provoque un deadlock.
    /// </summary>
    public Task<int> IncrementBothValuesReverseOrderAsync() => IncrementValuesAsync(2, 1);

    private async Task<int> IncrementValuesAsync(int firstId, int secondId)
    {
        return await _executor.ExecuteAsync(async (conn, tx) =>
        {
            var updated1 = await conn.ExecuteAsync(IncrementSql, new { Id = firstId }, transaction: tx);
            await Task.Delay(500); // délai pour laisser l'autre transaction s'interposer
            var updated2 = await conn.ExecuteAsync(IncrementSql, new { Id = secondId }, transaction: tx);
            return updated1 + updated2;
        });
    }

    /// <summary>Lit les valeurs actuelles.</summary>
    public async Task<List<DeadlockTestRecord>> GetValuesAsync()
    {
        return await _executor.ExecuteAsync(async (conn, tx) =>
        {
            var result = await conn.QueryAsync<DeadlockTestRecord>(
                "SELECT Id, Value FROM dbo.DeadlockTest ORDER BY Id",
                transaction: tx);
            return result.ToList();
        });
    }

    /// <summary>Remet toutes les valeurs à zéro.</summary>
    public async Task ResetAsync()
    {
        await _executor.ExecuteAsync(
            (conn, tx) => conn.ExecuteAsync("UPDATE dbo.DeadlockTest SET Value = 0", transaction: tx));
    }
}
