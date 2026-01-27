-- Database schema'yı kontrol et
-- RouteStops tablosu column tipleri
SELECT
    c.COLUMN_NAME,
    c.DATA_TYPE,
    c.IS_NULLABLE,
    c.CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS c
WHERE c.TABLE_NAME = 'RouteStops'
AND c.COLUMN_NAME IN ('ArriveBetweenStart', 'ArriveBetweenEnd')
ORDER BY c.ORDINAL_POSITION;

-- Customers tablosu column tipleri
SELECT
    c.COLUMN_NAME,
    c.DATA_TYPE,
    c.IS_NULLABLE,
    c.CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS c
WHERE c.TABLE_NAME = 'Customers'
AND c.COLUMN_NAME IN ('TimeWindowStart', 'TimeWindowEnd')
ORDER BY c.ORDINAL_POSITION;

-- Sample data kontrolü
SELECT TOP 5
    Id,
    Name,
    ArriveBetweenStart,
    ArriveBetweenEnd,
    SQL_VARIANT_PROPERTY(ArriveBetweenStart, 'BaseType') as StartType,
    SQL_VARIANT_PROPERTY(ArriveBetweenEnd, 'BaseType') as EndType
FROM RouteStops
WHERE ArriveBetweenStart IS NOT NULL OR ArriveBetweenEnd IS NOT NULL;