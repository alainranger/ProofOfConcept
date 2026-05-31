using System.Data;
using Microsoft.Data.SqlClient;

namespace DeadlockPolly.Core.DataAccess;

/// <summary>
/// Implémentation SQL Server de IDbConnectionProvider.
/// </summary>
public class SqlServerConnectionProvider : IDbConnectionProvider
{
    private readonly string _connectionString;

    public SqlServerConnectionProvider(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string ne peut pas être vide", nameof(connectionString));

        _connectionString = connectionString;
    }

    public async Task<IDbConnection> CreateAndOpenAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    public async Task<IDbTransaction> BeginTransactionAsync(
        IDbConnection connection,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
    {
        if (connection is not SqlConnection sqlConnection)
            throw new ArgumentException("Connection doit être un SqlConnection", nameof(connection));

        return (await sqlConnection.BeginTransactionAsync(isolationLevel, cancellationToken))!;
    }
}
