# ManageDeadlockPolly - Démonstration Polly + Dapper

Démonstration complète de **gestion des deadlocks SQL Server avec Polly et Dapper**.

## Architecture

```
┌─────────────────────────────────────────────────┐
│  .NET 8 Console App (Polly + Dapper)           │
├─────────────────────────────────────────────────┤
│  DeadlockRetryService                          │
│  └─ Retry policy sur erreur 1205               │
│     (Exponential backoff + Jitter)             │
│  DeadlockTestRepository                        │
│  └─ Opérations Update transactionnelles        │
└─────────────────────────────────────────────────┘
           ↓ (TCP 1433)
┌─────────────────────────────────────────────────┐
│  SQL Server 2022 (Docker)                      │
│  Database: DeadlockTestDb                      │
│  Table: dbo.DeadlockTest (Id, Value)           │
└─────────────────────────────────────────────────┘
```

## Démarrage

### 1. Prérequis

- Docker & Docker Compose
- .NET 8 SDK (optionnel, si build local)

### 2. Lancer l'application complète

```bash
# À la racine du projet
docker-compose up --build

# Ou juste le build
docker-compose build

# Puis démarrer
docker-compose up
```

**Attendez ~15-20 secondes** que SQL Server soit prêt et les tables initialisées.

## Documentation

- **Vue d'ensemble**: `docs/00-START-HERE.md`
- **Démarrage rapide**: `docs/QUICKSTART.md`
- **🆕 Architecture réutilisable**: `docs/ARCHITECTURE_SUMMARY.md`
- **🆕 Guide d'intégration (API/Workers/Tests)**: `docs/INTEGRATION_GUIDE.md`
- **Guide d'intégration (ancien)**: `docs/INTEGRATION_CHECKLIST.md`
- **Test manuel**: `docs/MANUAL_TEST_GUIDE.md`
- **Index**: `docs/INDEX.md`

### 3. Résultat attendu

```
═══════════════════════════════════════════════════════════
  Démo Polly + Dapper - Gestion des Deadlocks
═══════════════════════════════════════════════════════════

⏳ Attente de SQL Server...
✓ Connexion à SQL Server établie

-> Réinitialisation des données...
[TX] Transaction ouverte (Isolation: ReadCommitted)
[TX] Transaction validée ✓

📊 État initial:
   Id=1, Value=0
   Id=2, Value=0

1️⃣  Test 1: Opération normale (sans deadlock concurrent)
--------------------------------------------------
[TX] Transaction ouverte (Isolation: ReadCommitted)
[TX] Transaction validée ✓
✓ 2 enregistrements mis à jour

📊 Après opération:
   Id=1, Value=1
   Id=2, Value=1

2️⃣  Test 2: Avec deadlock concurrent (2 tâches concurrentes)
--------------------------------------------------
[TX] Transaction ouverte (Isolation: ReadCommitted)
[TX] Transaction ouverte (Isolation: ReadCommitted)
[RETRY 1] Deadlock détecté, retry dans 156ms
[TX] Transaction ouverte (Isolation: ReadCommitted)
[TX] Transaction validée ✓
[TX] Transaction validée ✓
✓ Deux opérations terminées avec succès malgré le deadlock
  Task1: 2 updates, Task2: 2 updates

📊 État final:
   Id=1, Value=3
   Id=2, Value=3
```

## Code Principal

### DeadlockRetryService.cs

**Classe clé**: Encapsule la logique Polly + Dapper.

```csharp
// Construire la service
var retryService = new DeadlockRetryService(connectionString, maxRetries: 5);

// Utiliser dans une opération transactionnelle
var result = await retryService.ExecuteWithDeadlockRetryAsync(
    async (conn, tx) =>
    {
        await conn.ExecuteAsync("UPDATE ...", params, tx);
        // Toute la tx est rejoué en cas de deadlock
        return count;
    }
);
```

**Que fait Polly?**

- ✓ Détecte `SqlException` avec `Number == 1205` (deadlock victim)
- ✓ Retry jusqu'à 5 fois avec **Exponential Backoff** (100ms, 200ms, 400ms, ...)
- ✓ **Jitter** aléatoire pour éviter les retry synchronized
- ✓ **Rejoue toute la transaction** (reconnexion + retx)
- ✓ Log chaque tentative

### DeadlockRetryPolicy

```csharp
Policy
    .Handle<SqlException>(ex => ex.Number == 1205)
    .WaitAndRetryAsync<T>(
        retryCount: 5,
        sleepDurationProvider: retryAttempt => 
            TimeSpan.FromMilliseconds(100 * Math.Pow(2, retryAttempt - 1))
                .Add(TimeSpan.FromMilliseconds(Random.Shared.Next(0, 50))),
        onRetry: (outcome, timespan, retryCount, context) =>
            Console.WriteLine($"[RETRY {retryCount}] Deadlock, retry dans {timespan.TotalMilliseconds}ms")
    )
    .Build();
```

### Opération Transactionnelle

```csharp
public async Task<int> IncrementBothValuesAsync()
{
    return await _retryService.ExecuteWithDeadlockRetryAsync(
        async (conn, tx) =>
        {
            var updated1 = await conn.ExecuteAsync(
                "UPDATE dbo.DeadlockTest SET Value = Value + 1 WHERE Id = @Id",
                new { Id = 1 },
                transaction: tx
            );
            
            await Task.Delay(500); // Fenêtre de contention
            
            var updated2 = await conn.ExecuteAsync(
                "UPDATE dbo.DeadlockTest SET Value = Value + 1 WHERE Id = @Id",
                new { Id = 2 },
                transaction: tx
            );
            
            return updated1 + updated2;
        }
    );
}
```

## Tests Manuels

### Vérifier les logs de deadlock SQL

```bash
# Se connecter au conteneur SQL
docker exec -it <sql-container-id> /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P 'YourStrong!Pass2024'

# Puis lancer la query manuelle d'un deadlock:
USE DeadlockTestDb;

-- Session A (exécuter d'abord)
BEGIN TRAN;
UPDATE dbo.DeadlockTest SET Value = Value + 1 WHERE Id = 1;
WAITFOR DELAY '00:00:02';
UPDATE dbo.DeadlockTest SET Value = Value + 1 WHERE Id = 2;
COMMIT TRAN;

-- En parallèle, Session B
BEGIN TRAN;
UPDATE dbo.DeadlockTest SET Value = Value + 1 WHERE Id = 2;
WAITFOR DELAY '00:00:02';
UPDATE dbo.DeadlockTest SET Value = Value + 1 WHERE Id = 1;
COMMIT TRAN;
```

L'une des sessions doit retourner: **Msg 1205, Level 13, State 13 - Deadlock**

### Arrêter les conteneurs

```bash
docker-compose down

# Supprimer les volumes (reset complet)
docker-compose down -v
```

## Points d'Optimisation

### 1. Réduire la fenêtre de deadlock

❌ **Mauvais**: Opérations longues dans la transaction

```csharp
BEGIN TRAN;
UPDATE dbo.A SET ...;
await http.GetAsync(...);  // 5 secondes!
UPDATE dbo.B SET ...;
COMMIT TRAN;
```

✅ **Bon**: Transaction courte et déterministe

```csharp
BEGIN TRAN;
UPDATE dbo.A SET ...;
UPDATE dbo.B SET ...;
COMMIT TRAN;
// Appel HTTP après
```

### 2. Ordre d'accès consistent

❌ **Crée des deadlocks**:

```csharp
// Transaction 1
UPDATE dbo.A WHERE Id = 1;
UPDATE dbo.B WHERE Id = 2;

// Transaction 2
UPDATE dbo.B WHERE Id = 2;
UPDATE dbo.A WHERE Id = 1;  // Circulaire = deadlock
```

✅ **Évite les deadlocks**:

```csharp
// Toujours le même ordre
UPDATE dbo.A WHERE Id = ... ORDER BY Id;  // Croissant
UPDATE dbo.B WHERE Id = ... ORDER BY Id;
```

### 3. Isolation Level

- **ReadCommitted** (défaut): Bon compromis. Retry sur deadlock c'est OK.
- **Serializable**: Plus sûr mais + lent, deadlocks fréquents.
- **ReadUncommitted**: "Sale reads" possibles, moins de deadlocks.

## Contexte Architecture Polly

```
ResiliencePipeline (Polly 8.0+)
  ├─ Retry Policy
  │  ├─ Max Attempts: 5
  │  ├─ Delay: ExponentialBackoff
  │  ├─ Jitter: +0-50ms
  │  └─ Predicate: SqlException.Number == 1205
  └─ Execute(action)
     └─ Enveloppe la transaction
```

## Dépendances

- **Polly** 8.4.2 - Résiliation & retry
- **Polly.Core** 8.4.2 - Core APIs
- **Dapper** 2.1.15 - Micro-ORM
- **Microsoft.Data.SqlClient** 5.1.5 - Driver SQL Server

## Performance

Sur cette démo:

- **Sans deadlock**: ~50-100ms par opération
- **Avec retry x3**: ~500-600ms (backoff + reexecution)
- **Perte acceptable** comparé au crash applicatif

## Notes

1. **Idempotence**: Assure que rejeux multiples ont le même effet.
   - Ici: `UPDATE ... SET Value = Value + 1` N'est PAS idempotent
   - **Mieux**: Ajouter une `version` ou `timestamp` pour détection de doublon

2. **Logging en Production**: Remplacer les `Console.WriteLine` par ILogger (Serilog, NLog, etc.)

3. **Circuit Breaker**: Ajouter après Retry si deadlocks récurrents:

```csharp
new ResiliencePipelineBuilder()
    .AddRetry(...)
    .AddCircuitBreaker(new CircuitBreakerStrategyOptions
    {
        FailureRatio = 0.5,  // 50% d'échecs
        MinimumThroughput = 4,
        Timeout = TimeSpan.FromSeconds(30)
    })
    .Build();
```

---

**Author**: Demo Alain Ranger  
**Date**: Mars 2024  
**Licence**: MIT
