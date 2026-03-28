-- init.sql
-- Script d'initialisation pour le test de deadlock

-- Attendre un peu pour que SQL Server soit complètement prêt
WAITFOR DELAY '00:00:02';

-- Créer la base de données
IF DB_ID('DeadlockTestDb') IS NULL
BEGIN
    CREATE DATABASE DeadlockTestDb;
    PRINT 'Database DeadlockTestDb créée';
END;
GO

USE DeadlockTestDb;
GO

-- Créer la table test
IF OBJECT_ID('dbo.DeadlockTest', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.DeadlockTest;
    PRINT 'Table DeadlockTest supprimée';
END;
GO

CREATE TABLE dbo.DeadlockTest
(
    Id INT NOT NULL PRIMARY KEY
    ,Value INT NOT NULL DEFAULT 0
    ,LastUpdated DATETIME2 DEFAULT GETUTCDATE()
);
PRINT 'Table DeadlockTest créée';

-- Insérer les données initiales
INSERT INTO dbo.DeadlockTest
    (Id, Value)
VALUES
    (1 ,0)
    ,(2 ,0);

PRINT 'Données initiales insérées';
GO

-- Créer un index pour les tests
CREATE NONCLUSTERED INDEX IX_DeadlockTest_Value 
ON dbo.DeadlockTest (Value);
PRINT 'Index créé';
GO
