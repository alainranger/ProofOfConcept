# Checklist d'Integration

Liste de verification pour integrer `DeadlockPolly.Core` en production.

---

## Phase 1 : Setup Initial

- [ ] Reference projet ou package NuGet ajoutee
- [ ] `dotnet build` reussit sans erreurs
- [ ] `dotnet test` reussit (29/29)

### Enregistrement DI

- [ ] `AddDeadlockRetryStack()` appele dans `Program.cs` / `Startup.cs`
- [ ] Chaine de connexion provient de la configuration (pas hardcodee)
- [ ] `MaxRetries` configure selon les besoins (recommande: 3 a 5)
- [ ] `OnRetry` configure avec logging adapte

```csharp
// Exemple minimal
services.AddDeadlockRetryStack(
    connectionString: config.GetConnectionString("Default")!,
    configureRetry: opt => opt.MaxRetries = 3);
```

---

## Phase 2 : Implementation des Repositories

- [ ] Repositories injectent `ITransactionalExecutor` (pas `TransactionalExecutor` directement)
- [ ] Toutes les operations SQL passent par `executor.ExecuteAsync()`
- [ ] Aucun `SqlConnection` ouvert manuellement dans les repositories
- [ ] Aucune transaction geree manuellement

```csharp
// Correct
public class MyRepo(ITransactionalExecutor executor)
{
    public Task<int> InsertAsync(MyEntity e) =>
        executor.ExecuteAsync((conn, tx) =>
            conn.ExecuteScalarAsync<int>(Sql, e, tx));
}
```

---

## Phase 3 : Tests

### Tests Unitaires

- [ ] Repositories testes avec `Mock<ITransactionalExecutor>`
- [ ] Comportement retry teste (succes apres N echecs)
- [ ] Comportement exception teste (echec apres MaxRetries)

```csharp
// Pattern de test unitaire
var mockExecutor = new Mock<ITransactionalExecutor>();
mockExecutor
    .Setup(x => x.ExecuteAsync(It.IsAny<Func<IDbConnection, IDbTransaction, Task<int>>>()))
    .ReturnsAsync(42);

var repo = new MyRepo(mockExecutor.Object);
var result = await repo.InsertAsync(entity);
Assert.Equal(42, result);
```

### Tests d'Integration

- [ ] Tests d'integration configurent `BaseDelayMs = 10` (delais courts)
- [ ] Base de tests separee ou transactions rollback en fin de test
- [ ] Test de simulation deadlock present (optionnel)

---

## Phase 4 : Configuration Production

### Connection String

- [ ] Chaine de connexion dans `appsettings.json` (pas dans le code)
- [ ] Secrets geres via User Secrets / Azure Key Vault / environnement
- [ ] `Connection Timeout` configure (recommande: 30s)
- [ ] `Command Timeout` configure si besoin

```json
// appsettings.json
{
  "ConnectionStrings": {
    "Default": "Server=prod-sql;Database=MyDb;Integrated Security=True;Connection Timeout=30;"
  }
}
```

### Options Retry

| Parametre | Developpement | Production |
|-----------|--------------|------------|
| `MaxRetries` | 2 | 3 a 5 |
| `BaseDelayMs` | 50 | 100 |
| `MaxDelayMs` | 500 | 2000 a 5000 |

---

## Phase 5 : Observabilite

- [ ] `OnRetry` callback logue avec niveau WARNING
- [ ] Metriques deadlock comptees (compteur Prometheus / Application Insights)
- [ ] Alertes configurees si taux de retry > seuil (ex: > 5% des transactions)

```csharp
opt.OnRetry = (attempt, delay) =>
{
    logger.LogWarning(
        "Deadlock SQL - tentative {Attempt} dans {Delay}ms | {Endpoint}",
        attempt,
        (int)delay.TotalMilliseconds,
        httpContext?.Request.Path);

    metrics.IncrementCounter("sql_deadlock_retries_total");
};
```

---

## Phase 6 : validation Finale

- [ ] `dotnet build ManageDeadlockPolly.slnx` -> 0 warnings, 0 errors
- [ ] `dotnet test ManageDeadlockPolly.slnx` -> 29/29 tests passes
- [ ] Tests d'integration passes contre base de test
- [ ] Review de code effectuee sur les repositories migrés
- [ ] Documentation interne mise a jour

---

## Anti-patterns a Eviter

| Anti-pattern | Probleme | Solution |
|---|---|---|
| Injecter `TransactionalExecutor` directement | Couplage fort, difficile a mocker | Injecter `ITransactionalExecutor` |
| Ouvrir `SqlConnection` dans le repository | Bypass le retry | Utiliser `executor.ExecuteAsync()` |
| Hardcoder la connection string | Securite, deplacement entre envs | Utiliser la configuration |
| `MaxRetries = 0` | Desactive le retry | Minimum 1, recommande 3 |
| Pas de `OnRetry` callback en prod | Deadlocks invisibles | Toujours logger les retries |

---

**Retour -> [INTEGRATION_GUIDE.md](./INTEGRATION_GUIDE.md)**
