-- Azure SQL Migration Script: TimeWindow String to Time Conversion
-- Bu script'i Azure SQL Database'de çalıştır
-- DİKKAT: Veri kaybına karşı öncesinde backup al!

USE [RotaAppDB]; -- Database adını değiştir gerekirse
GO

-- STEP 1: Önce mevcut string değerleri kontrol et
SELECT
    Id,
    Name,
    TimeWindowStart,
    TimeWindowEnd,
    CASE
        WHEN TimeWindowStart IS NULL THEN 'NULL'
        WHEN TRY_CAST(TimeWindowStart AS TIME) IS NULL THEN 'INVALID: ' + TimeWindowStart
        ELSE 'VALID'
    END AS StartStatus,
    CASE
        WHEN TimeWindowEnd IS NULL THEN 'NULL'
        WHEN TRY_CAST(TimeWindowEnd AS TIME) IS NULL THEN 'INVALID: ' + TimeWindowEnd
        ELSE 'VALID'
    END AS EndStatus
FROM Customers
WHERE TimeWindowStart IS NOT NULL OR TimeWindowEnd IS NOT NULL;

-- STEP 2: Geçici kolonlar oluştur
ALTER TABLE Customers
ADD TimeWindowStart_Temp TIME NULL,
    TimeWindowEnd_Temp TIME NULL;
GO

-- STEP 3: String değerleri TIME'a dönüştür
-- Sadece geçerli time formatındaki değerleri dönüştür
UPDATE Customers
SET
    TimeWindowStart_Temp = TRY_CAST(TimeWindowStart AS TIME),
    TimeWindowEnd_Temp = TRY_CAST(TimeWindowEnd AS TIME)
WHERE TimeWindowStart IS NOT NULL OR TimeWindowEnd IS NOT NULL;

-- STEP 4: Dönüştürme sonuçlarını kontrol et
SELECT
    Id,
    Name,
    TimeWindowStart AS OldStart,
    TimeWindowStart_Temp AS NewStart,
    TimeWindowEnd AS OldEnd,
    TimeWindowEnd_Temp AS NewEnd,
    CASE
        WHEN TimeWindowStart IS NOT NULL AND TimeWindowStart_Temp IS NULL THEN 'START CONVERSION FAILED'
        WHEN TimeWindowEnd IS NOT NULL AND TimeWindowEnd_Temp IS NULL THEN 'END CONVERSION FAILED'
        ELSE 'OK'
    END AS Status
FROM Customers
WHERE TimeWindowStart IS NOT NULL OR TimeWindowEnd IS NOT NULL;

-- STEP 5: Eski kolonları sil
ALTER TABLE Customers DROP COLUMN TimeWindowStart;
ALTER TABLE Customers DROP COLUMN TimeWindowEnd;
GO

-- STEP 6: Geçici kolonları yeniden adlandır
EXEC sp_rename 'Customers.TimeWindowStart_Temp', 'TimeWindowStart', 'COLUMN';
EXEC sp_rename 'Customers.TimeWindowEnd_Temp', 'TimeWindowEnd', 'COLUMN';
GO

-- STEP 7: RouteStops tablosu için de aynı işlem (eğer gerekiyorsa)
-- Önce ArriveBetweenStart ve ArriveBetweenEnd kolonlarının tipini kontrol et
SELECT
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'RouteStops'
  AND COLUMN_NAME IN ('ArriveBetweenStart', 'ArriveBetweenEnd');

-- Eğer RouteStops'ta da string ise, aşağıdaki adımları da çalıştır:
-- (Muhtemelen zaten TIME tipinde ama kontrol için)

/*
-- STEP 7a: RouteStops için geçici kolonlar (sadece gerekiyorsa)
ALTER TABLE RouteStops
ADD ArriveBetweenStart_Temp TIME NULL,
    ArriveBetweenEnd_Temp TIME NULL;
GO

-- STEP 7b: String değerleri TIME'a dönüştür (sadece gerekiyorsa)
UPDATE RouteStops
SET
    ArriveBetweenStart_Temp = TRY_CAST(ArriveBetweenStart AS TIME),
    ArriveBetweenEnd_Temp = TRY_CAST(ArriveBetweenEnd AS TIME)
WHERE ArriveBetweenStart IS NOT NULL OR ArriveBetweenEnd IS NOT NULL;

-- STEP 7c: Eski kolonları sil (sadece gerekiyorsa)
ALTER TABLE RouteStops DROP COLUMN ArriveBetweenStart;
ALTER TABLE RouteStops DROP COLUMN ArriveBetweenEnd;
GO

-- STEP 7d: Geçici kolonları yeniden adlandır (sadece gerekiyorsa)
EXEC sp_rename 'RouteStops.ArriveBetweenStart_Temp', 'ArriveBetweenStart', 'COLUMN';
EXEC sp_rename 'RouteStops.ArriveBetweenEnd_Temp', 'ArriveBetweenEnd', 'COLUMN';
GO
*/

-- STEP 8: Final doğrulama
SELECT 'Migration completed successfully!' AS Status;

-- Customers tablosu final kontrol
SELECT TOP 5
    Id,
    Name,
    TimeWindowStart,
    TimeWindowEnd
FROM Customers
WHERE TimeWindowStart IS NOT NULL OR TimeWindowEnd IS NOT NULL;

-- RouteStops tablosu final kontrol
SELECT TOP 5
    Id,
    Name,
    ArriveBetweenStart,
    ArriveBetweenEnd
FROM RouteStops
WHERE ArriveBetweenStart IS NOT NULL OR ArriveBetweenEnd IS NOT NULL;

PRINT 'Migration script completed!';
PRINT 'Check the results above to ensure all time values were converted correctly.';
PRINT 'If any conversions failed, you may need to clean/fix the data manually.';