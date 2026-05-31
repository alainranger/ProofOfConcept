# Projet Polly + Dapper - Gestion Deadlocks

Solution multi-projet **production-ready** dans :

```
/Users/alainranger/sources/ProofOfConcept/src/ManageDeadlockPolly/
```

---

## Structure du Projet

```
ManageDeadlockPolly.slnx               <- solution (3 projets)
|
+-- src/
|   +-- DeadlockPolly.Core/            Bibliotheque reutilisable (.NET 9)
|   |   +-- RetryPolicies/             IDeadlockRetryPolicy, PollyDeadlockRetryPolicy, Options
|   |   +-- DataAccess/                ITransactionalExecutor, TransactionalExecutor, ...
|   |   +-- Extensions/                ServiceCollectionExtensions (AddDeadlockRetryStack)
|   |   +-- Repositories/             DeadlockTestRecord, DeadlockTestRepository
|   |
|   +-- DeadlockPolly.Demo/            Application console de demonstration (.NET 9)
|       +-- Program.cs
|
+-- tests/
|   +-- DeadlockPolly.Tests/           Tests xUnit - 29 tests PASS (.NET 9)
|       +-- RetryPolicies/
|       +-- DataAccess/
|       +-- Helpers/
|
+-- docs/
+-- Examples/                          Patterns avances (API, Worker, Circuit Breaker)
+-- Dockerfile                         Multi-stage build .NET 9
+-- docker-compose.yml                 SQL Server 2022 + App
```

---

## Lancer la Demo (30 secondes)

```bash
cd /Users/alainranger/sources/ProofOfConcept/src/ManageDeadlockPolly
docker-compose up --build
```

**Ou depuis VS Code :** `Cmd+Shift+P` -> "Tasks: Run Task" -> "Docker: Demarrer"

Attendez ~20 s... puis la demo s'execute :

```
================================================================
  Demo Polly + Dapper - Gestion des Deadlocks
================================================================

Attente de SQL Server...
Connexion a SQL Server etablie

1. Test: operation simple
[TX] Transaction ouverte (Isolation: ReadCommitted)
[TX] Transaction validee
Succes: 2 enregistrements mis a jour

2. Test: concurrence avec deadlock
[RETRY 1] Deadlock detecte, retry dans 111ms
[TX] Transaction validee
Succes: les deux operations se sont terminees apres retry
```

---

## Lancer les Tests

```bash
dotnet test ManageDeadlockPolly.slnx
```

Resultat attendu : **29 tests reussis** en < 1 seconde (aucune dependance SQL).

---

## Commandes Cles

```bash
# Docker
docker-compose up --build             # Demarrer la demo
docker-compose down                   # Arreter
docker-compose down -v                # Arreter + supprimer volumes
docker-compose logs -f deadlock-app   # Logs app seulement

# .NET
dotnet build ManageDeadlockPolly.slnx    # Build solution
dotnet test  ManageDeadlockPolly.slnx    # Tests unitaires (29/29)
dotnet run --project src/DeadlockPolly.Demo  # Run local (necessite SQL)
```

---

## Utilisation de la Bibliotheque Core

```csharp
// 1. Enregistrement DI (une seule ligne)
services.AddDeadlockRetryStack(
    connectionString: "Server=...;",
    configureRetry: opt =>
    {
        opt.MaxRetries = 5;
        opt.OnRetry = (n, delay) => logger.LogWarning("Deadlock retry #{n}", n);
    });

// 2. Injection dans un repository
public class OrderRepository(ITransactionalExecutor executor)
{
    public Task<Order> CreateAsync(Order order) =>
        executor.ExecuteAsync(async (conn, tx) =>
        {
            var id = await conn.ExecuteScalarAsync<int>(InsertSql, order, tx);
            return order with { Id = id };
        });
}
```

---

## Prochaines Etapes

| Objectif                       | Ou aller                                           |
|--------------------------------|----------------------------------------------------|
| Voir la demo tourner           | `docker-compose up --build`                        |
| Comprendre l'architecture      | [ARCHITECTURE_SUMMARY.md](./ARCHITECTURE_SUMMARY.md) |
| Integrer dans ton projet       | [INTEGRATION_GUIDE.md](./INTEGRATION_GUIDE.md)    |
| Demarrer en 3 min              | [QUICKSTART.md](./QUICKSTART.md)                  |
| Navigation globale             | [INDEX.md](./INDEX.md)                            |

---

## Troubleshooting Rapide

| Probleme                    | Solution                                      |
|-----------------------------|-----------------------------------------------|
| `Port 1433 already in use`  | `lsof -i :1433` puis `kill -9 <PID>`         |
| `Docker not running`        | Ouvrir Docker Desktop                         |
| `Conteneur crash`           | `docker-compose logs` -> voir erreur          |
| `SQL timeout`               | Attendre 30 s, le healthcheck doit passer     |
| `Reset complet`             | `docker-compose down -v` puis `up --build`    |

---

**Next -> [QUICKSTART.md](./QUICKSTART.md)**
