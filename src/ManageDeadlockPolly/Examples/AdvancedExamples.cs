using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace ManageDeadlockPolly;

/// <summary>
/// Exemples avancés de gestion des deadlocks avec Polly + Dapper.
/// </summary>
public static class AdvancedDeadlockExamples
{
    /// <summary>
    /// Exemple 1: Retry + Circuit Breaker
    /// Si trop de deadlocks successifs, ouvre le circuit (fail-fast).
    /// </summary>
    public static IAsyncPolicy<T> BuildRetryWithCircuitBreakerPolicy<T>(
        int maxRetries = 5,
        int failuresToOpenCircuit = 10)
    {
        return Policy
            .Handle<SqlException>(ex => ex.Number == 1205)
            .OrResult<T>(r => false)
            .WaitAndRetryAsync<T>(
                retryCount: maxRetries,
                sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt - 1)),
                onRetry: (outcome, delay, retryCount, context) =>
                    Console.WriteLine($"[RETRY {retryCount}] Détecté, attente {delay.TotalMilliseconds:F0}ms")
            )
            .WrapAsync(
                Policy
                    .Handle<SqlException>(ex => ex.Number == 1205)
                    .OrResult<T>(r => false)
                    .CircuitBreakerAsync<T>(
                        handledEventsAllowedBeforeBreaking: failuresToOpenCircuit,
                        durationOfBreak: TimeSpan.FromSeconds(10),
                        onBreak: (outcome, duration) =>
                            Console.WriteLine($"🔴 CIRCUIT OUVERT pour {duration.TotalSeconds}s"),
                        onReset: () =>
                            Console.WriteLine("🟢 CIRCUIT FERMÉ - Retry autorisé")
                    )
            );
    }

    /// <summary>
    /// Exemple 2: Retry avec callback personnalisé pour logging/telemetry.
    /// </summary>
    public static IAsyncPolicy<T> BuildRetryWithTelemetry<T>(
        Action<SqlException, TimeSpan, int> onRetryCallback,
        int maxRetries = 5)
    {
        return Policy
            .Handle<SqlException>(ex => ex.Number == 1205)
            .OrResult<T>(r => false)
            .WaitAndRetryAsync<T>(
                retryCount: maxRetries,
                sleepDurationProvider: attempt =>
                {
                    var delay = TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt - 1));
                    var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 50));
                    return delay.Add(jitter);
                },
                onRetry: (outcome, delay, retryCount, context) =>
                {
                    var ex = outcome.Exception as SqlException;
                    onRetryCallback?.Invoke(ex!, delay, retryCount);
                }
            );
    }

    /// <summary>
    /// Exemple 3: Ordering idempotent
    /// Utilise une colonne de version pour détecter les updates en doublon.
    /// </summary>
    public static async Task<bool> UpdateWithVersionCheckAsync(
        IDbConnection conn,
        IDbTransaction tx,
        string tableName,
        int id,
        int currentVersion)
    {
        const string query = @"
            UPDATE {0}
            SET Version = Version + 1, LastUpdated = GETUTCDATE()
            WHERE Id = @Id AND Version = @CurrentVersion;
            SELECT @@ROWCOUNT;
        ";

        var rowsAffected = await conn.ExecuteScalarAsync<int>(
            string.Format(query, tableName),
            new { Id = id, CurrentVersion = currentVersion },
            transaction: tx
        );

        return rowsAffected > 0;
    }

    /// <summary>
    /// Exemple 4: Batch operations avec retry.
    /// Approche "chercher le lot, puis updater" = moins de temps d'attente en TX.
    /// </summary>
    public static async Task BatchUpdatePatternAsync(
        string connectionString,
        Func<IDbConnection, Task<List<int>>> fetchIdsToUpdate,
        Func<IDbConnection, IDbTransaction, List<int>, Task> performUpdate)
    {
        // 1. Récupérer les IDs HORS transaction (pas de verrouillage)
        await using var readConn = new SqlConnection(connectionString);
        await readConn.OpenAsync();
        var idsToUpdate = await fetchIdsToUpdate(readConn);
        await readConn.CloseAsync();

        // 2. Effectuer l'update EN transaction courte avec retry
        var retryService = new DeadlockRetryService(connectionString, maxRetries: 3);

        await retryService.ExecuteWithDeadlockRetryAsync(async (conn, tx) =>
        {
            await performUpdate(conn, tx, idsToUpdate);
            return true;
        });
    }

    /// <summary>
    /// Exemple 5: Timeout personnalisé + Deadlock retry.
    /// </summary>
    public static IAsyncPolicy<T> BuildRetryWithTimeoutPolicy<T>(
        int commandTimeoutSeconds = 30,
        int maxRetries = 5)
    {
        return Policy
            .Handle<SqlException>(ex => ex.Number == 1205 || ex.Number == -2) // 1205=Deadlock, -2=Timeout
            .OrResult<T>(r => false)
            .WaitAndRetryAsync<T>(
                retryCount: maxRetries,
                sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(int.Min(100 * int.Parse(Math.Pow(2, attempt).ToString()), 2000)),
                onRetry: (outcome, delay, retryCount, context) =>
                    Console.WriteLine($"[RETRY {retryCount}] Deadlock ou Timeout, retry dans {delay.TotalMilliseconds}ms")
            );
    }

    /// <summary>
    /// Exemple 6: Fallback strategy si tous les retries échouent.
    /// </summary>
    public static IAsyncPolicy<T> BuildRetryWithFallback<T>(
        Func<Task<T>> fallbackAction,
        int maxRetries = 5)
    {
        var fallbackPolicy = Policy<T>
            .Handle<SqlException>(ex => ex.Number == 1205)
            .FallbackAsync(fallbackAction);

        var retryPolicy = Policy<T>
            .Handle<SqlException>(ex => ex.Number == 1205)
            .WaitAndRetryAsync(
                retryCount: maxRetries,
                sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(100 * int.Parse(Math.Pow(2, attempt).ToString()))
            );

        return Policy.WrapAsync(retryPolicy, fallbackPolicy);
    }

    /// <summary>
    /// Exemple 7: Notifications liées aux deadlocks (pour monitoring/alertes).
    /// </summary>
    public class DeadlockNotificationService
    {
        private readonly List<DeadlockEvent> _events = new();

        public record DeadlockEvent(
            DateTime Timestamp,
            int RetryAttempt,
            TimeSpan DelayBeforeRetry,
            string Context
        );

        public void RecordDeadlock(int retry, TimeSpan delay, string context)
        {
            var evt = new DeadlockEvent(DateTime.UtcNow, retry, delay, context);
            _events.Add(evt);

            // Envoyer à monitoring (AppInsights, DataDog, etc.)
            Console.WriteLine($"[DEADLOCK EVENT] T={retry} Delay={delay.TotalMilliseconds}ms Context={context}");
        }

        public IReadOnlyList<DeadlockEvent> GetEvents() => _events.AsReadOnly();
    }

    /// <summary>
    /// Exemple 8: Adaptive retry (augmente delays si deadlocks fréquents).
    /// </summary>
    public class AdaptiveDeadlockRetryPolicy
    {
        private int _deadlockCount;
        private TimeSpan _baseDelay = TimeSpan.FromMilliseconds(100);

        public async Task<T> ExecuteAsync<T>(
            Func<Task<T>> action,
            int maxRetries = 5)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    return await action();
                }
                catch (SqlException ex) when (ex.Number == 1205)
                {
                    _deadlockCount++;

                    // Augmenter le delay adaptatif
                    var adaptiveDelay = _baseDelay.Multiply(Math.Pow(2, i) * (1 + _deadlockCount * 0.1));
                    var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 100));

                    if (i < maxRetries - 1)
                    {
                        Console.WriteLine($"[ADAPTIVE RETRY {i + 1}] Deadlock #{_deadlockCount}, Delay: {adaptiveDelay.Add(jitter).TotalMilliseconds}ms");
                        await Task.Delay(adaptiveDelay.Add(jitter));
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return default!;
        }
    }

    /// <summary>
    /// Exemple 9: Structured concurrency avec SemaphoreSlim (limiter les connexions concurrentes).
    /// </summary>
    public class ConcurrentDeadlockMitigator
    {
        private readonly SemaphoreSlim _connectionSemaphore;
        private readonly DeadlockRetryService _retryService;

        public ConcurrentDeadlockMitigator(DeadlockRetryService retryService, int maxConcurrentConnections = 10)
        {
            _retryService = retryService;
            _connectionSemaphore = new SemaphoreSlim(maxConcurrentConnections);
        }

        public async Task<T> ExecuteWithLimitAsync<T>(
            Func<IDbConnection, IDbTransaction, Task<T>> action)
        {
            await _connectionSemaphore.WaitAsync();
            try
            {
                return await _retryService.ExecuteWithDeadlockRetryAsync(action);
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }
    }
}
