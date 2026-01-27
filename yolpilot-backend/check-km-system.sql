-- ===================================================================
-- Kilometre Takip Sistemi Kontrol Script'i
-- Bu script tüm kilometre takip sistemini kontrol eder
-- ===================================================================

PRINT '===================================================================';
PRINT 'ADIM 1: VEHICLES TABLOSU - CurrentKm ALANI KONTROLÜ';
PRINT '===================================================================';

-- Vehicles tablosunda CurrentKm alanı var mı kontrol et
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
    PRINT 'Lütfen şu komutu çalıştırın:';
    PRINT 'ALTER TABLE Vehicles ADD CurrentKm INT NULL;';
END

PRINT '';
PRINT '===================================================================';
PRINT 'ADIM 2: JOURNEYS TABLOSU - StartKm VE EndKm ALANLARI KONTROLÜ';
PRINT '===================================================================';

-- Journeys tablosunda StartKm alanı var mı
IF EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Journeys'
    AND COLUMN_NAME = 'StartKm'
)
BEGIN
    PRINT '✓ StartKm alanı mevcut';
END
ELSE
BEGIN
    PRINT '✗ StartKm alanı bulunamadı!';
    PRINT 'Lütfen şu komutu çalıştırın:';
    PRINT 'ALTER TABLE Journeys ADD StartKm INT NULL;';
END

-- Journeys tablosunda EndKm alanı var mı
IF EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Journeys'
    AND COLUMN_NAME = 'EndKm'
)
BEGIN
    PRINT '✓ EndKm alanı mevcut';

    -- Her iki alan da varsa detayları göster
    SELECT
        COLUMN_NAME as [Alan],
        DATA_TYPE as [Veri Tipi],
        IS_NULLABLE as [Null Olabilir]
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Journeys'
    AND COLUMN_NAME IN ('StartKm', 'EndKm')
    ORDER BY COLUMN_NAME;
END
ELSE
BEGIN
    PRINT '✗ EndKm alanı bulunamadı!';
    PRINT 'Lütfen şu komutu çalıştırın:';
    PRINT 'ALTER TABLE Journeys ADD EndKm INT NULL;';
END

PRINT '';
PRINT '===================================================================';
PRINT 'ADIM 3: ARAÇLARIN GÜNCEL KİLOMETRE BİLGİLERİ';
PRINT '===================================================================';

-- Tüm araçların kilometre durumu
SELECT
    Id as [Araç ID],
    PlateNumber as [Plaka],
    Brand as [Marka],
    Model,
    CurrentKm as [Güncel KM],
    Status as [Durum],
    CASE
        WHEN CurrentKm IS NULL THEN '⚠️ Kilometre Girilmemiş'
        WHEN CurrentKm = 0 THEN '⚠️ Kilometre Sıfır'
        ELSE '✓ OK'
    END as [Kontrol]
FROM Vehicles
WHERE DeletedAt IS NULL
ORDER BY Id DESC;

PRINT '';
PRINT '===================================================================';
PRINT 'ADIM 4: SON 10 SEFERİN KİLOMETRE BİLGİLERİ';
PRINT '===================================================================';

-- Son 10 seferin kilometre bilgileri
SELECT TOP 10
    j.Id as [Sefer ID],
    j.Name as [Sefer Adı],
    v.PlateNumber as [Plaka],
    j.StartKm as [Başlangıç KM],
    j.EndKm as [Bitiş KM],
    CASE
        WHEN j.EndKm IS NOT NULL AND j.StartKm IS NOT NULL
        THEN j.EndKm - j.StartKm
        ELSE NULL
    END as [Kat Edilen KM],
    v.CurrentKm as [Araç Güncel KM],
    CASE
        WHEN j.Status = 100 THEN 'Planned'
        WHEN j.Status = 200 THEN 'InProgress'
        WHEN j.Status = 300 THEN 'Completed'
        WHEN j.Status = 400 THEN 'Cancelled'
        ELSE 'Unknown'
    END as [Durum],
    j.CreatedAt as [Oluşturulma],
    j.CompletedAt as [Tamamlanma]
FROM Journeys j
INNER JOIN Vehicles v ON j.VehicleId = v.Id
ORDER BY j.CreatedAt DESC;

PRINT '';
PRINT '===================================================================';
PRINT 'ADIM 5: KİLOMETRE TUTARSIZLIKLARI';
PRINT '===================================================================';

-- Kilometre tutarsızlıkları kontrol et
SELECT
    j.Id as [Sefer ID],
    j.Name as [Sefer Adı],
    v.PlateNumber as [Plaka],
    j.StartKm as [Başlangıç KM],
    j.EndKm as [Bitiş KM],
    v.CurrentKm as [Araç Güncel KM],
    CASE
        WHEN j.StartKm IS NULL THEN '⚠️ Başlangıç KM girilmemiş'
        WHEN j.Status = 300 AND j.EndKm IS NULL THEN '⚠️ Tamamlanmış sefer ama Bitiş KM yok'
        WHEN j.EndKm IS NOT NULL AND j.StartKm IS NOT NULL AND j.EndKm < j.StartKm THEN '❌ Bitiş KM < Başlangıç KM'
        WHEN j.Status = 300 AND v.CurrentKm != j.EndKm THEN '⚠️ Araç KM ile Sefer Bitiş KM eşleşmiyor'
        ELSE '✓ OK'
    END as [Sorun]
FROM Journeys j
INNER JOIN Vehicles v ON j.VehicleId = v.Id
WHERE j.CreatedAt > DATEADD(DAY, -30, GETDATE()) -- Son 30 gün
ORDER BY j.CreatedAt DESC;

PRINT '';
PRINT '===================================================================';
PRINT 'ADIM 6: İSTATİSTİKLER';
PRINT '===================================================================';

-- Genel istatistikler
SELECT
    COUNT(*) as [Toplam Araç],
    COUNT(CASE WHEN CurrentKm IS NOT NULL THEN 1 END) as [KM Bilgisi Olan],
    COUNT(CASE WHEN CurrentKm IS NULL THEN 1 END) as [KM Bilgisi Olmayan],
    AVG(CAST(CurrentKm as FLOAT)) as [Ortalama KM],
    MAX(CurrentKm) as [En Yüksek KM],
    MIN(CurrentKm) as [En Düşük KM]
FROM Vehicles
WHERE DeletedAt IS NULL;

-- Sefer istatistikleri
SELECT
    COUNT(*) as [Toplam Sefer],
    COUNT(CASE WHEN StartKm IS NOT NULL THEN 1 END) as [Başlangıç KM Olan],
    COUNT(CASE WHEN EndKm IS NOT NULL THEN 1 END) as [Bitiş KM Olan],
    COUNT(CASE WHEN StartKm IS NOT NULL AND EndKm IS NOT NULL THEN 1 END) as [Her İkisi de Olan],
    AVG(CASE WHEN EndKm IS NOT NULL AND StartKm IS NOT NULL THEN EndKm - StartKm END) as [Ortalama Kat Edilen KM]
FROM Journeys
WHERE CreatedAt > DATEADD(DAY, -30, GETDATE()); -- Son 30 gün

PRINT '';
PRINT '===================================================================';
PRINT 'ADIM 7: SON GÜNCELLENEN ARAÇLAR';
PRINT '===================================================================';

-- En son kilometresi güncellenen araçlar
SELECT TOP 10
    v.Id as [Araç ID],
    v.PlateNumber as [Plaka],
    v.CurrentKm as [Güncel KM],
    v.UpdatedAt as [Son Güncelleme],
    j.Id as [Son Sefer ID],
    j.StartKm as [Sefer Başlangıç KM],
    j.EndKm as [Sefer Bitiş KM]
FROM Vehicles v
LEFT JOIN Journeys j ON j.VehicleId = v.Id
    AND j.Id = (
        SELECT TOP 1 Id
        FROM Journeys
        WHERE VehicleId = v.Id
        ORDER BY CreatedAt DESC
    )
WHERE v.DeletedAt IS NULL
ORDER BY v.UpdatedAt DESC;

PRINT '';
PRINT '===================================================================';
PRINT 'KONTROL TAMAMLANDI!';
PRINT '===================================================================';
PRINT '';
PRINT 'Yukarıdaki sonuçları kontrol edin:';
PRINT '1. Tüm tablolarda gerekli alanlar var mı?';
PRINT '2. Araçların kilometre bilgileri girilmiş mi?';
PRINT '3. Seferlerde kilometre takibi çalışıyor mu?';
PRINT '4. Herhangi bir tutarsızlık var mı?';
PRINT '';
