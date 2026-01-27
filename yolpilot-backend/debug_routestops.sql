-- RouteStops tablosundaki tüm kolonları görmek için
-- En son oluşturulan route'un stops'larını göster
SELECT TOP 20
    rs.Id,
    rs.RouteId,
    rs.CustomerId,
    c.Name as CustomerName,
    rs.Name as StopName,
    rs.Address,
    rs.Latitude,
    rs.Longitude,
    rs.[Order],
    rs.OrderType,
    rs.Type,
    rs.ArriveBetweenStart,
    rs.ArriveBetweenEnd,
    rs.ServiceTime,
    rs.EstimatedArrivalTime,
    rs.EstimatedDepartureTime,
    rs.SignatureRequired,
    rs.PhotoRequired,
    rs.ProofOfDeliveryRequired,
    rs.IsExcluded,
    rs.ExclusionReason,
    rs.Notes,
    rs.ContactFullName,
    rs.ContactPhone,
    rs.ContactEmail,
    rs.CreatedAt,
    rs.UpdatedAt,
    rs.IsDeleted
FROM RouteStops rs
LEFT JOIN Customers c ON c.Id = rs.CustomerId
WHERE rs.RouteId IN (
    -- En son oluşturulan 3 route'u al
    SELECT TOP 3 Id FROM Routes ORDER BY CreatedAt DESC
)
ORDER BY rs.RouteId DESC, rs.[Order] ASC;

-- Alternatif: Belirli bir Route ID için
-- SELECT * FROM RouteStops WHERE RouteId = 353 ORDER BY [Order];

-- Route bilgilerini de görmek için:
SELECT TOP 5
    r.Id,
    r.Name,
    r.Date,
    r.Status,
    r.Optimized,
    r.TotalDeliveries,
    r.CreatedAt,
    COUNT(rs.Id) as StopCount,
    SUM(CASE WHEN rs.ArriveBetweenStart IS NOT NULL THEN 1 ELSE 0 END) as StopsWithTimeWindowStart,
    SUM(CASE WHEN rs.ArriveBetweenEnd IS NOT NULL THEN 1 ELSE 0 END) as StopsWithTimeWindowEnd
FROM Routes r
LEFT JOIN RouteStops rs ON rs.RouteId = r.Id AND rs.IsDeleted = 0
GROUP BY r.Id, r.Name, r.Date, r.Status, r.Optimized, r.TotalDeliveries, r.CreatedAt
ORDER BY r.CreatedAt DESC;