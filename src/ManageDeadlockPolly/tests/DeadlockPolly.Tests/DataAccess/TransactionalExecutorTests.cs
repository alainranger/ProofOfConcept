using System.Data;
using DeadlockPolly.Core.DataAccess;
using DeadlockPolly.Core.RetryPolicies;
using Moq;

namespace DeadlockPolly.Tests.DataAccess;

public class TransactionalExecutorTests
{
    private readonly Mock<IDbConnectionProvider> _providerMock = new();
    private readonly Mock<IDbConnection> _connectionMock = new();
    private readonly Mock<IDbTransaction> _transactionMock = new();

    // Politique passthrough : exécute l'action directement, sans retry
    private sealed class PassThroughRetryPolicy : IDeadlockRetryPolicy
    {
        public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct = default)
            => await action();

        public async Task ExecuteAsync(Func<Task> action, CancellationToken ct = default)
            => await action();

        public T Execute<T>(Func<T> action) => action();
    }

    public TransactionalExecutorTests()
    {
        _providerMock
            .Setup(p => p.CreateAndOpenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_connectionMock.Object);

        _providerMock
            .Setup(p => p.BeginTransactionAsync(
                _connectionMock.Object,
                It.IsAny<IsolationLevel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);
    }

    private TransactionalExecutor CreateExecutor()
        => new(_providerMock.Object, new PassThroughRetryPolicy());

    // ─── Constructor guards ──────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithNullConnectionProvider_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new TransactionalExecutor(null!, new PassThroughRetryPolicy()));
    }

    [Fact]
    public void Constructor_WithNullRetryPolicy_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new TransactionalExecutor(_providerMock.Object, null!));
    }

    // ─── ExecuteAsync<T> ────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_OnSuccess_ReturnsActionResult()
    {
        var executor = CreateExecutor();

        var result = await executor.ExecuteAsync((_, _) => Task.FromResult(42));

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task ExecuteAsync_OnSuccess_CommitsTransaction()
    {
        var executor = CreateExecutor();

        await executor.ExecuteAsync((_, _) => Task.FromResult(0));

        _transactionMock.Verify(t => t.Commit(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_OnSuccess_NeverRollsBack()
    {
        var executor = CreateExecutor();

        await executor.ExecuteAsync((_, _) => Task.FromResult(0));

        _transactionMock.Verify(t => t.Rollback(), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_OnSuccess_DisposesConnectionAndTransaction()
    {
        var executor = CreateExecutor();

        await executor.ExecuteAsync((_, _) => Task.FromResult(0));

        _transactionMock.Verify(t => t.Dispose(), Times.Once);
        _connectionMock.Verify(c => c.Dispose(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_OnFailure_RollsBackTransaction()
    {
        var executor = CreateExecutor();

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await executor.ExecuteAsync<int>((_, _) =>
                throw new InvalidOperationException("boom")));

        _transactionMock.Verify(t => t.Rollback(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_OnFailure_NeverCommits()
    {
        var executor = CreateExecutor();

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await executor.ExecuteAsync<int>((_, _) =>
                throw new InvalidOperationException("boom")));

        _transactionMock.Verify(t => t.Commit(), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_OnFailure_DisposesConnectionAndTransaction()
    {
        var executor = CreateExecutor();

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await executor.ExecuteAsync<int>((_, _) =>
                throw new InvalidOperationException("boom")));

        _transactionMock.Verify(t => t.Dispose(), Times.Once);
        _connectionMock.Verify(c => c.Dispose(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_OnFailure_PropagatesException()
    {
        var executor = CreateExecutor();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await executor.ExecuteAsync<int>((_, _) =>
                throw new InvalidOperationException("original")));

        Assert.Equal("original", ex.Message);
    }

    // ─── ExecuteAsync (void overload) ───────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_Void_OnSuccess_CommitsTransaction()
    {
        var executor = CreateExecutor();
        var called = false;

        await executor.ExecuteAsync((_, _) => { called = true; return Task.CompletedTask; });

        Assert.True(called);
        _transactionMock.Verify(t => t.Commit(), Times.Once);
    }

    // ─── Execute<T> (sync) ──────────────────────────────────────────────────────

    [Fact]
    public void Execute_OnSuccess_ReturnsResult()
    {
        var executor = CreateExecutor();

        var result = executor.Execute((_, _) => 77);

        Assert.Equal(77, result);
        _transactionMock.Verify(t => t.Commit(), Times.Once);
    }

    // ─── IsolationLevel is forwarded ────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_UsesSpecifiedIsolationLevel()
    {
        var executor = CreateExecutor();

        await executor.ExecuteAsync(
            (_, _) => Task.FromResult(0),
            IsolationLevel.Serializable);

        _providerMock.Verify(p =>
            p.BeginTransactionAsync(
                _connectionMock.Object,
                IsolationLevel.Serializable,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
