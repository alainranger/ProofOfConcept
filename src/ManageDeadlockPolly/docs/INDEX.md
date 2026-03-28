# Index de la Documentation

Navigation complete du projet **Polly + Dapper - Gestion Deadlocks**.

---

## Documents par Ordre de Lecture

| # | Fichier | Contenu | Temps |
|---|---------|---------|-------|
| 1 | [00-START-HERE.md](./00-START-HERE.md) | Introduction, structure, demo rapide | 3 min |
| 2 | [QUICKSTART.md](./QUICKSTART.md) | Demarrage rapide, build, tests | 5 min |
| 3 | [ARCHITECTURE_SUMMARY.md](./ARCHITECTURE_SUMMARY.md) | Architecture, design patterns, composants | 10 min |
| 4 | [INTEGRATION_GUIDE.md](./INTEGRATION_GUIDE.md) | Integration dans votre projet | 15 min |
| 5 | [INTEGRATION_CHECKLIST.md](./INTEGRATION_CHECKLIST.md) | Checklist d'integration | 5 min |
| 6 | [MANUAL_TEST_GUIDE.md](./MANUAL_TEST_GUIDE.md) | Guide de test manuel avec SQL | 20 min |

---

## Structure de la Solution

```
ManageDeadlockPolly.slnx
|
+-- src/
|   +-- DeadlockPolly.Core/         (bibliotheque - .NET 9)
|   +-- DeadlockPolly.Demo/         (console app - .NET 9)
|
+-- tests/
|   +-- DeadlockPolly.Tests/        (xUnit - 29 tests)
|
+-- docs/
+-- Examples/
+-- Dockerfile
+-- docker-compose.yml
```

---

## Fichiers Source

### DeadlockPolly.Core (src/DeadlockPolly.Core/)

| Fichier | Namespace | Role |
|---------|-----------|------|
| `RetryPolicies/IDeadlockRetryPolicy.cs` | `DeadlockPolly.Core.RetryPolicies` | Interface retry |
| `RetryPolicies/DeadlockRetryPolicyOptions.cs` | `DeadlockPolly.Core.RetryPolicies` | Options de configuration |
| `RetryPolicies/PollyDeadlockRetryPolicy.cs` | `DeadlockPolly.Core.RetryPolicies` | Implementation Polly |
| `DataAccess/IDbConnectionProvider.cs` | `DeadlockPolly.Core.DataAccess` | Interface connexion |
| `DataAccess/SqlServerConnectionProvider.cs` | `DeadlockPolly.Core.DataAccess` | Connexion SQL Server |
| `DataAccess/ITransactionalExecutor.cs` | `DeadlockPolly.Core.DataAccess` | Interface executeur |
| `DataAccess/TransactionalExecutor.cs` | `DeadlockPolly.Core.DataAccess` | Execution + retry |
| `Extensions/ServiceCollectionExtensions.cs` | `DeadlockPolly.Core.Extensions` | `AddDeadlockRetryStack()` |
| `Repositories/DeadlockTestRecord.cs` | `DeadlockPolly.Core.Repositories` | Modele de donnees |
| `Repositories/DeadlockTestRepository.cs` | `DeadlockPolly.Core.Repositories` | Repository demo |

### DeadlockPolly.Demo (src/DeadlockPolly.Demo/)

| Fichier | Namespace | Role |
|---------|-----------|------|
| `Program.cs` | `DeadlockPolly.Demo` | Point d'entree, 2 scenarios de test |

### DeadlockPolly.Tests (tests/DeadlockPolly.Tests/)

| Fichier | Tests | Role |
|---------|-------|------|
| `RetryPolicies/DeadlockRetryPolicyOptionsTests.cs` | 7 | Validation des options |
| `RetryPolicies/PollyDeadlockRetryPolicyTests.cs` | 12 | Tests politique Polly |
| `DataAccess/TransactionalExecutorTests.cs` | 10 | Tests executeur transactionnel |
| `Helpers/SqlExceptionHelper.cs` | - | Utilitaire: SqlException(1205) par reflexion |

---

## NuGet Packages

### DeadlockPolly.Core

| Package | Version | Usage |
|---------|---------|-------|
| `Polly` | 8.4.2 | Politique de retry |
| `Polly.Core` | 8.4.2 | Abstractions Polly |
| `Dapper` | 2.1.35 | Micro-ORM SQL |
| `Microsoft.Data.SqlClient` | 5.1.5 | Connexion SQL Server |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 8.0.0 | Interfaces DI |

### DeadlockPolly.Tests

| Package | Version | Usage |
|---------|---------|-------|
| `xunit` | 2.9.3 | Framework de tests |
| `xunit.runner.visualstudio` | 3.1.4 | Integration VS Code |
| `Moq` | 4.20.70 | Mocking |
| `Microsoft.NET.Test.Sdk` | 17.14.1 | SDK de tests |
| `coverlet.collector` | 6.0.4 | Couverture de code |

---

## Commandes de Reference

```bash
# Build
dotnet build ManageDeadlockPolly.slnx

# Tests (29 tests, aucune connexion SQL requise)
dotnet test ManageDeadlockPolly.slnx

# Demo (necessite Docker)
docker-compose up --build
docker-compose down -v

# Run local (necessite SQL Server local port 1433)
dotnet run --project src/DeadlockPolly.Demo
```

---

## Patterns Avances (Examples/)

| Fichier | Contenu |
|---------|---------|
| `Examples/AdvancedExamples.cs` | Circuit Breaker, telemetrie, retry exponentiel |
| `Examples/AspNetCoreExample.cs` | Integration ASP.NET Core (Controllers + DI) |
| `Examples/ReusableIntegrationExample.cs` | Pattern repository generique |

---

**Vue d'ensemble -> [ARCHITECTURE_SUMMARY.md](./ARCHITECTURE_SUMMARY.md)**
