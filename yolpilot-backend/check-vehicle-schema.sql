-- ===================================================================
-- YolPilot Database Schema Check Script
-- Bu script Vehicle tablosundaki CurrentKm alanını ve
-- ilgili değişiklikleri kontrol eder
-- ===================================================================

-- 1. Vehicle tablosunun tüm kolonlarını göster
SELECT
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Vehicles'
ORDER BY ORDINAL_POSITION;

PRINT '-----------------------------------';
PRINT 'Vehicle Table Columns Retrieved';
PRINT '-----------------------------------';

-- 2. CurrentKm alanını özellikle kontrol et
IF EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Vehicles'
    AND COLUMN_NAME = 'CurrentKm'
)
BEGIN
    PRINT '✓ CurrentKm alanı mevcut';

    SELECT
        COLUMN_NAME as [Alan],
        DATA_TYPE as [Veri Tipi],
        IS_NULLABLE as [Null Olabilir],
        COLUMN_DEFAULT as [Varsayılan Değer]
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Vehicles'
    AND COLUMN_NAME = 'CurrentKm';
END
ELSE
BEGIN
    PRINT '✗ CurrentKm alanı bulunamadı!';
    PRINT 'Migration çalıştırılması gerekiyor!';
END

PRINT '';
PRINT '-----------------------------------';
PRINT 'Sample Vehicle Data (First 10)';
PRINT '-----------------------------------';

-- 3. İlk 10 aracın kilometre bilgisini göster
SELECT TOP 10
    Id,
    PlateNumber as [Plaka],
    Brand as [Marka],
    Model,
    CurrentKm as [Kilometre],
    Status as [Durum],
    CreatedAt as [Oluşturulma]
FROM Vehicles
ORDER BY Id DESC;

PRINT '';
PRINT '-----------------------------------';
PRINT 'Journey Completion Statistics';
PRINT '-----------------------------------';

-- 4. Son tamamlanan seferlerdeki kilometre güncellemelerini kontrol et
SELECT TOP 20
    j.Id as [Sefer ID],
    v.PlateNumber as [Plaka],
    v.CurrentKm as [Güncel KM],
    j.Status as [Sefer Durumu],
    j.StartedAt as [Başlangıç],
    j.CompletedAt as [Bitiş]
FROM Journeys j
INNER JOIN Vehicles v ON j.VehicleId = v.Id
WHERE j.Status = 'Completed'
ORDER BY j.CompletedAt DESC;

PRINT '';
PRINT '-----------------------------------';
PRINT 'Journey Stops - Recent Completions';
PRINT '-----------------------------------';

-- 5. Son tamamlanan durakların durumunu kontrol et (depo dönüşleri dahil)
SELECT TOP 20
    js.Id as [Durak ID],
    js.JourneyId as [Sefer ID],
    CASE
        WHEN js.RouteStopId IS NULL THEN 'DEPO DÖNÜŞÜ'
        ELSE CAST(js.RouteStopId as VARCHAR)
    END as [Rota Durağı],
    js.Status as [Durum],
    js.CheckInTime as [Varış],
    js.CheckOutTime as [Çıkış]
FROM JourneyStops js
WHERE js.Status = 'Completed'
ORDER BY js.UpdatedAt DESC;

PRINT '';
PRINT '-----------------------------------';
PRINT 'JourneyStatus Notes (Last 10)';
PRINT '-----------------------------------';

-- 6. Journey status tablosundaki notları kontrol et (kilometre bilgisi burada kaydediliyor mu?)
SELECT TOP 10
    JourneyId as [Sefer ID],
    StopId as [Durak ID],
    Type as [Tip],
    Notes as [Notlar],
    ReceiverName as [Teslim Alan],
    CreatedAt as [Tarih]
FROM JourneyStatuses
WHERE Notes IS NOT NULL
ORDER BY CreatedAt DESC;

PRINT '';
PRINT '-----------------------------------';
PRINT 'Vehicle Count by Status';
PRINT '-----------------------------------';

-- 7. Araç durumlarına göre sayılar
SELECT
    Status as [Durum],
    COUNT(*) as [Adet],
    AVG(CAST(CurrentKm as FLOAT)) as [Ortalama KM],
    MAX(CurrentKm) as [Maksimum KM]
FROM Vehicles
WHERE DeletedAt IS NULL
GROUP BY Status;

PRINT '';
PRINT '===================================';
PRINT 'Schema Check Complete!';
PRINT '===================================';
