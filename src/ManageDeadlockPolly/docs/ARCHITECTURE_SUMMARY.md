# Architecture - Polly + Dapper Deadlock Retry

## Vue d'Ensemble

Solution en 3 projets .NET 9 pour gerer les deadlocks SQL Server avec retry automatique.

```
┌─────────────────────────────────────────────────────────────┐
│                    DeadlockPolly.Demo                       │
│                    (Console App)                            │
│  Program.cs  ──>  ITransactionalExecutor                    │
└────────────────────────┬────────────────────────────────────┘
                         │ depends on
┌────────────────────────▼────────────────────────────────────┐
│                    DeadlockPolly.Core                       │
│                    (Class Library)                          │
│                                                             │
│  Extensions/                                                │
│    AddDeadlockRetryStack()     ── enregistrement DI one-shot│
│                                                             │
│  DataAccess/                                                │
│    ITransactionalExecutor                                   │
│    TransactionalExecutor  ──> IDeadlockRetryPolicy          │
│    IDbConnectionProvider                                    │
│    SqlServerConnectionProvider                              │
│                                                             │
│  RetryPolicies/                                             │
│    IDeadlockRetryPolicy                                     │
│    PollyDeadlockRetryPolicy   ── Polly ResiliencePipeline   │
│    DeadlockRetryPolicyOptions ── config: MaxRetries, delays │
│                                                             │
│  Repositories/                                              │
│    DeadlockTestRepository  ──> ITransactionalExecutor       │
│    DeadlockTestRecord                                       │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                    DeadlockPolly.Tests                      │
│                    (xUnit - 29 tests)                       │
│  Tests RetryPolicies, DataAccess (Moq), Helpers             │
└─────────────────────────────────────────────────────────────┘
```

---

## Composants Detailles

### 1. DeadlockRetryPolicyOptions

Options de configuration de la politique de retry.

```csharp
namespace DeadlockPolly.Core.RetryPolicies;

public class DeadlockRetryPolicyOptions
{
    public int MaxRetries { get; set; } = 5;
    public int InitialDelayMs { get; set; } = 100;
    public int MaxJitterMs { get; set; } = 50;
    public Action<int, TimeSpan>? OnRetry { get; set; }
}
```

**Calcul du delai :** jitter exponentiel basé sur `InitialDelayMs` avec variation aléatoire jusqu'à `MaxJitterMs`

### 2. PollyDeadlockRetryPolicy

Implementation de `IDeadlockRetryPolicy` via Polly `ResiliencePipeline`.

- Detecte les deadlocks: `SqlException.Number == 1205`
- Retry avec delai exponentiel + jitter
- Callback `OnRetry` configurable

### 3. TransactionalExecutor

Orchestre les deux responsabilites :
1. Ouvre une connexion SQL (via `IDbConnectionProvider`)
2. Enveloppe dans une transaction avec `IsolationLevel.ReadCommitted`
3. Delègue l'execution à `IDeadlockRetryPolicy.ExecuteAsync()`

```csharp
// Signature de la methode principale
Task<T> ExecuteAsync<T>(
    Func<IDbConnection, IDbTransaction, Task<T>> operation);
```

### 4. ServiceCollectionExtensions

Point d'entree DI unique pour les consommateurs.

```csharp
services.AddDeadlockRetryStack(
    connectionString: "Server=...;Database=...;",
    configureRetry: opt =>
    {
        opt.MaxRetries = 5;
        opt.InitialDelayMs = 100;
        opt.MaxJitterMs = 50;
        opt.OnRetry = (attempt, delay) =>
            Console.WriteLine($"[RETRY {attempt}] dans {delay.TotalMilliseconds:F0}ms");
    });
```

Enregistre dans le conteneur DI :
- `IDeadlockRetryPolicy` -> `PollyDeadlockRetryPolicy` (Singleton)
- `IDbConnectionProvider` -> `SqlServerConnectionProvider` (Scoped)
- `ITransactionalExecutor` -> `TransactionalExecutor` (Scoped)

---

## Flux d'Execution

```
Application
    │
    ▼
ITransactionalExecutor.ExecuteAsync(operation)
    │
    ├─ Ouvre connexion SQL (SqlServerConnectionProvider)
    ├─ Ouvre transaction (IsolationLevel.ReadCommitted)
    │
    ▼
IDeadlockRetryPolicy.ExecuteAsync(wrappedOperation)
    │
    ├─[1er essai]─ operation(conn, tx) ──> SqlException 1205
    │              └─ Polly: attendre delai_1, retry
    │
    ├─[2eme essai]─ operation(conn, tx) ──> SqlException 1205
    │              └─ Polly: attendre delai_2, retry
    │
    └─[3eme essai]─ operation(conn, tx) ──> Succes
                   └─ COMMIT transaction
                      Retourner resultat
```

---

## Tests Unitaires (29 tests)

| Classe de test | Tests | Ce qui est teste |
|----------------|-------|-----------------|
| `DeadlockRetryPolicyOptionsTests` | 7 | Valeurs par defaut, validation MaxRetries/delays |
| `PollyDeadlockRetryPolicyTests` | 12 | Succes immediat, retry sur deadlock, callback OnRetry, max retries atteint |
| `TransactionalExecutorTests` | 10 | Ouverture conn/tx, commit, rollback, propagation exception |

**Caracteristique cle :** tous les tests sont 100% en memoire (Moq), aucune base SQL requise.

`SqlExceptionHelper` utilise la reflexion pour instancier un vrai `SqlException(1205)`
(impossible à construire normalement car le constructeur est interne).

---

## Decisions de Design

| Decision | Choix | Justification |
|----------|-------|---------------|
| Framework retry | Polly 8 (ResiliencePipeline) | Standard industrie, testable, extensible |
| Micro-ORM | Dapper | Leger, performant, compatible transactions |
| DI | Microsoft.Extensions.DI | Compatible ASP.NET Core, Worker Service, etc. |
| Isolation transaction | ReadCommitted | Equilibre entre consistance et concurrence |
| Jitter delai | Exponentiel + aleatoire | Evite le "thundering herd" sur deadlock simultanes |
| Structure solution | 3 projets | Separation des responsabilites, Core reutilisable |

---

**Suite -> [INTEGRATION_GUIDE.md](./INTEGRATION_GUIDE.md)**
