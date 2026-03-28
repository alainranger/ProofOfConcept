## 🚀 Démarrage Rapide

### Prérequis

- ✅ Docker Desktop (avec Docker Compose)
- ✅ macOS / Linux / Windows avec WSL2

### 1. Cloner/Accéder au projet

```bash
cd /Users/alainranger/sources/ProofOfConcept/src/ManageDeadlockPolly
```

### 2. Lancer l'application complète

**Option A: VS Code (recommandé)**

- Ouvrir la palette de commandes: `Cmd/Ctrl + Shift + P`
- Taper: "Tasks: Run Task"
- Sélectionner: "🐳 Docker: Démarrer (build + run)"

**Option B: Terminal**

```bash
docker-compose up --build
```

**⏳ Attendez ~20s** pour que:

- SQL Server démarre
- La DB `DeadlockTestDb` est créée
- Les tables sont initialisées
- L'app .NET démarre et exécute les tests

### 3. Regarder les résultats

```
═══════════════════════════════════════════════════════════
  Démo Polly + Dapper - Gestion des Deadlocks
═══════════════════════════════════════════════════════════

⏳ Attente de SQL Server...
✓ Connexion à SQL Server établie

1️⃣  Test 1: Opération normale
[TX] Transaction ouverte
[TX] Transaction validée ✓
✓ 2 enregistrements mis à jour

2️⃣  Test 2: Avec deadlock concurrent
[RETRY 1] Deadlock détecté, retry dans 156ms
[TX] Transaction validée ✓
✓ Deux opérations terminées avec succès
```

### 4. Arrêter

```bash
docker-compose down        # Garder les volumes
docker-compose down -v     # Supprimer les volumes
```

---

## 📊 Logs & Monitoring

### Voir les logs en VS Code (recommandé)

- Palette de commandes: `Cmd/Ctrl + Shift + P`
- Taper: "Tasks: Run Task"
- Choisir:
  - `🐳 Docker: Logs (tout)` - Tout
  - `🐳 Docker: Logs (app)` - Juste l'app
  - `🐳 Docker: Logs (SQL)` - Juste SQL Server

### Ou en terminal

```bash
docker-compose logs -f              # Tout
docker-compose logs -f deadlock-app # App
docker-compose logs -f sql-server   # SQL

---

## 🔧 Customisation

### Changer le mot de passe SQL Server

1. Modifier `docker-compose.yml` → `SA_PASSWORD`
2. Modifier `Program.cs` → `connectionString`

### Augmenter les retries Polly

Modifier `Program.cs`:

```csharp
var retryService = new DeadlockRetryService(connectionString, maxRetries: 10); // 5 → 10
```

### Modifier les délais de retry

Modifier `DeadlockRetryService.cs` → méthode `BuildDeadlockRetryPolicy<T>()`:

```csharp
Delay = TimeSpan.FromMilliseconds(200 * Math.Pow(2, retryAttempt - 1)) // 100 → 200
```

---

## ✅ Points clés de la démo

✓ **Polly retry** sur `SqlException` (error 1205)  
✓ **Exponential backoff** (100ms, 200ms, 400ms...)  
✓ **Jitter aléatoire** pour éviter synchronisation  
✓ **Transaction complète rejoué** en cas de deadlock  
✓ **Logging détaillé** de chaque tentative  
✓ **Docker Compose** SQL Server + .NET app  
✓ **Tests concurrents** pour reproduire le deadlock  

---

## 🎓 Pour aller plus loin

- → Voir `README.md` pour la documentation complète
- → Voir `Examples/AdvancedExamples.cs` pour:
  - Circuit Breaker
  - Versioning idempotent
  - Batch patterns
  - Adaptive retries
  - Telemetry integration

---

**Happy Deadlock Handling! 🎉**
