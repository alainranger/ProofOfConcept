using Microsoft.Data.SqlClient;
using Polly;

namespace DeadlockPolly.Core.RetryPolicies;

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
    /// Politique Polly pour void — détecte SqlException 1205, backoff exponentiel + jitter.
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
                        Random.Shared.Next(0, _options.MaxJitterMs + 1));
                    var totalDelay = baseDelay.Add(jitter);
                    _options.OnRetry?.Invoke(retryAttempt, totalDelay);
                    return totalDelay;
                });
    }

    private IAsyncPolicy<T> BuildAsyncPolicy<T>()
    {
        return Policy
            .Handle<SqlException>(ex => ex.Number == _options.DeadlockErrorNumber)
            .OrResult<T>(_ => false) // jamais évalué, requis pour le typage générique
            .WaitAndRetryAsync<T>(
                retryCount: _options.MaxRetries,
                sleepDurationProvider: retryAttempt =>
                {
                    var baseDelay = TimeSpan.FromMilliseconds(
                        _options.InitialDelayMs * Math.Pow(2, retryAttempt - 1));
                    var jitter = TimeSpan.FromMilliseconds(
                        Random.Shared.Next(0, _options.MaxJitterMs + 1));
                    var totalDelay = baseDelay.Add(jitter);
                    _options.OnRetry?.Invoke(retryAttempt, totalDelay);
                    return totalDelay;
                });
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        var policy = BuildAsyncPolicy<T>();
        return await policy.ExecuteAsync(async (_) => await action(), cancellationToken);
    }

    public async Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        await _asyncPolicy.ExecuteAsync(async (_) => await action(), cancellationToken);
    }

    public T Execute<T>(Func<T> action)
    {
        return ExecuteAsync(() => Task.FromResult(action())).GetAwaiter().GetResult();
    }
}
