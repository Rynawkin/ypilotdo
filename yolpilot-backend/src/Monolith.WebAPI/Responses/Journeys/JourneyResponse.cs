// src/Monolith.WebAPI/Responses/Journeys/JourneyResponse.cs

#nullable enable

using Monolith.WebAPI.Data.Journeys;
using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.Responses.Workspace;

namespace Monolith.WebAPI.Responses.Journeys;

public class JourneyResponse
{
    public JourneyResponse(Journey journey)
    {
        Id = journey.Id;
        Name = journey.Name;
        RouteId = journey.RouteId;
        Polyline = journey?.Polyline?.Replace(@"\\", @"\");
        Date = journey.Date.ToString("yyyy-MM-dd");
        AssignedAt = journey.CreatedAt;

        StartedAt = journey.StartedAt;
        FinishedAt = journey.FinishedAt;

        // ✅ Kilometre bilgileri
        StartKm = journey.StartKm;
        EndKm = journey.EndKm;

        // ✅ Yakıt ve araç durumu bilgileri
        StartFuel = journey.StartFuel;
        EndFuel = journey.EndFuel;
        VehicleCondition = journey.VehicleCondition;

        // ✅ Optimizasyon bayrağı
        NeedsReoptimization = journey.NeedsReoptimization;

        StartDetails = journey.StartDetails is not null ? new StartDetailsResponse(journey.StartDetails) : null;
        EndDetails = journey.EndDetails is not null ? new EndDetailsResponse(journey.EndDetails) : null;
        Driver = journey.Driver is not null ? new JourneyDriverResponse(journey.Driver) : null;

        Stops = journey.Stops?.OrderBy(x => x.Order).Select(x => new JourneyStopResponse(x)).ToList();

        Statuses = journey.Statuses?
            .OrderBy(x => x.CreatedAt)
            .Select(x => new JourneyStatusResponse(x)).ToList();

        Route = journey.Route is not null ? new RouteResponse(journey.Route) : null;
        
        // YENÄ° EKLENEN ALANLAR - Route'dan alÄ±nÄ±yor
        TotalDistance = journey.Route?.TotalDistance ?? 0;
        TotalDuration = journey.Route?.TotalDuration ?? 0;
        
        // Journey status'Ã¼ - DÃœZELTÄ°LDÄ°
        Status = DetermineJourneyStatus(journey);
        
        // DEBUG LOG EKLE
        Console.WriteLine($"[DEBUG] Journey #{journey.Id} - Entity Status: {journey.Status}, Response Status: {Status}, StartedAt: {journey.StartedAt}");
        
        // Current stop index - DÃœZELTÄ°LDÄ° (Array index olarak, Order deÄŸil)
        if (journey.Stops != null && journey.Stops.Any())
        {
            var currentStop = journey.Stops
                .OrderBy(x => x.Order)
                .FirstOrDefault(s => s.Status == JourneyStopStatus.Pending || 
                                    s.Status == JourneyStopStatus.InProgress);
            
            if (currentStop != null)
            {
                // Order 1'den baÅŸladÄ±ÄŸÄ± iÃ§in index iÃ§in -1 yapÄ±yoruz
                CurrentStopIndex = currentStop.Order - 1;
            }
            else
            {
                // TÃ¼m stop'lar tamamlanmÄ±ÅŸsa son stop'un index'i
                CurrentStopIndex = journey.Stops.Count - 1;
            }
        }
        else
        {
            CurrentStopIndex = 0;
        }
        
        // Live location (ileride SignalR ile gÃ¼ncellenecek)
        LiveLocation = null;
    }
    
    // DÃœZELTÄ°LDÄ° - Journey entity'sindeki Status enum'Ä±nÄ± kullan
    private string DetermineJourneyStatus(Journey journey)
    {
        // Journey entity'sindeki Status enum'Ä±nÄ± kullan
        return journey.Status switch
        {
            JourneyStatusEnum.Planned => "planned",
            JourneyStatusEnum.InProgress => "in_progress",
            JourneyStatusEnum.Completed => "completed",
            JourneyStatusEnum.Cancelled => "cancelled",
            JourneyStatusEnum.OnHold => "on_hold",
            _ => "planned" // Default olarak planned
        };
    }

    public int Id { get; set; }
    public string? Name { get; set; }
    public int RouteId { get; set; }
    public string Polyline { get; set; }
    public string Date { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }

    // ✅ Kilometre bilgileri
    public int? StartKm { get; set; }
    public int? EndKm { get; set; }

    // ✅ Yakıt ve araç durumu bilgileri
    public string? StartFuel { get; set; }
    public string? EndFuel { get; set; }
    public string? VehicleCondition { get; set; }

    // ✅ YENİ - Optimizasyon gerekiyor mu?
    public bool NeedsReoptimization { get; set; }

    public StartDetailsResponse StartDetails { get; set; }
    public EndDetailsResponse EndDetails { get; set; }
    public JourneyDriverResponse Driver { get; set; }
    public List<JourneyStopResponse> Stops { get; set; }
    public List<JourneyStatusResponse> Statuses { get; set; }
    public RouteResponse Route { get; set; }
    
    // YENÄ° EKLENEN ALANLAR
    public double TotalDistance { get; set; }
    public int TotalDuration { get; set; }
    public string Status { get; set; }
    public int CurrentStopIndex { get; set; }
    public LiveLocationResponse LiveLocation { get; set; }
}

// YENÄ° - Live location iÃ§in response class
public class LiveLocationResponse
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Speed { get; set; }
    public double? Heading { get; set; }
    public DateTime Timestamp { get; set; }
}

public class JourneyDriverResponse
{
    public JourneyDriverResponse(Driver driver)
    {
        Id = driver.Id.ToString();
        FullName = driver.Name;
        Email = driver.Email ?? "";
        PhoneNumber = driver.Phone;
    }

    public string Id { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
}

public class JourneyStopResponse
{
    public bool? IsExcluded { get; set; }
    public JourneyStopResponse(JourneyStop journeyStop)
    {
        Id = journeyStop.Id;
        JourneyId = journeyStop.JourneyId;
        StopId = journeyStop.StopId;
        RouteStopId = journeyStop.RouteStopId;
        Order = journeyStop.Order;
        Status = journeyStop.Status.ToString().ToLowerInvariant();
        IsExcluded = journeyStop.RouteStop?.IsExcluded;

        Distance = journeyStop.Distance;
        StartAddress = journeyStop.StartAddress;
        StartLatitude = journeyStop.StartLatitude;
        StartLongitude = journeyStop.StartLongitude;
        EndAddress = journeyStop.EndAddress;
        EndLatitude = journeyStop.EndLatitude;
        EndLongitude = journeyStop.EndLongitude;
        
        EstimatedArrivalTime = journeyStop.EstimatedArrivalTime.ToString(@"hh\:mm\:ss");
        EstimatedDepartureTime = journeyStop.EstimatedDepartureTime?.ToString(@"hh\:mm\:ss");

        // ✅ YENİ - Original ETA'lar (planlanan zamanlar - analiz için)
        OriginalEstimatedArrivalTime = journeyStop.OriginalEstimatedArrivalTime != TimeSpan.Zero
            ? journeyStop.OriginalEstimatedArrivalTime.ToString(@"hh\:mm\:ss")
            : null;
        OriginalEstimatedDepartureTime = journeyStop.OriginalEstimatedDepartureTime != TimeSpan.Zero
            ? journeyStop.OriginalEstimatedDepartureTime.ToString(@"hh\:mm\:ss")
            : null;

        ArriveBetweenStart = journeyStop.ArriveBetweenStart?.ToString(@"hh\:mm\:ss");
        ArriveBetweenEnd = journeyStop.ArriveBetweenEnd?.ToString(@"hh\:mm\:ss");

        // ✅ ÖNEMLİ: EKSİK OLAN FIELD'LAR EKLENDİ
        CheckInTime = journeyStop.CheckInTime?.ToString("yyyy-MM-dd'T'HH:mm:ss.fff");
        CheckOutTime = journeyStop.CheckOutTime?.ToString("yyyy-MM-dd'T'HH:mm:ss.fff");

        // ✅ YENİ: Gecikme bilgileri
        DelayReasonCategory = journeyStop.DelayReasonCategory?.ToString();
        DelayReason = journeyStop.DelayReason;
        NewDelay = journeyStop.NewDelay;
        CumulativeDelay = journeyStop.CumulativeDelay;

        if (journeyStop.RouteStop != null)
        {
            RouteStop = new RouteStopResponse(journeyStop.RouteStop);
        }
    }
    
    public int Id { get; set; }
    public string? Name { get; set; }
    public int JourneyId { get; set; }
    public int StopId { get; set; }
    public int RouteStopId { get; set; }
    public int Order { get; set; }
    public string Status { get; set; }
    public double Distance { get; set; }
    public string StartAddress { get; set; }
    public double StartLatitude { get; set; }
    public double StartLongitude { get; set; }
    public string EndAddress { get; set; }
    public double EndLatitude { get; set; }
    public double EndLongitude { get; set; }
    public string EstimatedArrivalTime { get; set; }
    public string EstimatedDepartureTime { get; set; }

    // ✅ YENİ - Original ETA'lar (planlanan zamanlar - dispatcher analizi için)
    public string? OriginalEstimatedArrivalTime { get; set; }
    public string? OriginalEstimatedDepartureTime { get; set; }

    public string ArriveBetweenStart { get; set; }
    public string ArriveBetweenEnd { get; set; }
    public RouteStopResponse RouteStop { get; set; }

    // âœ… Ã–NEMLÄ°: EKSÄ°K OLAN PROPERTY'LER EKLENDÄ°
    public string? CheckInTime { get; set; }
    public string? CheckOutTime { get; set; }

    // ✅ YENİ: Gecikme tracking property'leri
    public string? DelayReasonCategory { get; set; }
    public string? DelayReason { get; set; }
    public int NewDelay { get; set; }
    public int CumulativeDelay { get; set; }
}

public class RouteStopResponse
{
    public bool IsExcluded { get; set; }
    public string? ExclusionReason { get; set; }
    public RouteStopResponse(RouteStop routeStop)
    {
        Id = routeStop.Id;
        Name = routeStop.Name;
        Address = routeStop.Address;
        Latitude = routeStop.Latitude;
        Longitude = routeStop.Longitude;
        Notes = routeStop.Notes;
        ContactFullName = routeStop.ContactFullName;
        ContactPhone = routeStop.ContactPhone;
        ContactEmail = routeStop.ContactEmail;
        Type = routeStop.Type.ToString();
        OrderType = routeStop.OrderType.ToString();
        Order = routeStop.Order;
        ProofOfDeliveryRequired = routeStop.ProofOfDeliveryRequired;
        SignatureRequired = routeStop.SignatureRequired;
        PhotoRequired = routeStop.PhotoRequired;
        ServiceTime = routeStop.ServiceTime?.ToString(@"hh\:mm\:ss");
        Status = "pending"; // Default status
        IsExcluded = routeStop.IsExcluded;
        ExclusionReason = routeStop.ExclusionReason;

        EstimatedArrivalTime = routeStop.EstimatedArrivalTime?.ToString(@"hh\:mm\:ss");
        EstimatedDepartureTime = routeStop.EstimatedDepartureTime?.ToString(@"hh\:mm\:ss");

        CustomerId = routeStop.CustomerId ?? 0;
        
        if (routeStop.Customer != null)
        {
            Customer = new CustomerResponse
            {
                Id = routeStop.Customer.Id,
                Code = routeStop.Customer.Code,
                Name = routeStop.Customer.Name,
                Address = routeStop.Customer.Address,
                Phone = routeStop.Customer.Phone ?? "",
                Email = routeStop.Customer.Email,
                Latitude = routeStop.Customer.Latitude,
                Longitude = routeStop.Customer.Longitude,
                CreatedAt = routeStop.Customer.CreatedAt,
                UpdatedAt = routeStop.Customer.UpdatedAt ?? routeStop.Customer.CreatedAt
            };
        }
        else
        {
            Customer = null;
        }
    }
    
    public int Id { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Notes { get; set; }
    public string ContactFullName { get; set; }
    public string ContactPhone { get; set; }
    public string ContactEmail { get; set; }
    public string Type { get; set; }
    public string OrderType { get; set; }
    public int Order { get; set; }
    public bool ProofOfDeliveryRequired { get; set; }
    public bool SignatureRequired { get; set; }
    public bool PhotoRequired { get; set; }
    public string ServiceTime { get; set; }
    public string Status { get; set; }

    // Frontend için gerekli
    public int CustomerId { get; set; }
    public CustomerResponse Customer { get; set; }
    public string EstimatedArrivalTime { get; set; }
    public string EstimatedDepartureTime { get; set; }
}

public class StartDetailsResponse
{
    public StartDetailsResponse(JourneyStartDetails details)
    {
        StartTime = details.StartTime.ToString(@"hh\:mm\:ss");
        Name = details.Name;
        Address = details.Address;
        Latitude = details.Latitude;
        Longitude = details.Longitude;
    }
    
    public StartDetailsResponse(RouteStartDetails details)
    {
        StartTime = details.StartTime.ToString(@"hh\:mm\:ss");
        Name = details.Name;
        Address = details.Address;
        Latitude = details.Latitude;
        Longitude = details.Longitude;
    }
    
    public string StartTime { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class EndDetailsResponse
{
    public EndDetailsResponse(JourneyEndDetails details)
    {
        Name = details.Name;
        Address = details.Address;
        Latitude = details.Latitude;
        Longitude = details.Longitude;
        EstimatedArrivalTime = null; // JourneyEndDetails'da EstimatedArrivalTime yok
    }

    public EndDetailsResponse(RouteEndDetails details)
    {
        Name = details.Name;
        Address = details.Address;
        Latitude = details.Latitude;
        Longitude = details.Longitude;
        EstimatedArrivalTime = details.EstimatedArrivalTime?.ToString(@"hh\:mm\:ss");
    }

    public string Name { get; set; }
    public string Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? EstimatedArrivalTime { get; set; }
}

public class RouteResponse
{
    public RouteResponse(Monolith.WebAPI.Data.Journeys.Route route)
    {
        Id = route.Id;
        Name = route.Name;
        Date = route.Date.ToString("yyyy-MM-dd");
        Status = route.Status;
        
        // ID'ler
        DriverId = route.DriverId?.ToString();
        VehicleId = route.VehicleId?.ToString();
        DepotId = route.DepotId.ToString();
        
        // Metrikler
        TotalDistance = route.TotalDistance ?? 0;
        TotalDuration = route.TotalDuration ?? 0;
        TotalDeliveries = route.TotalDeliveries;
        CompletedDeliveries = route.CompletedDeliveries;
        Optimized = route.Optimized;
        AvoidTolls = route.AvoidTolls;
        Notes = route.Notes;
        
        // Timestamps
        CreatedAt = route.CreatedAt;
        UpdatedAt = route.UpdatedAt ?? route.CreatedAt;
        StartedAt = route.StartedAt;
        CompletedAt = route.CompletedAt;
        
        // Details
        StartDetails = route.StartDetails != null ? new StartDetailsResponse(route.StartDetails) : null;
        EndDetails = route.EndDetails != null ? new EndDetailsResponse(route.EndDetails) : null;
        
        // Stops
        Stops = route.Stops?.OrderBy(x => x.Order).Select(x => new RouteStopResponse(x)).ToList();
        
        // Relations
        if (route.Driver != null)
        {
            Driver = new DriverResponse 
            { 
                Id = route.Driver.Id.ToString(),
                Name = route.Driver.Name,
                Phone = route.Driver.Phone,
                Email = route.Driver.Email,
                LicenseNumber = route.Driver.LicenseNumber,
                Status = route.Driver.Status,
                CreatedAt = route.Driver.CreatedAt
            };
        }
        
        if (route.Vehicle != null)
        {
            Vehicle = new VehicleResponse
            {
                Id = route.Vehicle.Id,
                PlateNumber = route.Vehicle.PlateNumber,
                Type = route.Vehicle.Type,
                Brand = route.Vehicle.Brand,
                Model = route.Vehicle.Model,
                Year = route.Vehicle.Year,
                Capacity = route.Vehicle.Capacity,
                Status = route.Vehicle.Status,
                FuelType = route.Vehicle.FuelType,
                CurrentKm = route.Vehicle.CurrentKm, // ✅ YENİ
                CreatedAt = route.Vehicle.CreatedAt,
                UpdatedAt = route.Vehicle.UpdatedAt
            };

            // ✅ Route response'a da vehicle'ın güncel kilometresini ekle
            CurrentKm = route.Vehicle.CurrentKm;
        }
        
        if (route.Depot != null)
        {
            Depot = new DepotResponse(route.Depot);
        }
    }
    
    public int Id { get; set; }
    public string Name { get; set; }
    public string Date { get; set; }
    public string Status { get; set; }
    public string DriverId { get; set; }
    public string VehicleId { get; set; }
    public string DepotId { get; set; }
    public int? CurrentKm { get; set; } // ✅ Vehicle'ın güncel kilometresi
    public double TotalDistance { get; set; }
    public int TotalDuration { get; set; }
    public int TotalDeliveries { get; set; }
    public int CompletedDeliveries { get; set; }
    public bool Optimized { get; set; }
    public bool AvoidTolls { get; set; }
    public string Notes { get; set; }
    
    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Details
    public StartDetailsResponse StartDetails { get; set; }
    public EndDetailsResponse EndDetails { get; set; }
    public List<RouteStopResponse> Stops { get; set; }
    
    // Relations
    public DriverResponse Driver { get; set; }
    public VehicleResponse Vehicle { get; set; }
    public DepotResponse Depot { get; set; }
    
    // âœ… YENÄ° EKLENEN PROPERTY'LER - BUILD HATASINI Ã‡Ã–ZMEK Ä°Ã‡Ä°N
    public int? JourneyId { get; set; }
    public string JourneyStatus { get; set; }
}

// âœ… GÃœNCELLENMÄ°Åž JourneyStatusResponse - YENÄ° FIELD'LAR EKLENDÄ°
public class JourneyStatusResponse
{
    public JourneyStatusResponse(JourneyStatus status)
    {
        Id = status.Id;
        StopId = status.StopId; // ✅ EKSIK OLAN FIELD EKLENDI
        Status = status.Status.ToString();
        Notes = status.Notes;
        CreatedAt = status.CreatedAt;
        
        // âœ… YENÄ° EKLENEN FIELD'LAR
        FailureReason = status.FailureReason;
        SignatureBase64 = status.SignatureBase64;
        PhotoBase64 = status.PhotoBase64;
        SignatureUrl = status.SignatureUrl;
        PhotoUrl = status.PhotoUrl;
        Latitude = status.Latitude;
        Longitude = status.Longitude;
    }
    
    public int Id { get; set; }
    public int StopId { get; set; } // ✅ EKSIK OLAN FIELD EKLENDI
    public string? Name { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // âœ… YENÄ° EKLENEN FIELD'LAR
    public string FailureReason { get; set; }
    public string SignatureBase64 { get; set; }
    public string PhotoBase64 { get; set; }
    public string SignatureUrl { get; set; }
    public string PhotoUrl { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
