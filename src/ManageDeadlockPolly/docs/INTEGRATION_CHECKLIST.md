## 📋 Checklist d'Intégration

Utilise cette checklist pour intégrer Polly + Dapper + Deadlock Retry dans ton projet existant.

---

### ✅ Étape 1: Dépendances

```bash
dotnet add package Polly --version 8.4.2
dotnet add package Polly.Core --version 8.4.2
dotnet add package Dapper --version 2.1.15
dotnet add package Microsoft.Data.SqlClient --version 5.1.5
```

**Fichier csproj** doit contenir:

```xml
<ItemGroup>
    <PackageReference Include="Dapper" Version="2.1.15" />
    <PackageReference Include="Microsoft.Data.SqlClient" Version="5.1.5" />
    <PackageReference Include="Polly" Version="8.4.2" />
    <PackageReference Include="Polly.Core" Version="8.4.2" />
</ItemGroup>
```

---

### ✅ Étape 2: Classe Service (Copier-coller DeadlockRetryService.cs)

Place dans ton projet:

```
src/
  Services/
    DeadlockRetryService.cs
```

Adapter le namespace si besoin.

---

### ✅ Étape 3: Dependency Injection (Si tu utilises DI)

```csharp
// Startup.cs ou Program.cs
services.AddSingleton(new DeadlockRetryService(connectionString, maxRetries: 5));
services.AddScoped<MyRepository>();
```

Puis dans le Repository:

```csharp
public class MyRepository
{
    private readonly DeadlockRetryService _retryService;

    public MyRepository(DeadlockRetryService retryService)
    {
        _retryService = retryService;
    }

    public async Task<bool> UpdateAsync(int id, string newValue)
    {
        return await _retryService.ExecuteWithDeadlockRetryAsync(
            async (conn, tx) =>
            {
                await conn.ExecuteAsync(
                    "UPDATE MyTable SET Value = @Value WHERE Id = @Id",
                    new { Id = id, Value = newValue },
                    transaction: tx
                );
                return true;
            }
        );
    }
}
```

---

### ✅ Étape 4: Logging (Remplacer Console.WriteLine)

Ajouter au `DeadlockRetryService`:

```csharp
using Microsoft.Extensions.Logging;

public class DeadlockRetryService
{
    private readonly ILogger<DeadlockRetryService> _logger;

    public DeadlockRetryService(string connectionString, ILogger<DeadlockRetryService> logger, int maxRetries = 5)
    {
        _connectionString = connectionString;
        _logger = logger;
        _deadlockRetryPolicy = BuildDeadlockRetryPolicy(maxRetries);
    }

    private IAsyncPolicy<T> BuildDeadlockRetryPolicy<T>(int maxRetries)
    {
        return Policy
            .Handle<SqlException>(ex => ex.Number == 1205)
            .OrResult<T>(r => false)
            .WaitAndRetryAsync<T>(
                retryCount: maxRetries,
                sleepDurationProvider: retryAttempt =>
                {
                    var baseDelay = TimeSpan.FromMilliseconds(100 * Math.Pow(2, retryAttempt - 1));
                    var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 50));
                    return baseDelay.Add(jitter);
                },
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        $"Deadlock détecté (attempt {retryCount}), retry dans {timespan.TotalMilliseconds:F0}ms"
                    );
                }
            );
    }

    // ... reste du code
}
```

DI avec Serilog (exemple):

```csharp
// Program.cs
services.AddLogging(config =>
{
    config.ClearProviders();
    config.AddSerilog(new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day)
        .CreateLogger());
});

services.AddSingleton(sp => new DeadlockRetryService(
    connectionString,
    sp.GetRequiredService<ILogger<DeadlockRetryService>>(),
    maxRetries: 5
));
```

---

### ✅ Étape 5: Monitoring & Telemetry (Optionnel)

Ajouter Application Insights:

```csharp
// Program.cs
services.AddApplicationInsightsTelemetry();
```

Modifier `DeadlockRetryService`:

```csharp
using Microsoft.ApplicationInsights;

public class DeadlockRetryService
{
    private readonly TelemetryClient _telemetryClient;

    public DeadlockRetryService(
        string connectionString,
        ILogger<DeadlockRetryService> logger,
        TelemetryClient telemetryClient,
        int maxRetries = 5)
    {
        _connectionString = connectionString;
        _logger = logger;
        _telemetryClient = telemetryClient;
        // ...
    }

    private IAsyncPolicy<T> BuildDeadlockRetryPolicy<T>(int maxRetries)
    {
        return Policy
            .Handle<SqlException>(ex => ex.Number == 1205)
            .OrResult<T>(r => false)
            .WaitAndRetryAsync<T>(
                retryCount: maxRetries,
                sleepDurationProvider: retryAttempt =>
                {
                    var baseDelay = TimeSpan.FromMilliseconds(100 * Math.Pow(2, retryAttempt - 1));
                    var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 50));
                    return baseDelay.Add(jitter);
                },
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning($"Deadlock detected (attempt {retryCount})");
                    
                    // Envoyer telemetry à AppInsights
                    _telemetryClient.TrackEvent("DeadlockRetry", new Dictionary<string, string>
                    {
                        { "Attempt", retryCount.ToString() },
                        { "DelayMs", timespan.TotalMilliseconds.ToString("F0") }
                    });
                }
            );
    }
}
```

---

### ✅ Étape 6: Tests Unitaires

```csharp
// Tests.cs
using Xunit;
using Moq;
using System.Data;

public class DeadlockRetryServiceTests
{
    [Fact]
    public async Task RetryOnDeadlock_ShouldSucceedEventually()
    {
        // Arrange
        var connectionString = "Server=...";
        var service = new DeadlockRetryService(connectionString, maxRetries: 3);
        
        var callCount = 0;
        
        // Act
        var result = await service.ExecuteWithDeadlockRetryAsync(async (conn, tx) =>
        {
            callCount++;
            if (callCount < 2)
                throw new SqlException(); // Simuler deadlock
            return "success";
        });

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task MaxRetriesExceeded_ShouldThrow()
    {
        // Arrange
        var connectionString = "Server=...";
        var service = new DeadlockRetryService(connectionString, maxRetries: 1);

        // Act & Assert
        await Assert.ThrowsAsync<SqlException>(async () =>
        {
            await service.ExecuteWithDeadlockRetryAsync(async (conn, tx) =>
            {
                throw new SqlException();
            });
        });
    }
}
```

---

### ✅ Étape 7: Configuration Recommandée (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=MyDb;User Id=sa;Password=***;Encrypt=false;TrustServerCertificate=true;"
  },
  "Polly": {
    "DeadlockRetry": {
      "MaxRetryAttempts": 5,
      "InitialDelayMs": 100,
      "BackoffMultiplier": 2,
      "MaxJitterMs": 50
    },
    "CircuitBreaker": {
      "FailureThreshold": 0.5,
      "MinimumThroughput": 4,
      "TimeoutSeconds": 30
    }
  }
}
```

Puis charger:

```csharp
// Program.cs
var config = builder.Configuration;
var pollyConfig = config.GetSection("Polly:DeadlockRetry");

services.AddSingleton(sp => new DeadlockRetryService(
    config.GetConnectionString("DefaultConnection")!,
    sp.GetRequiredService<ILogger<DeadlockRetryService>>(),
    maxRetries: int.Parse(pollyConfig["MaxRetryAttempts"] ?? "5")
));
```

---

## 🚨 Erreurs Courantes

| Problème | Cause | Solution |
|----------|-------|----------|
| `SqlException: timeout` | Requête trop lente | Ré-indexer, optimiser query, augmenter `Connection Timeout` |
| `Deadlock keeps happening` | Ordre d'accès inconsistent | Toujours accéder aux ressources dans le même ordre (ID croissant) |
| `Retry politique ne triggère pas` | Exception pas `SqlException` | Vérifier type d'exception exact, ajouter predicate pour autre types |
| `Performance dégradée après retry` | Backoff trop agressif | Réduire delays: `100ms * 1.5^attempt` au lieu de `2^attempt` |
| `Circuit breaker s'ouvre trop tôt` | Seuil trop bas | Augmenter `handledEventsAllowedBeforeBreaking` ou `MinimumThroughput` |
| `Deadlock pendant tests` | Transactions trop longues | Réduire scope de la transaction, éviter I/O dedans |

---

## 📈 Performance Tips

- **Transactions courtes**: 10-100ms idéal
- **Isolation Level**: `ReadCommitted` par défaut (bon compromis)
- **Indexes**: Créer sur colonnes WHERE/JOIN
- **Batch processing**: Regrouper updates pour réduire contexte-switches
- **Async everywhere**: Utiliser `async/await` pour ne pas bloquer threads
- **Connection pooling**: SQL Client pool automatique, vérifier `Min Pool Size` si besoin

---

## 🏆 Best Practices Résumé

```csharp
// ✅ BON
using (var tx = await conn.BeginTransactionAsync())
{
    await conn.ExecuteAsync("UPDATE A SET ...", tx: tx);
    await conn.ExecuteAsync("UPDATE B SET ...", tx: tx);
    // Tout dans l'ordre (A, puis B, toujours)
    await tx.CommitAsync();
}

// ❌ MAUVAIS
using (var tx = await conn.BeginTransactionAsync())
{
    await http.GetAsync(...); // Appel externe dans TX!
    await httpClient.PostAsync(...);
    await Task.Delay(5000); // Attente longue!
    await conn.ExecuteAsync("UPDATE ...", tx: tx);
}

// ❌ TRÈS MAUVAIS
// Tx 1: Update A, Update B
// Tx 2: Update B, Update A  ← Deadlock!
```

---

**Ready to integrate? Start with Step 1 → Step 7! 🚀**
