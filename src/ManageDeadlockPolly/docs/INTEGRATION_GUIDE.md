# Guide d'Intégration - Architecture Polly Réutilisable

## Vue d'ensemble

L'architecture de retry deadlock a été refactorisée pour une **réutilisabilité maximale** dans des projets applicatifs réels (API, Microservices, Workers, etc).

### Couches de l'architecture

```
┌─────────────────────────────────────────────────────────────┐
│  Couche Application (API Controllers, Services, Workers)   │
│         └─ Injecte ITransactionalExecutor                   │
├─────────────────────────────────────────────────────────────┤
│  Couche Abstraction Transactionnelle                        │
│         ├─ ITransactionalExecutor (orchestration)           │
│         ├─ IDbConnectionProvider (connexions)               │
│         └─ IDeadlockRetryPolicy (stratégies)                │
├─────────────────────────────────────────────────────────────┤
│  Couche Implémentation                                       │
│         ├─ TransactionalExecutor (impl générique)           │
│         ├─ SqlServerConnectionProvider (SQL Server)         │
│         └─ PollyDeadlockRetryPolicy (Polly)                 │
├─────────────────────────────────────────────────────────────┤
│  Couche Infrastructure                                       │
│         └─ Microsoft.Data.SqlClient + Polly                 │
└─────────────────────────────────────────────────────────────┘
```

**Avantage:** Chaque couche est testable, remplaçable et découplée.

---

## Utilisation dans un ASP.NET Core API

### 1. Configuration au démarrage (Program.cs)

```csharp
using ManageDeadlockPolly.Extensions;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Configuration simple
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDeadlockRetryStack(connectionString, options =>
{
    options.MaxRetries = 5;
    options.InitialDelayMs = 100;
    options.MaxJitterMs = 50;
    options.OnRetry = (retryCount, delay) =>
    {
        Console.WriteLine($"[RETRY {retryCount}] Deadlock, retry in {delay.TotalMilliseconds:F0}ms");
    };
});

builder.Services.AddScoped<IOrderRepository, OrderRepository>();

var app = builder.Build();
app.MapControllers();
app.Run();
```

### 2. Utilisation dans un Repository ou Service

```csharp
using ManageDeadlockPolly.DataAccess;
using Dapper;

public interface IOrderRepository
{
    Task<Order> CreateOrderAsync(CreateOrderRequest request);
    Task UpdateOrderAsync(int orderId, UpdateOrderRequest request);
}

public class OrderRepository : IOrderRepository
{
    private readonly ITransactionalExecutor _executor;

    public OrderRepository(ITransactionalExecutor executor)
    {
        _executor = executor;
    }

    public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        return await _executor.ExecuteAsync(async (conn, tx) =>
        {
            // Logique métier : créer la commande + mettre à jour l'inventaire
            var orderId = await conn.ExecuteScalarAsync<int>(
                "INSERT INTO Orders (CustomerId, Total) VALUES (@CustomerId, @Total); SELECT SCOPE_IDENTITY();",
                new { request.CustomerId, request.Total },
                transaction: tx);

            // Simuler une logique pouvant générer un deadlock
            await Task.Delay(100);

            foreach (var line in request.Lines)
            {
                await conn.ExecuteAsync(
                    "UPDATE Inventory SET Quantity = Quantity - @Qty WHERE ProductId = @ProductId",
                    new { Qty = line.Quantity, line.ProductId },
                    transaction: tx);
            }

            return new Order { Id = orderId, CustomerId = request.CustomerId, Total = request.Total };
        });
    }

    public async Task UpdateOrderAsync(int orderId, UpdateOrderRequest request)
    {
        await _executor.ExecuteAsync(async (conn, tx) =>
        {
            await conn.ExecuteAsync(
                "UPDATE Orders SET Status = @Status WHERE Id = @Id",
                new { Status = request.Status, Id = orderId },
                transaction: tx);

            // Polly retry automatique en cas de deadlock
        });
    }
}
```

### 3. Utilisation dans un contrôleur

```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderRepository _repository;

    public OrdersController(IOrderRepository repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<ActionResult<Order>> CreateOrder([FromBody] CreateOrderRequest request)
    {
        try
        {
            var order = await _repository.CreateOrderAsync(request);
            return CreatedAtAction(nameof(CreateOrder), new { id = order.Id }, order);
        }
        catch (Exception ex)
        {
            // ITransactionalExecutor gère le retry, vous ne devriez pas arriver ici pour un deadlock
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }
}
```

---

## Utilisation dans un Background Worker / Service

```csharp
using ManageDeadlockPolly.DataAccess;
using ManageDeadlockPolly.Extensions;

class Program
{
    static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                var connectionString = context.Configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException("Connection string not found");

                // Même configuration que dans ASP.NET Core
                services.AddDeadlockRetryStack(connectionString, options =>
                {
                    options.MaxRetries = 3;
                    options.OnRetry = (retryCount, delay) =>
                        Console.WriteLine($"Worker: Retry {retryCount} in {delay.TotalMilliseconds}ms");
                });

                services.AddScoped<ISyncWorker, SyncWorker>();
                services.AddHostedService<BackgroundSyncService>();
            })
            .Build();

        await host.RunAsync();
    }
}

public class BackgroundSyncService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public BackgroundSyncService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var worker = scope.ServiceProvider.GetRequiredService<ISyncWorker>();
            
            try
            {
                await worker.SyncDataAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Sync failed after retries: {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}

public class SyncWorker : ISyncWorker
{
    private readonly ITransactionalExecutor _executor;

    public SyncWorker(ITransactionalExecutor executor)
    {
        _executor = executor;
    }

    public async Task SyncDataAsync()
    {
        await _executor.ExecuteAsync(async (conn, tx) =>
        {
            // Logique de synchronisation
            var records = await conn.QueryAsync(
                "SELECT * FROM SyncQueue WHERE Status = 'Pending'",
                transaction: tx);

            foreach (var record in records)
            {
                // Traitement qui peut causer des deadlocks
                await conn.ExecuteAsync(
                    "UPDATE SyncQueue SET Status = 'Processed' WHERE Id = @Id",
                    new { Id = record.Id },
                    transaction: tx);
            }
        });
    }
}
```

---

## Utilisation avec Tests Unitaires

### Créer un Mock de ITransactionalExecutor

```csharp
using Moq;
using ManageDeadlockPolly.DataAccess;
using Xunit;

public class OrderRepositoryTests
{
    [Fact]
    public async Task CreateOrder_ShouldCallExecutorWithCorrectAction()
    {
        // Arrange
        var mockExecutor = new Mock<ITransactionalExecutor>();
        
        // Simuler le comportement de l'executor
        mockExecutor
            .Setup(e => e.ExecuteAsync(It.IsAny<Func<IDbConnection, IDbTransaction, Task<Order>>>(), It.IsAny<IsolationLevel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Order { Id = 1, CustomerId = 123, Total = 99.99m });

        var repository = new OrderRepository(mockExecutor.Object);

        // Act
        var result = await repository.CreateOrderAsync(new CreateOrderRequest { CustomerId = 123, Total = 99.99m });

        // Assert
        Assert.Equal(1, result.Id);
        mockExecutor.Verify(e => e.ExecuteAsync(It.IsAny<Func<IDbConnection, IDbTransaction, Task<Order>>>(), It.IsAny<IsolationLevel>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

---

## Configuration Avancée

### Option 1: Enregistrer une implémentation custom de IDeadlockRetryPolicy

```csharp
services.AddSingleton<IDeadlockRetryPolicy>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<MyCustomRetryPolicy>>();
    return new MyCustomRetryPolicy(logger);
});
```

### Option 2: Créer un fournisseur de connexion personnalisé

```csharp
public class PostgreSqlConnectionProvider : IDbConnectionProvider
{
    private readonly string _connectionString;

    public PostgreSqlConnectionProvider(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IDbConnection> CreateAndOpenAsync(CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    public async Task<IDbTransaction> BeginTransactionAsync(
        IDbConnection connection,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
    {
        // Implémentation PostgreSQL
        return (await connection.BeginTransactionAsync(isolationLevel, cancellationToken))!;
    }
}

// Enregistrement
services.AddScoped<IDbConnectionProvider>(
    _ => new PostgreSqlConnectionProvider(connectionString));
```

---

## Avantages de cette architecture

✅ **Découplage**: Chaque composant a un rôle bien défini  
✅ **Testabilité**: Toutes les interfaces sont mockables  
✅ **Réutilisabilité**: Utilisable dans n'importe quel projet .NET  
✅ **Extensibilité**: Easy to swap implementations (Polly → autre, SQL → Postgres, etc)  
✅ **Configuration**: Options validées et flexibles  
✅ **Monitoring**: Callbacks pour logging/metrics  

---

## Fichiers d'implémentation

- `RetryPolicies/IDeadlockRetryPolicy.cs` - Interface de stratégie retry
- `RetryPolicies/PollyDeadlockRetryPolicy.cs` - Implémentation Polly
- `RetryPolicies/DeadlockRetryPolicyOptions.cs` - Configuration options
- `DataAccess/IDbConnectionProvider.cs` - Interface fournisseur de connexion
- `DataAccess/SqlServerConnectionProvider.cs` - Implémentation SQL Server
- `DataAccess/ITransactionalExecutor.cs` - Interface orchestration transactionnelle
- `DataAccess/TransactionalExecutor.cs` - Implémentation générique
- `Extensions/ServiceCollectionExtensions.cs` - Extensions DI
