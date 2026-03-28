using ManageDeadlockPolly.DataAccess;
using ManageDeadlockPolly.RetryPolicies;
using Microsoft.Data.SqlClient;

namespace ManageDeadlockPolly;

internal static class Program
{
    private const string DefaultConnectionString = "Server=localhost,1433;Database=DeadlockTestDb;User Id=sa;Password=YourStrong!Pass2024;Encrypt=false;TrustServerCertificate=true;Connection Timeout=30;";

    private static async Task Main()
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionString") ?? DefaultConnectionString;

        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("  Démo Polly + Dapper - Gestion des Deadlocks");
        Console.WriteLine("═══════════════════════════════════════════════════════════\n");

        await EnsureDatabaseReadyAsync(connectionString);
        await InitializeDatabaseAsync(connectionString);

        var retryPolicy = new PollyDeadlockRetryPolicy(new DeadlockRetryPolicyOptions
        {
            MaxRetries = 5,
            OnRetry = (retryCount, delay) =>
                Console.WriteLine($"[RETRY {retryCount}] Deadlock détecté, retry dans {delay.TotalMilliseconds:F0}ms")
        });
        var connectionProvider = new SqlServerConnectionProvider(connectionString);
        var executor = new TransactionalExecutor(connectionProvider, retryPolicy);
        var repository = new DeadlockTestRepository(executor);

        await ResetAndPrintInitialStateAsync(repository);
        await RunSimpleScenarioAsync(repository);
        await repository.ResetAsync();
        await RunConcurrentScenarioAsync(repository);

        Console.WriteLine("\n\n═══════════════════════════════════════════════════════════");
        Console.WriteLine("  Tests terminés");
        Console.WriteLine("═══════════════════════════════════════════════════════════");
    }

    private static async Task ResetAndPrintInitialStateAsync(DeadlockTestRepository repository)
    {
        Console.WriteLine("-> Réinitialisation des données...");
        await repository.ResetAsync();

        Console.WriteLine("\nÉtat initial:");
        await PrintValuesAsync(repository);
    }

    private static async Task RunSimpleScenarioAsync(DeadlockTestRepository repository)
    {
        Console.WriteLine("\n\n1. Test: opération simple");
        Console.WriteLine("-".PadRight(50, '-'));

        try
        {
            var updatedCount = await repository.IncrementBothValuesAsync();
            Console.WriteLine($"Succes: {updatedCount} enregistrements mis à jour");
            Console.WriteLine("Etat après opération:");
            await PrintValuesAsync(repository);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur: {ex.Message}");
        }
    }

    private static async Task RunConcurrentScenarioAsync(DeadlockTestRepository repository)
    {
        Console.WriteLine("\n\n2. Test: concurrence avec deadlock");
        Console.WriteLine("-".PadRight(50, '-'));

        try
        {
            var task1 = repository.IncrementBothValuesAsync();
            await Task.Delay(100);
            var task2 = repository.IncrementBothValuesReverseOrderAsync();

            var results = await Task.WhenAll(task1, task2);
            Console.WriteLine("Succes: les deux opérations se sont terminées après retry");
            Console.WriteLine($"Task1: {results[0]} updates, Task2: {results[1]} updates");
            Console.WriteLine("Etat final:");
            await PrintValuesAsync(repository);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur non gérée: {ex.Message}");
        }
    }

    private static async Task PrintValuesAsync(DeadlockTestRepository repository)
    {
        var values = await repository.GetValuesAsync();
        foreach (var record in values)
        {
            Console.WriteLine($"  Id={record.Id}, Value={record.Value}");
        }
    }

    private static async Task EnsureDatabaseReadyAsync(string connectionString)
    {
        const int maxAttempts = 30;
        const int delayMs = 1000;
        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master"
        };

        Console.WriteLine("⏳ Attente de SQL Server...");

        for (int i = 0; i < maxAttempts; i++)
        {
            try
            {
                using var conn = new SqlConnection(builder.ConnectionString);
                await conn.OpenAsync();
                Console.WriteLine("Connexion à SQL Server établie");
                return;
            }
            catch (Exception ex)
            {
                if (i < maxAttempts - 1)
                {
                    Console.Write(".");
                    await Task.Delay(delayMs);
                }
                else
                {
                    throw new InvalidOperationException("SQL Server n'a pas répondu après 30 tentatives", ex);
                }
            }
        }
    }

    private static async Task InitializeDatabaseAsync(string connectionString)
    {
        const string createDatabaseSql = """
            IF DB_ID('DeadlockTestDb') IS NULL
            BEGIN
                CREATE DATABASE DeadlockTestDb;
            END
            """;

        const string createSchemaSql = """
            IF OBJECT_ID('dbo.DeadlockTest', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.DeadlockTest
                (
                    Id INT NOT NULL PRIMARY KEY,
                    Value INT NOT NULL DEFAULT 0,
                    LastUpdated DATETIME2 NOT NULL DEFAULT GETUTCDATE()
                );

                INSERT INTO dbo.DeadlockTest (Id, Value)
                VALUES (1, 0), (2, 0);

                CREATE NONCLUSTERED INDEX IX_DeadlockTest_Value
                    ON dbo.DeadlockTest (Value);
            END
            """;

        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master"
        };

        await using (var masterConnection = new SqlConnection(builder.ConnectionString))
        {
            await masterConnection.OpenAsync();
            await using var createDatabaseCommand = new SqlCommand(createDatabaseSql, masterConnection);
            await createDatabaseCommand.ExecuteNonQueryAsync();
        }

        await using (var appConnection = new SqlConnection(connectionString))
        {
            await appConnection.OpenAsync();
            await using var createSchemaCommand = new SqlCommand(createSchemaSql, appConnection);
            await createSchemaCommand.ExecuteNonQueryAsync();
        }

        Console.WriteLine("Base de données et schéma prêts");
    }
}
