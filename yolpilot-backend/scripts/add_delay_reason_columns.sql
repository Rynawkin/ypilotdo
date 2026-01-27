-- =============================================
-- Add Delay Reason Columns to JourneyStops
-- Date: 2025-10-08
-- Description: Gecikme sebebi tracking için kolonlar ekleniyor
-- =============================================

-- 1. DelayReasonCategory kolonu (enum değerler için)
ALTER TABLE JourneyStops
ADD DelayReasonCategory INT NULL;

-- 2. DelayReason kolonu (serbest metin açıklama için)
ALTER TABLE JourneyStops
ADD DelayReason NVARCHAR(500) NULL;

-- 3. NewDelay kolonu (bu durağa özgü yeni gecikme - dakika cinsinden)
ALTER TABLE JourneyStops
ADD NewDelay INT NOT NULL DEFAULT 0;

-- 4. CumulativeDelay kolonu (bu durağa kadar toplam gecikme - dakika cinsinden)
ALTER TABLE JourneyStops
ADD CumulativeDelay INT NOT NULL DEFAULT 0;

GO

-- İndeks oluştur (performans için)
CREATE NONCLUSTERED INDEX IX_JourneyStops_DelayReasonCategory
ON JourneyStops(DelayReasonCategory)
WHERE DelayReasonCategory IS NOT NULL;

GO

-- Trigger oluştur (CumulativeDelay otomatik güncelleme için)
CREATE OR ALTER TRIGGER TR_JourneyStops_UpdateCumulativeDelay
ON JourneyStops
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Sadece CheckInTime güncellendiğinde çalış
    IF UPDATE(CheckInTime)
    BEGIN
        -- Güncellenen her stop için
        UPDATE js
        SET
            CumulativeDelay = ISNULL(
                (
                    SELECT SUM(ISNULL(NewDelay, 0))
                    FROM JourneyStops
                    WHERE JourneyId = js.JourneyId
                    AND [Order] <= js.[Order]
                ), 0
            )
        FROM JourneyStops js
        INNER JOIN inserted i ON js.Id = i.Id;
    END
END;

GO

-- Verification: Kolonların eklendiğini kontrol et
SELECT
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'JourneyStops'
AND COLUMN_NAME IN ('DelayReasonCategory', 'DelayReason', 'NewDelay', 'CumulativeDelay')
ORDER BY ORDINAL_POSITION;

GO

PRINT 'Migration completed successfully!';
PRINT 'Added columns: DelayReasonCategory, DelayReason, NewDelay, CumulativeDelay';
