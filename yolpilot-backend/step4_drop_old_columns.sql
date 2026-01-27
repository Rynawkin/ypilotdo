-- ADIM 4: Eski string kolonları sil
-- Bu script'i Azure SQL Database'de çalıştır

USE [RotaAppDB];
GO

-- Eski string kolonları sil
ALTER TABLE Customers DROP COLUMN TimeWindowStart;
ALTER TABLE Customers DROP COLUMN TimeWindowEnd;
GO

PRINT 'Step 4 completed: Old string columns dropped.';
PRINT 'Proceed to Step 5 to rename temporary columns.';