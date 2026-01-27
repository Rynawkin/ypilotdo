-- ADIM 3: Geçici TIME kolonları oluştur ve veriyi dönüştür
-- Bu script'i Azure SQL Database'de çalıştır

USE [RotaAppDB];
GO

-- Geçici TIME kolonları ekle
ALTER TABLE Customers
ADD TimeWindowStart_New TIME NULL,
    TimeWindowEnd_New TIME NULL;
GO

-- String değerleri TIME formatına dönüştür
UPDATE Customers
SET TimeWindowStart_New = TRY_CAST(TimeWindowStart AS TIME),
    TimeWindowEnd_New = TRY_CAST(TimeWindowEnd AS TIME)
WHERE TimeWindowStart IS NOT NULL OR TimeWindowEnd IS NOT NULL;

-- Dönüştürme sonuçlarını kontrol et
SELECT
    COUNT(*) as TotalRecords,
    COUNT(TimeWindowStart) as HasStartTime,
    COUNT(TimeWindowStart_New) as ConvertedStartTime,
    COUNT(TimeWindowEnd) as HasEndTime,
    COUNT(TimeWindowEnd_New) as ConvertedEndTime,
    COUNT(CASE WHEN TimeWindowStart IS NOT NULL AND TimeWindowStart_New IS NULL THEN 1 END) as FailedStartConversions,
    COUNT(CASE WHEN TimeWindowEnd IS NOT NULL AND TimeWindowEnd_New IS NULL THEN 1 END) as FailedEndConversions
FROM Customers;

-- Başarısız dönüştürme varsa detayları göster
SELECT TOP 5
    Id, Name,
    TimeWindowStart as OldStart,
    TimeWindowStart_New as NewStart,
    TimeWindowEnd as OldEnd,
    TimeWindowEnd_New as NewEnd
FROM Customers
WHERE (TimeWindowStart IS NOT NULL AND TimeWindowStart_New IS NULL)
   OR (TimeWindowEnd IS NOT NULL AND TimeWindowEnd_New IS NULL);

PRINT 'Step 3 completed: Temporary TIME columns created and data converted.';
PRINT 'Review the results above. If FailedStartConversions and FailedEndConversions are 0, proceed to Step 4.';