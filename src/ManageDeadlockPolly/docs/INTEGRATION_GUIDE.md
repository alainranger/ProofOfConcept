# Guide d'Integration

Integrer `DeadlockPolly.Core` dans votre projet .NET existant.

---

## 1. Ajouter la Reference

### Via fichier .csproj

```xml
<ItemGroup>
  <ProjectReference Include="path/to/DeadlockPolly.Core/DeadlockPolly.Core.csproj" />
</ItemGroup>
```

### Via NuGet (si package publie)

```bash
dotnet add package DeadlockPolly.Core
```

---

## 2. Enregistrer les Services

```csharp
using DeadlockPolly.Core.Extensions;

// Dans Program.cs ou Startup.cs
builder.Services.AddDeadlockRetryStack(
    connectionString: builder.Configuration.GetConnectionString("DefaultConnection")!,
    configureRetry: opt =>
    {
        opt.MaxRetries = 5;
        opt.BaseDelayMs = 100;
        opt.MaxDelayMs = 3000;
        opt.OnRetry = (attempt, delay) =>
            logger.LogWarning(
                "Deadlock detecte, tentative {Attempt} dans {Delay}ms",
                attempt,
                delay.TotalMilliseconds);
    });
```

---

## 3. Injecter ITransactionalExecutor

### Dans un Repository

```csharp
using DeadlockPolly.Core.DataAccess;

public class OrderRepository(ITransactionalExecutor executor)
{
    private const string InsertSql = @"
        INSERT INTO Orders (CustomerId, Total, CreatedAt)
        OUTPUT INSERTED.Id
        VALUES (@CustomerId, @Total, @CreatedAt)";

    public async Task<int> CreateOrderAsync(Order order)
    {
        return await executor.ExecuteAsync(async (conn, tx) =>
        {
            return await conn.ExecuteScalarAsync<int>(InsertSql, order, tx);
        });
    }
}
```

### Dans un Service

```csharp
using DeadlockPolly.Core.DataAccess;

public class InventoryService(ITransactionalExecutor executor)
{
    public async Task TransferStockAsync(int fromId, int toId, int quantity)
    {
        await executor.ExecuteAsync(async (conn, tx) =>
        {
            await conn.ExecuteAsync(
                "UPDATE Stock SET Quantity -= @qty WHERE ProductId = @id",
                new { qty = quantity, id = fromId }, tx);

            await conn.ExecuteAsync(
                "UPDATE Stock SET Quantity += @qty WHERE ProductId = @id",
                new { qty = quantity, id = toId }, tx);

            return true;
        });
    }
}
```

---

## 4. Integration ASP.NET Core

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDeadlockRetryStack(
    connectionString: builder.Configuration.GetConnectionString("Sql")!,
    configureRetry: opt => opt.MaxRetries = 3);

builder.Services.AddScoped<OrderRepository>();

var app = builder.Build();

app.MapPost("/orders", async (OrderRepository repo, Order order) =>
{
    var id = await repo.CreateOrderAsync(order);
    return Results.Created($"/orders/{id}", new { Id = id });
});

app.Run();
```

---

## 5. Integration Worker Service

```csharp
// Worker.cs
public class StockSyncWorker(ITransactionalExecutor executor, ILogger<StockSyncWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await executor.ExecuteAsync(async (conn, tx) =>
            {
                await conn.ExecuteAsync("UPDATE Stock SET LastSync = GETUTCDATE()", tx);
                return 0;
            });

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
```

---

## 6. Configuration Avancee

### Retry Exponentiel avec Jitter

```csharp
opt.MaxRetries = 5;
opt.BaseDelayMs = 50;    // 1er retry ~50-150ms
opt.MaxDelayMs = 5000;   // Plafond a 5 secondes
```

### Logging Detaille

```csharp
opt.OnRetry = (attempt, delay) =>
{
    logger.LogWarning(
        "[DEADLOCK] Tentative {Attempt}/{MaxRetries} - Attente {Delay}ms",
        attempt,
        opt.MaxRetries,
        (int)delay.TotalMilliseconds);
};
```

### Test avec Retry Rapide

```csharp
// Dans les tests d'integration
services.AddDeadlockRetryStack(
    connectionString: testConnectionString,
    configureRetry: opt =>
    {
        opt.MaxRetries = 2;
        opt.BaseDelayMs = 10;   // Delais tres courts pour les tests
        opt.MaxDelayMs = 50;
    });
```

---

## 7. Namespaces de Reference

```csharp
using DeadlockPolly.Core.DataAccess;       // ITransactionalExecutor, IDbConnectionProvider
using DeadlockPolly.Core.Extensions;       // AddDeadlockRetryStack
using DeadlockPolly.Core.RetryPolicies;   // IDeadlockRetryPolicy, DeadlockRetryPolicyOptions
using DeadlockPolly.Core.Repositories;   // DeadlockTestRecord, DeadlockTestRepository (demo)
```

---

## 8. Checklist d'Integration

Voir [INTEGRATION_CHECKLIST.md](./INTEGRATION_CHECKLIST.md) pour la liste complete.

---

**Voir aussi -> [MANUAL_TEST_GUIDE.md](./MANUAL_TEST_GUIDE.md)**
