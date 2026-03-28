# Quickstart

Demarrez en moins de 5 minutes.

---

## Option A : Demo Docker (recommande)

### Prerequis
- Docker Desktop installe et en cours d'execution
- Port 1433 libre

### Etapes

```bash
# 1. Se placer dans le projet
cd /Users/alainranger/sources/ProofOfConcept/src/ManageDeadlockPolly

# 2. Lancer
docker-compose up --build

# Attendre ~20s le demarrage de SQL Server...
# La demo s'execute automatiquement et affiche les resultats.

# 3. Arreter
docker-compose down -v
```

### Sortie Attendue

```
deadlock-app  | Attente de SQL Server...
deadlock-app  | Connexion a SQL Server etablie
deadlock-app  |
deadlock-app  | === Test 1: Operation Simple ===
deadlock-app  | [TX] Transaction ouverte (Isolation: ReadCommitted)
deadlock-app  | [TX] Transaction validee
deadlock-app  | Succes: 2 enregistrements mis a jour
deadlock-app  |
deadlock-app  | === Test 2: Simulation Deadlock ===
deadlock-app  | [RETRY 1] Deadlock detecte, retry dans 111ms
deadlock-app  | [TX] Transaction validee
deadlock-app  | Succes apres 1 retry
```

---

## Option B : Build et Tests Locaux (sans SQL)

```bash
cd /Users/alainranger/sources/ProofOfConcept/src/ManageDeadlockPolly

# Build de la solution
dotnet build ManageDeadlockPolly.slnx

# Tests unitaires (29 tests - aucune connexion SQL requise)
dotnet test ManageDeadlockPolly.slnx
```

Resultat attendu :

```
Build succeeded.
    0 Warning(s)
    0 Error(s)

Passed!  - Failed: 0, Passed: 29, Skipped: 0, Total: 29
```

---

## Option C : Run Local (necessité SQL Server)

```bash
# Demarrer SQL Server seul via Docker
docker-compose up sqlserver -d

# Attendre ~20s, puis
dotnet run --project src/DeadlockPolly.Demo
```

---

## Structure de la Solution

```
ManageDeadlockPolly.slnx
|
+-- src/DeadlockPolly.Core/      Bibliotheque (.NET 9)
+-- src/DeadlockPolly.Demo/      Demo console (.NET 9)
+-- tests/DeadlockPolly.Tests/   Tests xUnit (.NET 9)
```

---

## Prochaines Etapes

| Objectif | Document |
|----------|----------|
| Comprendre l'architecture | [ARCHITECTURE_SUMMARY.md](./ARCHITECTURE_SUMMARY.md) |
| Integrer dans ton projet | [INTEGRATION_GUIDE.md](./INTEGRATION_GUIDE.md) |
| Test manuel avec deadlocks reels | [MANUAL_TEST_GUIDE.md](./MANUAL_TEST_GUIDE.md) |
