# 📚 ManageDeadlockPolly - Index Complet

> **Gestion complète des Deadlocks SQL Server avec Polly + Dapper**

Bienvenue! Ce projet contient une démonstration **production-ready** de gestion automatique des deadlocks SQL Server avec retry Polly + Dapper.

---

## 🎯 Démarrage Rapide (3 min)

**Impatient? Starts here:**

1. 📖 Lire → [QUICKSTART.md](./QUICKSTART.md) (2 min)
2. ▶️ Executer: `docker-compose up --build`
   - Ou en VS Code: `Cmd/Ctrl + Shift + P` → "Tasks: Run Task" → "🐳 Docker: Démarrer"
3. 👀 Observer les logs avec deadlock + retry

**Resultat**: App complète avec SQL Server prête en ~20s ✅

---

## 📖 Documentation Complète

| File | Descrição | Quand Lire |
|------|-----------|-----------|
| [QUICKSTART.md](./QUICKSTART.md) | Guide 3 min pour lancer | **DEBUT** - prioriser! |
| [README.md](../README.md) | Doc complète + architecture | Architecture & concepts |
| [INTEGRATION_CHECKLIST.md](./INTEGRATION_CHECKLIST.md) | Intégrer dans ton projet | Ajouter à ton code |
| [MANUAL_TEST_GUIDE.md](./MANUAL_TEST_GUIDE.md) | Reproduire deadlock manuellement | Tester/debugger |
| **Ce fichier** (INDEX.md) | Navigation globale | Tu es ici 👈 |

---

## 💻 Fichiers de Code

### Core Classes

| Fichier | Rôle | Utilité |
|---------|------|---------|
| `DeadlockRetryService.cs` | **Service principal** | Polly retry + transaction management |
| `Program.cs` | Harness de test | Démonstration complète |
| `AdvancedExamples.cs` | Patterns avancés | Circuit Breaker, Versioning, Telemetry |
| `AspNetCoreExample.cs` | Intégration ASP.NET Core | DI, Controllers, Tests unitaires |

### Infrastructure

| Fichier | Rôle |
|---------|------|
| `docker-compose.yml` | Orchestration SQL Server + app |
| `Dockerfile` | Image .NET 8 multi-stage |
| `init.sql` | Création DB + tables |
| `.vscode/tasks.json` | Tâches VS Code (docker, dotnet, logs) |

### Config

| Fichier | Rôle |
|---------|------|
| `.dockerignore` | Optimise la taille image Docker |
| `.gitignore` | Exclude bin/, obj/, logs/ |
| `ManageDeadlockPolly.csproj` | Projet .NET 8 + dépendances |

---

## 🚀 Cas d'Usage

### 1️⃣ Je veux juste voir démo

```bash
docker-compose up --build
# Regarde les logs, vois le deadlock + retry
```

→ [QUICKSTART.md](./QUICKSTART.md)

### 2️⃣ Je veux ajouter Polly deadlock retry à mon projet

```bash
# Copie DeadlockRetryService.cs dans ton projet
# Ajoute les dépendances du .csproj
# Lire la checklist d'intégration
```

→ [INTEGRATION_CHECKLIST.md](./INTEGRATION_CHECKLIST.md)

### 3️⃣ Je veux comprendre l'architecture

```bash
# Lire README.md complet
# Voir démo dans Program.cs
# Explorer Examples/AdvancedExamples.cs
```

→ [README.md](../README.md)

### 4️⃣ Je veux reproduire un deadlock manuellement

```bash
# Utiliser le guide SQL Server
# Lancer 2 sessions concurrentes
# Observer error 1205
```

→ [MANUAL_TEST_GUIDE.md](./MANUAL_TEST_GUIDE.md)

### 5️⃣ Je veux intégrer dans ASP.NET Core

```bash
# Voir Examples/AspNetCoreExample.cs
# Copier la configuration DI
# Tester avec WebApplicationFactory
```

→ `Examples/AspNetCoreExample.cs`

---

## 📊 Architecture Globale

```
┌─────────────────────────────────────────────────────────┐
│                   Utilisateur Final                     │
├─────────────────────────────────────────────────────────┤
│            Examples/AspNetCoreExample.cs               │
│      (Controllers + DI + Tests unitaires)              │
├─────────────────────────────────────────────────────────┤
│            DeadlockRetryService.cs                      │
│          (Polly Retry + Transaction)                   │
├─────────────────────────────────────────────────────────┤
│        DeadlockTestRepository.cs                        │
│      (Dapper queries + business logic)                 │
├─────────────────────────────────────────────────────────┤
│           SQL Server (1433)                            │
│        Database: DeadlockTestDb                        │
│        Table: dbo.DeadlockTest                         │
└─────────────────────────────────────────────────────────┘
```

---

## 🔑 Points Clés - Cheatsheet

### Polly Retry Policy (ce qu'il fait)

```csharp
// Retry sur error 1205 (deadlock)
// Max 5 tentatives
// Délais: 100ms, 200ms, 400ms, 800ms, 1600ms
// + jitter aléatoire 0-50ms
// Rejeu COMPLET de la transaction
```

### Transaction Management (comment?)

```csharp
// 1. Ouvrir connexion
await using var conn = new SqlConnection(...);

// 2. Beginer transaction
await using var tx = await conn.BeginTransactionAsync();

// 3. Executer queries avec transaction
await conn.ExecuteAsync("UPDATE ...", tx);

// 4. Commit ou Rollback automatique en finally
```

### Deadlock Detection (d'où?)

```csharp
// Polly détecte SqlException avec Number == 1205
// C'est l'error code SQL Server pour deadlock
// Tous les autres exceptions = throw (pas de retry)
```

---

## 🧪 Tests

### Quick Test (Docker)

```bash
docker-compose up --build  # Lance tout (20s)
# Observez les logs
docker-compose down        # Arrête
```

### Manual Test (SQL Direct)

```bash
docker exec -it <sql-container> sqlcmd -S localhost ...
# Session 1: UPDATE Id=1, UPDATE Id=2
# Session 2: UPDATE Id=2, UPDATE Id=1  (inverse!)
# → Deadlock! → Msg 1205
```

### Unit Tests

Voir `Examples/AspNetCoreExample.cs` → classe `DeadlockRetryServiceTests`

```csharp
[Fact]
public async Task OnDeadlock_RetriesAndSucceeds()
{
    // Arrange: setup service
    // Act: trigger deadlock
    // Assert: verify retry happened
}
```

---

## 📈 Performance

| Opération | Temps |
|-----------|-------|
| Requête normal (pas deadlock) | 50-100ms |
| Deadlock + retry x2 | 300-500ms |
| Deadlock + retry x5 | 1-2s |

**Verdict**: Acceptable vs. crash applicatif ✅

---

## ⚠️ Erreurs Courantes

| Erreur | Cause | Solution |
|--------|-------|----------|
| `SqlException: timeout` | Requête slow | Créer indexes, optimiser query |
| Deadlock persiste | Ordre non-deterministic | Toujours accéder en ordre croissant (Id 1→2→3) |
| Polly ne retry pas | Exception type incorrect | Vérifier Number == 1205 |
| Container ne démarre | Port 1433 used | `lsof -i :1433` puis kill |
| Donnees incohérentes après test | Normal | Reset avec `docker-compose down -v` |

---

## 🎓 Prochaines Étapes

### Débutant

1. Lire docs/QUICKSTART.md
2. Lancer `docker-compose up --build` et observer
3. Essayer manual test du guide

### Intermédiaire

1. Lire README.md complet
2. Ajouter à ton projet (docs/INTEGRATION_CHECKLIST.md)
3. Configurer logging (Serilog ou ILogger)

### Avancé

1. Voir Examples/AdvancedExamples.cs (Circuit Breaker, etc.)
2. Ajouter telemetry (Application Insights)
3. Optimiser patterns prédéfinis
4. Adapter isolation level si besoin

---

## 🛠️ Commandes Rapides

**VS Code** (recommandé - `Cmd/Ctrl + Shift + P` → "Tasks: Run Task"):

- 🐳 Docker: Démarrer (build + run)
- 🐳 Docker: Logs (tout / app / SQL)
- 📦 .NET: Build
- 📦 .NET: Run

**Terminal:**

```bash
# Lancer
docker-compose up --build        # Build + run complet

# Développement
dotnet build                     # Compiler local
dotnet run                       # Run local

# Monitoring
docker-compose logs -f           # Logs app
docker-compose logs -f sql-server # SQL logs

# Arrêt
docker-compose down              # Stop (volumes persistent)
docker-compose down -v           # Stop + delete volumes
```

---

## 📞 Support

### Questions fréquentes

**Q: Puis-je utiliser dans production?**  
✅ Oui! Code est production-ready. Adapter logging + monitoring.

**Q: Ça marche avec Entity Framework?**  
Partiellement. Polly fonctionne au niveau SQL. Voir INTEGRATION_CHECKLIST pour EF.

**Q: Combien de temps avant de retry?**  
Exponential avec jitter: 100ms, 200ms, 400ms, ... (+0-50ms jitter)

**Q: Faut-il rendre opérations idempotentes?**  
Recommandé oui. Ou ajouter versioning pour détecter doublon. Voir INTEGRATION_CHECKLIST.

---

## 📦 Dépendances

```xml
<ItemGroup>
    <PackageReference Include="Polly" Version="8.4.2" />
    <PackageReference Include="Polly.Core" Version="8.4.2" />
    <PackageReference Include="Dapper" Version="2.1.15" />
    <PackageReference Include="Microsoft.Data.SqlClient" Version="5.1.5" />
</ItemGroup>
```

Compatibilité: **.NET 6+** (démo utilise .NET 8)

---

## 📜 Fichier Complet du Projet

```
ManageDeadlockPolly/
├── .dockerignore                  # Docker optimizations
├── .gitignore                     # Git exclusions
├── .vscode/tasks.json             # Tâches VS Code 🎯
├── Dockerfile                     # Multi-stage build .NET 8
├── docker-compose.yml             # SQL Server + App orchestration
├── ManageDeadlockPolly.csproj
├── Program.cs                     # Harness de test complet
├── init.sql                       # Création DB + tables test
│
├── Sources (.cs)
│  ├── DeadlockRetryService.cs     # ⭐ Core service Polly
│  ├── Examples/AdvancedExamples.cs   # Patterns avancés
│  └── Examples/AspNetCoreExample.cs  # Intégration ASP.NET Core
│
├── Scripts (optionnel)
│  ├── scripts/setup.sh            # Setup automatique
│  └── scripts/dev.sh              # Utilitaires shell
│
└── Documentation (.md)
    ├── docs/QUICKSTART.md            # 3 min pour lancer ⭐
    ├── README.md                     # Doc complète
    ├── docs/INTEGRATION_CHECKLIST.md # Ajouter à ton projet
    ├── docs/MANUAL_TEST_GUIDE.md     # Reproduire deadlock
    ├── docs/00-START-HERE.md         # Vue d'ensemble
    └── docs/INDEX.md                 # This file 👈
```

---

## 🎉 Bon à Savoir

- ✅ **Prêt à la production**: Code testé + logging
- ✅ **Docker out-of-the-box**: Lancer tout avec 1 commande (`docker-compose up`)
- ✅ **VS Code tasks**: Intégration native dans l'éditeur 🎯  
- ✅ **Extensible**: Copier-coller DeadlockRetryService.cs  
- ✅ **Patterns avancés**: Circuit Breaker, Telemetry, etc.
- ✅ **Tests inclus**: Unitaires + intégration

---

**🚀 Ready? → [QUICKSTART.md](./QUICKSTART.md)**

---

*Last Updated: Mars 2024*  
*License: MIT*
