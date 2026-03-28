using DeadlockPolly.Core.RetryPolicies;
using DeadlockPolly.Tests.Helpers;
using Microsoft.Data.SqlClient;

namespace DeadlockPolly.Tests.RetryPolicies;

public class PollyDeadlockRetryPolicyTests
{
    // Options rapides pour éviter des délais réels dans les tests
    private static DeadlockRetryPolicyOptions FastOptions(int maxRetries = 5) => new()
    {
        MaxRetries = maxRetries,
        InitialDelayMs = 1,
        MaxJitterMs = 0
    };

    // ─── ExecuteAsync<T> ────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_OnSuccess_ReturnsResult()
    {
        var policy = new PollyDeadlockRetryPolicy(FastOptions());

        var result = await policy.ExecuteAsync(() => Task.FromResult(42));

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task ExecuteAsync_OnDeadlock_RetriesUntilSuccess()
    {
        var attempts = 0;
        var policy = new PollyDeadlockRetryPolicy(FastOptions(maxRetries: 5));

        var result = await policy.ExecuteAsync(async () =>
        {
            attempts++;
            if (attempts < 3)
                throw SqlExceptionHelper.CreateDeadlockException();
            return attempts;
        });

        Assert.Equal(3, result);
        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task ExecuteAsync_OnNonDeadlockException_DoesNotRetry()
    {
        var attempts = 0;
        var policy = new PollyDeadlockRetryPolicy(FastOptions());

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await policy.ExecuteAsync<int>(async () =>
            {
                attempts++;
                throw new InvalidOperationException("pas un deadlock");
            });
        });

        Assert.Equal(1, attempts); // aucun retry
    }

    [Fact]
    public async Task ExecuteAsync_OnNonDeadlockSqlException_DoesNotRetry()
    {
        var attempts = 0;
        var policy = new PollyDeadlockRetryPolicy(FastOptions());

        await Assert.ThrowsAsync<SqlException>(async () =>
        {
            await policy.ExecuteAsync<int>(() =>
            {
                attempts++;
                throw SqlExceptionHelper.CreateSqlException(208, "Invalid object name");
            });
        });

        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task ExecuteAsync_WhenMaxRetriesExceeded_PropagatesSqlException()
    {
        var options = FastOptions(maxRetries: 2);
        var policy = new PollyDeadlockRetryPolicy(options);

        var ex = await Assert.ThrowsAsync<SqlException>(async () =>
        {
            await policy.ExecuteAsync<int>(() =>
                throw SqlExceptionHelper.CreateDeadlockException());
        });

        Assert.Equal(1205, ex.Number);
    }

    [Fact]
    public async Task ExecuteAsync_OnRetry_InvokesOnRetryCallback()
    {
        var retryCallbackArgs = new List<(int retryCount, TimeSpan delay)>();
        var options = FastOptions(maxRetries: 3);
        options.OnRetry = (count, delay) => retryCallbackArgs.Add((count, delay));
        var policy = new PollyDeadlockRetryPolicy(options);
        var attempts = 0;

        await policy.ExecuteAsync(async () =>
        {
            attempts++;
            if (attempts < 3)
                throw SqlExceptionHelper.CreateDeadlockException();
        });

        Assert.Equal(2, retryCallbackArgs.Count);
        Assert.Equal(1, retryCallbackArgs[0].retryCount);
        Assert.Equal(2, retryCallbackArgs[1].retryCount);
    }

    // ─── ExecuteAsync (void) ────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_Void_OnSuccess_Completes()
    {
        var executed = false;
        var policy = new PollyDeadlockRetryPolicy(FastOptions());

        await policy.ExecuteAsync(async () => { executed = true; });

        Assert.True(executed);
    }

    [Fact]
    public async Task ExecuteAsync_Void_OnDeadlock_RetriesAndSucceeds()
    {
        var attempts = 0;
        var policy = new PollyDeadlockRetryPolicy(FastOptions(maxRetries: 5));

        await policy.ExecuteAsync(async () =>
        {
            attempts++;
            if (attempts < 3)
                throw SqlExceptionHelper.CreateDeadlockException();
        });

        Assert.Equal(3, attempts);
    }

    // ─── Execute<T> (sync) ──────────────────────────────────────────────────────

    [Fact]
    public void Execute_OnSuccess_ReturnsSynchronously()
    {
        var policy = new PollyDeadlockRetryPolicy(FastOptions());

        var result = policy.Execute(() => 99);

        Assert.Equal(99, result);
    }
}
