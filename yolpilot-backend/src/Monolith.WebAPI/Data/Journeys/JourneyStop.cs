using Monolith.WebAPI.External.Google.Models;
using System;

namespace Monolith.WebAPI.Data.Journeys;

public class JourneyStop : BaseEntity
{
    // do not remove this constructor
    public JourneyStop()
    {
    }

    public JourneyStop(int journeyId, int stopId, int order,
        double distance,
        string startAddress, double startLatitude, double startLongitude,
        string endAddress, double endLatitude, double endLongitude,
        TimeSpan estimatedArrivalTime, TimeSpan estimatedDepartureTime,
        TimeSpan? arriveBetweenStart, TimeSpan? arriveBetweenEnd)
    {
        JourneyId = journeyId;
        StopId = stopId;
        Order = order;

        Distance = distance;

        StartAddress = startAddress;
        StartLatitude = startLatitude;
        StartLongitude = startLongitude;

        EndAddress = endAddress;
        EndLatitude = endLatitude;
        EndLongitude = endLongitude;

        EstimatedArrivalTime = estimatedArrivalTime;
        EstimatedDepartureTime = estimatedDepartureTime;

        ArriveBetweenStart = arriveBetweenStart;
        ArriveBetweenEnd = arriveBetweenEnd;
        
        // Yeni eklenen field'lar için default değerler
        RouteStopId = stopId;
        Status = JourneyStopStatus.Pending;
    }

    // YENİ MODEL İLE ÇALIŞAN CONSTRUCTOR
    public JourneyStop(int journeyId, RouteStop routeStop, int order, Leg leg,
        TimeSpan prevEstimatedDepartureTime, TimeSpan defaultServiceTime)
    {
        JourneyId = journeyId;
        StopId = routeStop.Id;
        Order = order;
        Distance = leg.Distance.Value / 1000.0;
        StartAddress = leg.StartAddress;
        StartLatitude = leg.StartLocation.Lat;
        StartLongitude = leg.StartLocation.Lng;
        EndAddress = leg.EndAddress;
        EndLatitude = leg.EndLocation.Lat;
        EndLongitude = leg.EndLocation.Lng;

        ArriveBetweenStart = routeStop.ArriveBetweenStart;
        ArriveBetweenEnd = routeStop.ArriveBetweenEnd;

        EstimatedArrivalTime = prevEstimatedDepartureTime + TimeSpan.FromSeconds(leg.Duration.Value);
        EstimatedDepartureTime = routeStop.ServiceTime.HasValue
            ? prevEstimatedDepartureTime + TimeSpan.FromSeconds(leg.Duration.Value) + routeStop.ServiceTime.Value
            : prevEstimatedDepartureTime + TimeSpan.FromSeconds(leg.Duration.Value) + defaultServiceTime;
        
        // Yeni eklenen field'lar için default değerler
        RouteStopId = routeStop.Id;
        Status = JourneyStopStatus.Pending;
    }

    // ESKİ MODEL İLE UYUMLULUK İÇİN (GEÇİCİ - İLERİDE SİLİNEBİLİR)
    public JourneyStop(int journeyId, RouteStop routeStop, int order, GoogleDirectionsResponseLeg leg,
        TimeSpan prevEstimatedDepartureTime, TimeSpan defaultServiceTime)
    {
        JourneyId = journeyId;
        StopId = routeStop.Id;
        Order = order;
        Distance = leg.distance.value / 1000.0;
        StartAddress = leg.start_address;
        StartLatitude = leg.start_location.lat;
        StartLongitude = leg.start_location.lng;
        EndAddress = leg.end_address;
        EndLatitude = leg.end_location.lat;
        EndLongitude = leg.end_location.lng;

        ArriveBetweenStart = routeStop.ArriveBetweenStart;
        ArriveBetweenEnd = routeStop.ArriveBetweenEnd;

        EstimatedArrivalTime = prevEstimatedDepartureTime + TimeSpan.FromSeconds(leg.duration.value);
        EstimatedDepartureTime = routeStop.ServiceTime.HasValue
            ? prevEstimatedDepartureTime + TimeSpan.FromSeconds(leg.duration.value) + routeStop.ServiceTime.Value
            : prevEstimatedDepartureTime + TimeSpan.FromSeconds(leg.duration.value) + defaultServiceTime;
        
        // Yeni eklenen field'lar için default değerler
        RouteStopId = routeStop.Id;
        Status = JourneyStopStatus.Pending;
    }

    // BaseEntity'den geliyor
    // public int Id { get; set; }
    // public DateTime CreatedAt { get; set; }
    // public DateTime UpdatedAt { get; set; }
    
    public int JourneyId { get; set; }
    public int StopId { get; set; }
    
    // EKLENEN FIELD
    public int RouteStopId { get; set; }
    
    // EKLENEN FIELD
    public JourneyStopStatus Status { get; set; }
    
    // ✅ V38 - YENİ EKLENEN FIELD'LAR
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }

    public int Order { get; set; }

    /// <summary>
    /// Distance in kilometers
    /// </summary>
    public double Distance { get; set; }

    public string StartAddress { get; set; }
    public double StartLatitude { get; set; }
    public double StartLongitude { get; set; }
    public string EndAddress { get; set; }
    public double EndLatitude { get; set; }
    public double EndLongitude { get; set; }

    public TimeSpan EstimatedArrivalTime { get; set; }
    public TimeSpan? EstimatedDepartureTime { get; set; }

    // ✅ YENİ - Original ETA'lar (planlanan zamanlar - analiz için)
    public TimeSpan OriginalEstimatedArrivalTime { get; set; }
    public TimeSpan OriginalEstimatedDepartureTime { get; set; }

    public TimeSpan? ArriveBetweenStart { get; set; }
    public TimeSpan? ArriveBetweenEnd { get; set; }

    // ✅ YENİ - Gecikme Sebebi Tracking
    /// <summary>
    /// Gecikme sebebi kategorisi
    /// </summary>
    public DelayReasonCategory? DelayReasonCategory { get; set; }

    /// <summary>
    /// Gecikme sebebi detaylı açıklama
    /// </summary>
    public string DelayReason { get; set; }

    /// <summary>
    /// Bu durağa özgü yeni gecikme (dakika)
    /// </summary>
    public int NewDelay { get; set; }

    /// <summary>
    /// Bu durağa kadar toplam kümülatif gecikme (dakika)
    /// </summary>
    public int CumulativeDelay { get; set; }

    public Journey Journey { get; set; }
    public RouteStop RouteStop { get; set; }
    
    // ✅ YENİ EKLENEN - Status güncelleme metodu
    public void UpdateStatus(JourneyStopStatus newStatus)
    {
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gerçek gecikmeyi hesaplar (Planlanan vs Gerçekleşen - dakika cinsinden)
    /// </summary>
    public int CalculateActualDelay(TimeZoneInfo timezone)
    {
        if (!CheckInTime.HasValue)
            return 0;

        // ✅ FALLBACK: Eğer OriginalETA henüz set edilmemişse (eski journey'ler için), mevcut ETA'yı kullan
        var plannedArrivalTime = OriginalEstimatedArrivalTime != TimeSpan.Zero
            ? OriginalEstimatedArrivalTime
            : EstimatedArrivalTime;

        if (plannedArrivalTime == TimeSpan.Zero)
            return 0;

        var checkInTimeLocal = TimeZoneInfo.ConvertTimeFromUtc(CheckInTime.Value, timezone);
        var checkInMinutes = checkInTimeLocal.Hour * 60 + checkInTimeLocal.Minute;
        var plannedMinutes = (int)plannedArrivalTime.TotalMinutes;

        // ✅ BUGFIX: Gece yarısı geçişini düzelt
        // Eğer plannedMinutes > 1200 (20:00+) ve checkInMinutes < 360 (06:00-) ise,
        // muhtemelen gece yarısı geçilmiş, checkInMinutes'e 24 saat ekle
        var delayMinutes = checkInMinutes - plannedMinutes;
        if (plannedMinutes > 1200 && checkInMinutes < 360 && delayMinutes < -600)
        {
            // Gece yarısı geçişi tespit edildi, checkInMinutes'e 24 saat (1440 dakika) ekle
            delayMinutes = (checkInMinutes + 1440) - plannedMinutes;
        }

        return delayMinutes;
    }

    /// <summary>
    /// Yeni gecikmeyi hesaplar (Mevcut gecikme - Önceki kümülatif gecikme)
    /// </summary>
    public int CalculateNewDelay(int previousCumulativeDelay, TimeZoneInfo timezone)
    {
        var actualDelay = CalculateActualDelay(timezone);
        return actualDelay - previousCumulativeDelay;
    }

    /// <summary>
    /// Gecikme sebebini ayarlar
    /// </summary>
    public void SetDelayReason(DelayReasonCategory? category, string reason, int newDelay, int cumulativeDelay)
    {
        DelayReasonCategory = category;
        DelayReason = reason;
        NewDelay = newDelay;
        CumulativeDelay = cumulativeDelay;
        UpdatedAt = DateTime.UtcNow;
    }
}

// EKLENEN ENUM
public enum JourneyStopStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Skipped
}

// ✅ YENİ - Gecikme Sebebi Kategorileri
public enum DelayReasonCategory
{
    Traffic = 1,              // Trafik Yoğunluğu
    CustomerNotReady = 2,     // Müşteri Hazır Değil
    VehicleIssue = 3,         // Araç Arızası
    Weather = 4,              // Hava Koşulları
    UnloadingDelay = 5,       // Yükleme/Boşaltma Gecikmesi
    RouteChange = 6,          // Rota Değişikliği
    AccidentArea = 7,         // Kaza Bölgesi
    BreakTime = 8,            // Yemek/Mola
    Other = 9                 // Diğer
}