using Microsoft.Data.SqlClient;
using Polly;

namespace ManageDeadlockPolly.RetryPolicies;

/// <summary>
/// Implémentation Polly de la stratégie de retry sur deadlock SQL Server.
/// Gère automatiquement le retry avec backoff exponentiel + jitter.
/// </summary>
public class PollyDeadlockRetryPolicy : IDeadlockRetryPolicy
{
    private readonly DeadlockRetryPolicyOptions _options;
    private readonly IAsyncPolicy _asyncPolicy;

    public PollyDeadlockRetryPolicy(DeadlockRetryPolicyOptions? options = null)
    {
        options?.Validate();
        _options = options ?? new DeadlockRetryPolicyOptions();
        _asyncPolicy = BuildAsyncPolicy();
    }

    /// <summary>
    /// Construit la politique Polly pour retry sur deadlock (erreur SQL Server 1205).
    /// - Détecte SqlException avec Number == 1205 (deadlock victim)
    /// - Backoff exponentiel + jitter aléatoire
    /// - Callback optionnel pour logging/monitoring
    /// </summary>
    private IAsyncPolicy BuildAsyncPolicy()
    {
        return Policy
            .Handle<SqlException>(ex => ex.Number == _options.DeadlockErrorNumber)
            .WaitAndRetryAsync(
                retryCount: _options.MaxRetries,
                sleepDurationProvider: retryAttempt =>
                {
                    var baseDelay = TimeSpan.FromMilliseconds(
                        _options.InitialDelayMs * Math.Pow(2, retryAttempt - 1));
                    var jitter = TimeSpan.FromMilliseconds(
                        Random.Shared.Next(0, _options.MaxJitterMs));
                    var totalDelay = baseDelay.Add(jitter);

                    _options.OnRetry?.Invoke(retryAttempt, totalDelay);
                    return totalDelay;
                });
    }

    private IAsyncPolicy<T> BuildAsyncPolicy<T>()
    {
        return Policy
            .Handle<SqlException>(ex => ex.Number == _options.DeadlockErrorNumber)
            .OrResult<T>(r => false) // Jamais ici, juste pour typage générique
            .WaitAndRetryAsync<T>(
                retryCount: _options.MaxRetries,
                sleepDurationProvider: retryAttempt =>
                {
                    var baseDelay = TimeSpan.FromMilliseconds(
                        _options.InitialDelayMs * Math.Pow(2, retryAttempt - 1));
                    var jitter = TimeSpan.FromMilliseconds(
                        Random.Shared.Next(0, _options.MaxJitterMs));
                    var totalDelay = baseDelay.Add(jitter);

                    _options.OnRetry?.Invoke(retryAttempt, totalDelay);
                    return totalDelay;
                });
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        var policy = BuildAsyncPolicy<T>();
        return await policy.ExecuteAsync(async (ctx) => await action(), cancellationToken);
    }

    public async Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        await _asyncPolicy.ExecuteAsync(async (ctx) => await action(), cancellationToken);
    }

    public T Execute<T>(Func<T> action)
    {
        return ExecuteAsync(() => Task.FromResult(action())).GetAwaiter().GetResult();
    }
}
