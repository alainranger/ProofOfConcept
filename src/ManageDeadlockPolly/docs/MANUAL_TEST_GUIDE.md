# Guide de Test Manuel

Tester la gestion des deadlocks avec une vraie base SQL Server.

---

## Prerequis

- Docker Desktop installe et en cours d'execution
- Port 1433 libre sur la machine

---

## 1. Demarrer l'Infrastructure

```bash
cd /Users/alainranger/sources/ProofOfConcept/src/ManageDeadlockPolly

# Demarrer SQL Server + l'application demo
docker-compose up --build

# OU demarrer SQL Server seul (pour les tests manuels)
docker-compose up sqlserver -d
```

Attendre le message dans les logs :
```
sqlserver  | SQL Server is now ready for client connections.
```
(environ 20 secondes)

---

## 2. Verifier la Connexion SQL

```bash
# Depuis la machine hote
docker exec -it <container_sqlserver> /opt/mssql-tools18/bin/sqlcmd     -S localhost -U sa -P "$SA_PASSWORD"     -C -Q "SELECT @@VERSION"
```

Resultat attendu : version SQL Server 2022.

---

## 3. Tester le Schema

La demo cree automatiquement la table `DeadlockTest` au demarrage.

```sql
-- Verifier la table
SELECT * FROM DeadlockTest;

-- Verifier les enregistrements de test
SELECT Id, Value, LastUpdated, RetryCount FROM DeadlockTest ORDER BY Id;
```

---

## 4. Simuler un Deadlock Manuel

### Methode : Deux Sessions Concurrentes

Ouvrir **deux fenetres de terminal** separees.

**Session 1 :**
```sql
BEGIN TRANSACTION;

-- Verrouiller l'enregistrement 1
UPDATE DeadlockTest SET Value = 'Session1-Row1' WHERE Id = 1;

-- Attendre avant de continuer (laisser Session 2 demarrer)
WAITFOR DELAY '00:00:05';

-- Tenter de verrouiller l'enregistrement 2 (deadlock si Session 2 l'a pris)
UPDATE DeadlockTest SET Value = 'Session1-Row2' WHERE Id = 2;

COMMIT TRANSACTION;
```

**Session 2 :**
```sql
BEGIN TRANSACTION;

-- Verrouiller l'enregistrement 2
UPDATE DeadlockTest SET Value = 'Session2-Row2' WHERE Id = 2;

-- Tenter de verrouiller l'enregistrement 1 (deadlock avec Session 1)
UPDATE DeadlockTest SET Value = 'Session2-Row1' WHERE Id = 1;

COMMIT TRANSACTION;
```

**Resultat attendu :**
- SQL Server choisit une victime (l'une des sessions recoit l'erreur 1205)
- L'application detecte `SqlException.Number == 1205` et retry automatiquement
- Les logs affichent `[RETRY N] Deadlock detecte, retry dans Xms`

---

## 5. Valider les Logs de Retry

```bash
# Logs de l'application demo
docker-compose logs -f deadlock-app

# Filtrer les lignes de retry
docker-compose logs deadlock-app | grep -i "retry\|deadlock\|RETRY"
```

Sortie attendue lors d'un deadlock :
```
deadlock-app  | [RETRY 1] Deadlock detecte, retry dans 111ms
deadlock-app  | [RETRY 2] Deadlock detecte, retry dans 247ms
deadlock-app  | [TX] Transaction validee
```

---

## 6. Tests de Charge (optionnel)

Lancer plusieurs instances concurrentes pour provoquer des deadlocks naturels :

```bash
# Depuis la machine hote - lancer 5 instances en parallele
for i in {1..5}; do
    dotnet run --project src/DeadlockPolly.Demo &
done
wait
```

Observer les retries dans les logs.

---

## 7. Nettoyer

```bash
# Arreter les conteneurs
docker-compose down

# Arreter + supprimer les volumes (repart a zero)
docker-compose down -v

# Verifier que le port est libere
lsof -i :1433
```

---

## 8. Commandes SQL de Reference

```sql
-- Voir les verrous actifs
SELECT
    request_session_id,
    resource_type,
    resource_description,
    request_mode,
    request_status
FROM sys.dm_tran_locks
WHERE resource_database_id = DB_ID();

-- Voir les transactions actives
SELECT
    session_id,
    transaction_id,
    is_local,
    is_enlisted
FROM sys.dm_tran_session_transactions;

-- Historique des deadlocks (trace par defaut)
SELECT
    XEventData.XEvent.value('(data/value)[1]', 'varchar(max)') AS DeadlockGraph
FROM (
    SELECT CAST(target_data AS XML) AS TargetData
    FROM sys.dm_xe_session_targets t
    JOIN sys.dm_xe_sessions s ON t.event_session_address = s.address
    WHERE s.name = 'system_health'
    AND t.target_name = 'ring_buffer'
) AS Data
CROSS APPLY TargetData.nodes('RingBufferTarget/event[@name="xml_deadlock_report"]') AS XEventData (XEvent);
```

---

## 9. Troubleshooting

| Probleme | Cause Probable | Solution |
|----------|----------------|---------|
| `Connection refused :1433` | SQL Server pas encore pret | Attendre 30s, verifier `docker-compose logs sqlserver` |
| `Login failed for user 'sa'` | Mauvais mot de passe | Verifier `MSSQL_SA_PASSWORD` dans `docker-compose.yml` |
| `Cannot open database` | Base pas creee | La demo la cree au demarrage, relancer l'app |
| Pas de deadlock genere | Transactions pas assez concurrentes | Augmenter parallelisme ou utiliser WAITFOR |
| `Port 1433 already in use` | SQL Server local tourne | `sudo lsof -i :1433` puis `kill -9 <PID>` |

---

**Retour -> [00-START-HERE.md](./00-START-HERE.md)**
