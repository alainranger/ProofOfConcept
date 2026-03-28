## 🧪 Guide de Test Manuelle - Reproduction des Deadlocks

Ce guide explique comment reproduire manuellement un deadlock SQL Server pour valider que Polly vraiment retry.

---

## 1️⃣ Préparation: Démarrer les conteneurs

```bash
docker-compose up -d
```

Attendre ~15s que SQL Server soit opérationnel.

---

## 2️⃣ Se connecter à SQL Server

```bash
# Option A: Avec docker exec
docker exec -it manage_deadlock_polly-sql-server-1 /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P 'YourStrong!Pass2024'

# Option B: Avec Azure Data Studio ou SQL Server Management Studio
# Server: localhost,1433
# Login: sa
# Password: YourStrong!Pass2024
```

---

## 3️⃣ Vérifier l'état initial

```sql
USE DeadlockTestDb;
GO

SELECT * FROM dbo.DeadlockTest;
GO

-- Résultat attendu:
-- Id   Value
-- --   -----
-- 1    0
-- 2    0
```

---

## 4️⃣ Reproduire le Deadlock Manuellement

### Scénario: 2 Sessions SQL concurrentes

**Ordre critique**:

1. Ouvrir 2 sessions SQL (terminal 1 et terminal 2)
2. Exécuter **simultanément** le code dessous

### Session 1 (Terminal A) - Exécute IMMÉDIATEMENT après

```sql
USE DeadlockTestDb;
GO

BEGIN TRANSACTION;
PRINT 'SESSION 1: Transaction démarrée';

UPDATE dbo.DeadlockTest SET Value = Value + 100 WHERE Id = 1;
PRINT 'SESSION 1: Update Id=1 fait, attente 3 secondes...';

WAITFOR DELAY '00:00:03';

PRINT 'SESSION 1: Tentative Update Id=2...';
UPDATE dbo.DeadlockTest SET Value = Value + 100 WHERE Id = 2;

PRINT 'SESSION 1: Commit...';
COMMIT TRANSACTION;

SELECT * FROM dbo.DeadlockTest;
GO
```

### Session 2 (Terminal B) - Exécute **Pendant que Session 1 attend (WAITFOR)**

```sql
USE DeadlockTestDb;
GO

-- Attendre un peu pour que Session 1 verrouille Id=1
WAITFOR DELAY '00:00:01';

BEGIN TRANSACTION;
PRINT 'SESSION 2: Transaction démarrée';

UPDATE dbo.DeadlockTest SET Value = Value + 200 WHERE Id = 2;
PRINT 'SESSION 2: Update Id=2 fait, attente 3 secondes...';

WAITFOR DELAY '00:00:03';

PRINT 'SESSION 2: Tentative Update Id=1...';
UPDATE dbo.DeadlockTest SET Value = Value + 200 WHERE Id = 1;

PRINT 'SESSION 2: Commit...';
COMMIT TRANSACTION;

SELECT * FROM dbo.DeadlockTest;
GO
```

### Résultat Attendu

Une des 2 sessions affichera:

```
Msg 1205, Level 13, State 13, Server e1234567890ab, Line 10
Deadlock victim. The transaction (process ID 123) was deadlocked on 
{lock} resources with another process and has been chosen as the deadlock victim.
```

L'autre session poursuivra normalement.

---

## 5️⃣ Vérifier l'état après deadlock

```sql
SELECT * FROM dbo.DeadlockTest;
GO

-- Note: Seule une transaction a réussi
-- Donc les valeurs ne seront pas cohérentes
-- (Depends quelle session a gagné)
```

---

## 6️⃣ Reset pour le prochain test

```sql
UPDATE dbo.DeadlockTest SET Value = 0;
GO

SELECT * FROM dbo.DeadlockTest;
GO
```

---

## 🎯 Variantes de Deadlock

### Variante A: Escalade de verrou (LOCK ESCALATION)

```sql
-- Session 1
BEGIN TRAN;
UPDATE dbo.DeadlockTest SET Value = Value + 1 WHERE Id = 1;
UPDATE dbo.DeadlockTest SET Value = Value + 1 WHERE Id = 2;
UPDATE dbo.DeadlockTest SET Value = Value + 1 WHERE Id = 3;
COMMIT TRAN;

-- Session 2 - Inverse
BEGIN TRAN;
UPDATE dbo.DeadlockTest SET Value = Value + 1 WHERE Id = 3;
UPDATE dbo.DeadlockTest SET Value = Value + 1 WHERE Id = 2;
UPDATE dbo.DeadlockTest SET Value = Value + 1 WHERE Id = 1;
COMMIT TRAN;
```

### Variante B: Deadlock sur Index

```sql
-- Créer un index pour augmenter contention
CREATE INDEX IX_Value ON dbo.DeadlockTest(Value);

-- Session 1
BEGIN TRAN;
SELECT * FROM dbo.DeadlockTest WHERE Value > 0 WITH (HOLDLOCK);
UPDATE dbo.DeadlockTest SET Value = Value + 1 WHERE Id = 2;
COMMIT TRAN;

-- Session 2
BEGIN TRAN;
SELECT * FROM dbo.DeadlockTest WHERE Value > 0 WITH (HOLDLOCK);
UPDATE dbo.DeadlockTest SET Value = Value + 1 WHERE Id = 1;
COMMIT TRAN;
```

### Variante C: Conversion de verrou (Lock Conversion)

```sql
-- Session 1
BEGIN TRAN;
SELECT * FROM dbo.DeadlockTest WITH (UPDLOCK);
WAITFOR DELAY '00:00:03';
UPDATE dbo.DeadlockTest SET Value = Value + 1 WHERE Id = 1;
COMMIT TRAN;

-- Session 2
BEGIN TRAN;
SELECT * FROM dbo.DeadlockTest WITH (UPDLOCK);
UPDATE dbo.DeadlockTest SET Value = Value + 1 WHERE Id = 2;
COMMIT TRAN;
```

---

## 📊 Analyser les Deadlocks SQL Server

### Activer la trace de deadlock

```sql
-- Dans SQL Server Management Studio ou sqlcmd
USE master;
GO

DBCC TRACEON(1222, -1);  -- Enable deadlock graph
GO

-- Les deadlocks seront maintenant loggés dans l'Event Log
-- Vérifier: SQL Server logs → "Deadlock graph" 
```

### Voir l'historique des sessions bloquées

```sql
-- En temps réel
SELECT
    request_session_id,
    blocking_session_id,
    command,
    wait_type,
    wait_duration_ms
FROM sys.dm_exec_requests
WHERE blocking_session_id > 0;
GO

-- Détails des verrous
SELECT
    resource_type,
    resource_description,
    request_mode,
    request_type,
    request_status
FROM sys.dm_tran_locks
WHERE request_session_id = 123;  -- Remplacer 123 par session ID
GO
```

---

## 🔍 Simulation Automatisée (C#)

Pour reproduire directement depuis C#:

```csharp
// Ajouter dans Program.cs après les tests normaux

Console.WriteLine("\n3️⃣  Test 3: Simulation de Deadlock Chronométrée\n");
Console.WriteLine("-".PadRight(50, '-'));

try
{
    var task1 = Task.Run(() =>
    {
        using var conn = new SqlConnection(connectionString);
        conn.Open();
        using var tx = conn.BeginTransaction();

        conn.Execute("UPDATE dbo.DeadlockTest SET Value = Value + 1 WHERE Id = 1", transaction: tx);
        Thread.Sleep(2000); // Fenêtre de contention
        conn.Execute("UPDATE dbo.DeadlockTest SET Value = Value + 1 WHERE Id = 2", transaction: tx);

        tx.Commit();
        Console.WriteLine("✓ Task1 terminée");
    });

    var task2 = Task.Run(() =>
    {
        Thread.Sleep(500); // Décalage pour contention garantie
        
        using var conn = new SqlConnection(connectionString);
        conn.Open();
        using var tx = conn.BeginTransaction();

        conn.Execute("UPDATE dbo.DeadlockTest SET Value = Value + 1 WHERE Id = 2", transaction: tx);
        Thread.Sleep(2000);
        conn.Execute("UPDATE dbo.DeadlockTest SET Value = Value + 1 WHERE Id = 1", transaction: tx);

        tx.Commit();
        Console.WriteLine("✓ Task2 terminée");
    });

    Task.WaitAll(task1, task2);
    Console.WriteLine("✓ Deadlock reproduced et géré");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ Erreur: {ex.Message}");
}
```

---

## ✅ Checklist Validation Polly

- [ ] Le deadlock est bien reproduct (Msg 1205 visible)
- [ ] Polly log les retries ("RETRY 1", "RETRY 2")
- [ ] Les délais augmententexponentiellement (100ms, 200ms, 400ms)
- [ ] Au final, l'opération réussit
- [ ] Les données finales sont cohérentes

---

## 🚨 Troubleshooting

| Symptôme | Cause | Fix |
|----------|-------|-----|
| Pas de deadlock | Timing off | Augmenter WAITFOR (00:00:05) |
| Deadlock sur 1 session seulement | Normal | L'autre session gagne, c'est OK |
| "Timeout" au lieu de "1205" | Requête trop lente | Optimiser index, query |
| Polly ne retry pas | Exception pas `SqlException` | Vérifier type d'exception en log |
| Deadlock dans Polly mais retry infini | Max retries insuffisant | Augmenter `maxRetries` parameter |

---

**Tip**: Préférer les tests automatisés (`docker-compose up`)pour la CI/CD! 🤖
