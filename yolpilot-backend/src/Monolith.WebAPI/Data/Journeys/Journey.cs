using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using Monolith.WebAPI.Applications.Commands.Journeys;
using Monolith.WebAPI.Data.Journeys.Events;
using Monolith.WebAPI.Data.Members;
using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.External.Google.Models;
using Monolith.WebAPI.External.RouteXL.Models;
using Monolith.WebAPI.Infrastructure;

namespace Monolith.WebAPI.Data.Journeys;

public partial class Journey : BaseEntity
{
    // do not remove this constructor
    public Journey()
    {
    }

    public string? Name { get; set; } // ✅ YENİ EKLENEN
    [MaxLength(int.MaxValue)] public string Polyline { get; set; }
    public DateTime Date { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? FinishedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public DateTime? ArchivedAt { get; set; } // Arşivlenme tarihi

    // ✅ YENİ - Kilometre takibi
    public int? StartKm { get; set; } // Sefer başlangıç kilometresi
    public int? EndKm { get; set; } // Sefer bitiş kilometresi

    // ✅ YENİ - Yakıt ve araç durumu takibi
    public string? StartFuel { get; set; } // Sefer başlangıç yakıt seviyesi (full, three_quarters, half, quarter, empty)
    public string? EndFuel { get; set; } // Sefer bitiş yakıt seviyesi
    public string? VehicleCondition { get; set; } // Sefer bitişindeki araç durumu (good, needs_cleaning, needs_maintenance, damaged)

    // ✅ YENİ - Optimizasyon bayrağı (yeni durak eklenince true olur)
    public bool NeedsReoptimization { get; set; }

    // ✅ V38 - YENİ EKLENEN LiveLocation
    public LiveLocation LiveLocation { get; set; }

    // ✅ V38 - CurrentStopIndex artık veritabanında saklanacak
    public int CurrentStopIndex { get; set; }
    
    // ✅ V39 FIX - WorkspaceId NULLABLE YAPILDI
    public int? WorkspaceId { get; set; }
    public Workspace.Workspace Workspace { get; set; }
     
    public int RouteId { get; private set; }
    public Route Route { get; set; }

    public int DriverId { get; private set; }
    public Driver Driver { get; set; }
    
    // ✅ V38 - Vehicle relationship eklendi
    public int? VehicleId { get; set; }
    public Vehicle Vehicle { get; set; }

    public ICollection<JourneyStatus> Statuses { get; set; }
    public ICollection<JourneyStop> Stops { get; set; }

    public JourneyStartDetails StartDetails { get; set; }
    public JourneyEndDetails EndDetails { get; set; }

    // Status property'si - V38
    public JourneyStatusEnum Status { get; set; }

    public Journey(AssignRouteCommand command, Route route)
    {
        RouteId = command.RouteId;
        DriverId = command.DriverId;
        Date = DateTime.UtcNow.Date; // ✅ DÜZELTME: Route.Date yerine bugünün tarihini kullan
        CurrentStopIndex = 0; // Başlangıçta 0
        Status = JourneyStatusEnum.Planned; // Başlangıç durumu
        
        Name = !string.IsNullOrEmpty(command.Name) ? command.Name : route.Name;
        
        // ✅ V39 FIX - Route üzerinden WorkspaceId'yi al
        WorkspaceId = route.WorkspaceId;
        VehicleId = route.VehicleId;

        Polyline = route.Polyline;
        
        StartDetails = new JourneyStartDetails
        {
            StartTime = route.StartDetails.StartTime,
            Name = route.StartDetails.Name,
            Address = route.StartDetails.Address,
            Latitude = route.StartDetails.Latitude,
            Longitude = route.StartDetails.Longitude
        };

        EndDetails = new JourneyEndDetails
        {
            Name = route.EndDetails.Name,
            Address = route.EndDetails.Address,
            Latitude = route.EndDetails.Latitude,
            Longitude = route.EndDetails.Longitude
        };
    }

    public void Start()
    {
        Console.WriteLine($"[CRITICAL] Journey.Start() called for Journey #{Id} - StackTrace: {Environment.StackTrace}");
        StartedAt = DateTime.UtcNow;
        Status = JourneyStatusEnum.InProgress;

        // ✅ Launch delay hesapla ve uygula (güvenli kontroller ile)
        if (StartDetails != null && Stops != null && Stops.Any())
        {
            try
            {
                var plannedStartDateTime = Date.Date.Add(StartDetails.StartTime);
                var actualStartDateTime = StartedAt.Value;
                var launchDelay = actualStartDateTime - plannedStartDateTime;

                Console.WriteLine($"[LAUNCH_DELAY] Journey #{Id} - Planned: {plannedStartDateTime}, Actual: {actualStartDateTime}, Delay: {launchDelay.TotalMinutes:F1} min");

                // Önce tüm pending stoplara Original ETA'ları set et
                foreach (var stop in Stops.Where(s => s.Status == JourneyStopStatus.Pending))
                {
                    // Original ETA'ları sakla (ilk kez başlatıldığında veya sıfırsa)
                    if (stop.OriginalEstimatedArrivalTime == TimeSpan.Zero)
                    {
                        stop.OriginalEstimatedArrivalTime = stop.EstimatedArrivalTime;
                        stop.OriginalEstimatedDepartureTime = stop.EstimatedDepartureTime ?? TimeSpan.Zero;
                        Console.WriteLine($"[LAUNCH_DELAY] Set original ETAs for stop {stop.Id}: Arrival={stop.OriginalEstimatedArrivalTime}, Departure={stop.OriginalEstimatedDepartureTime}");
                    }
                }

                // Eğer gecikme varsa, Current ETA'ları güncelle
                if (launchDelay.TotalMinutes > 1) // 1 dakikadan fazla gecikme
                {
                    Console.WriteLine($"[LAUNCH_DELAY] Applying {launchDelay.TotalMinutes:F1} minute delay to all pending stops.");

                    foreach (var stop in Stops.Where(s => s.Status == JourneyStopStatus.Pending))
                    {
                        stop.EstimatedArrivalTime += launchDelay;
                        if (stop.EstimatedDepartureTime.HasValue)
                        {
                            stop.EstimatedDepartureTime = stop.EstimatedDepartureTime.Value + launchDelay;
                        }
                        stop.UpdatedAt = DateTime.UtcNow;
                    }

                    Console.WriteLine($"[LAUNCH_DELAY] Applied delay to {Stops.Count(s => s.Status == JourneyStopStatus.Pending)} pending stops.");
                }
                else
                {
                    Console.WriteLine($"[LAUNCH_DELAY] Journey #{Id} started on time (delay: {launchDelay.TotalMinutes:F1} min). No ETA adjustment needed.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LAUNCH_DELAY] Error applying launch delay for Journey #{Id}: {ex.Message}");
                Console.WriteLine($"[LAUNCH_DELAY] Stack trace: {ex.StackTrace}");
                // Hata olsa bile journey başlatılmaya devam etsin
            }
        }
        else
        {
            Console.WriteLine($"[LAUNCH_DELAY] Journey #{Id} - Skipping launch delay (StartDetails or Stops is null)");
        }

        AddDomainEvent(new JourneyStartedEvent(Id, StartedAt));
    }

    public void Finish()
    {
        FinishedAt = DateTime.UtcNow;
        CompletedAt = DateTime.UtcNow;
        Status = JourneyStatusEnum.Completed;
        AddDomainEvent(new JourneyFinishedEvent(Id, FinishedAt));
    }
    
    public void Cancel()
    {
        FinishedAt = DateTime.UtcNow;
        CancelledAt = DateTime.UtcNow;
        Status = JourneyStatusEnum.Cancelled;
        AddDomainEvent(new JourneyCancelledEvent(Id, CancelledAt));
    }
    
    // Status kontrolü için yardımcı metodlar
    public bool IsCancelled => CancelledAt.HasValue;
    public bool IsCompleted => CompletedAt.HasValue && !CancelledAt.HasValue;
    public bool IsActive => StartedAt.HasValue && !FinishedAt.HasValue;
    public bool StopsCountedForBilling { get; private set; }
    
    public void MarkStopsAsCounted()
    {
        StopsCountedForBilling = true;
        UpdatedAt = DateTime.UtcNow;
    }

    // ✅ CurrentStopIndex güncelleme metodu
    public void UpdateCurrentStopIndex(int newIndex)
    
    {
        if (newIndex >= 0 && (Stops == null || newIndex < Stops.Count))
        {
            CurrentStopIndex = newIndex;
            UpdatedAt = DateTime.UtcNow;
        }
    }
    
    
    // ✅ Mevcut stop'u getir
    public JourneyStop GetCurrentStop()
    {
        if (Stops == null || !Stops.Any())
            return null;
            
        return Stops.OrderBy(s => s.Order).ElementAtOrDefault(CurrentStopIndex);
    }
}

public partial class Journey
{
    private void InitStops()
    {
        if (StartedAt.HasValue || FinishedAt.HasValue)
            throw new ApiException("Cannot change the journey after it has started", 400);

        if (Stops?.Count > 0)
            Stops.Clear();

        Stops = new List<JourneyStop>();
    }

    public void CreateStops(RouteXlResponse response, string polyline)
    {
        InitStops();

        RouteStop prevRouteStop = null;
        var prevR = response.Route.First();
        foreach (var r in response.Route.Skip(1))
        {
            RouteStop routeStop = null;
            try
            {
                routeStop = Route.Stops.FirstOrDefault(x => x.Id == int.Parse(r.Value.Name));
            }
            catch (Exception)
            {
                // ignored
            }

            var estimatedDepartureTime = StartDetails.StartTime + TimeSpan.FromMinutes(r.Value.Arrival);
            estimatedDepartureTime +=
                routeStop?.ServiceTime is not null ? routeStop!.ServiceTime!.Value : Route.Workspace.DefaultServiceTime;

            Stops.Add(new JourneyStop(
                journeyId: Id,
                stopId: routeStop?.Id ?? prevRouteStop?.Id ?? throw new ApiException("Route stop not found", 404),
                order: int.Parse(r.Key),
                distance: r.Value.Distance - prevR.Value.Distance,
                startAddress: prevRouteStop?.Address ?? StartDetails.Address,
                startLatitude: prevRouteStop?.Latitude ?? StartDetails.Latitude,
                startLongitude: prevRouteStop?.Longitude ?? StartDetails.Longitude,
                endAddress: routeStop?.Address ?? EndDetails.Address,
                endLatitude: routeStop?.Latitude ?? EndDetails.Latitude,
                endLongitude: routeStop?.Longitude ?? EndDetails.Longitude,
                estimatedArrivalTime: StartDetails.StartTime + TimeSpan.FromMinutes(r.Value.Arrival),
                estimatedDepartureTime: estimatedDepartureTime,
                arriveBetweenStart: routeStop?.ArriveBetweenStart,
                arriveBetweenEnd: routeStop?.ArriveBetweenEnd
            ));

            prevRouteStop = routeStop;
            prevR = r;
        }

        Stops.Last().EstimatedDepartureTime = null;

        Polyline = polyline;
        UpdatedAt = DateTime.UtcNow;
    }

    // YENİ MODEL İLE ÇALIŞACAK ŞEKİLDE GÜNCELLENDİ
    public void CreateStops(DirectionsResponse response, IReadOnlyList<int> waypointIds)
    {
        InitStops();

        if (response?.Routes == null || response.Routes.Count == 0)
            return;

        var route = response.Routes.FirstOrDefault();
        if (route?.Legs == null)
            return;

        // waypoint_order'ı route'dan al veya response'dan al
        var waypointOrder = route.WaypointOrder ?? response.WaypointOrder ?? new List<int>();

        Stops = GetJourneyStops();
        if (Stops.Count > 0)
        {
            Stops.Last().EstimatedDepartureTime = null;
        }

        Polyline = route.OverviewPolyline?.Points;
        UpdatedAt = DateTime.UtcNow;
        return;

        List<JourneyStop> GetJourneyStops()
        {
            var result = new List<JourneyStop>();

            var index = 0;
            JourneyStop previousStop = null;
            RouteStop prevRouteStop = null;
            foreach (var leg in route.Legs)
            {
                // Son leg değilse waypoint_order'dan ID'yi al
                var routeStopId = index != route.Legs.Count - 1 && waypointOrder.Count > index 
                    ? waypointIds[waypointOrder[index]] 
                    : 0;
                    
                var routeStop = Route.Stops.FirstOrDefault(x => x.Id == routeStopId);

                var estimatedDepartureTime = previousStop?.EstimatedDepartureTime ?? StartDetails.StartTime;
                
                // JourneyStop constructor'ını kullan
                var journeyStop = new JourneyStop(
                    journeyId: Id,
                    routeStop: routeStop ?? prevRouteStop,
                    order: index + 1,
                    leg: leg,
                    prevEstimatedDepartureTime: estimatedDepartureTime,
                    defaultServiceTime: Route.Workspace.DefaultServiceTime
                );
                result.Add(journeyStop);

                previousStop = journeyStop;
                prevRouteStop = routeStop;
                index++;
            }

            return result;
        }
    }

    // ESKİ MODEL İLE UYUMLULUK İÇİN (GEÇİCİ - İLERİDE SİLİNEBİLİR)
    public void CreateStops(GoogleDirectionsResponse response, IReadOnlyList<int> waypointIds)
    {
        InitStops();

        if (response?.routes == null || response.routes.Count == 0)
            return;

        var route = response.routes.FirstOrDefault();
        if (route?.legs == null)
            return;

        Stops = GetJourneyStops();
        if (Stops.Count > 0)
        {
            Stops.Last().EstimatedDepartureTime = null;
        }

        Polyline = route.overview_polyline?.points;
        UpdatedAt = DateTime.UtcNow;
        return;

        List<JourneyStop> GetJourneyStops()
        {
            var result = new List<JourneyStop>();

            var index = 0;
            JourneyStop previousStop = null;
            RouteStop prevRouteStop = null;
            foreach (var oldLeg in route.legs)
            {
                // Eski modeli yeni modele dönüştür
                var leg = new Leg
                {
                    Distance = oldLeg.distance != null ? new Distance 
                    { 
                        Value = oldLeg.distance.value, 
                        Text = oldLeg.distance.text 
                    } : null,
                    Duration = oldLeg.duration != null ? new Duration 
                    { 
                        Value = oldLeg.duration.value, 
                        Text = oldLeg.duration.text 
                    } : null,
                    StartAddress = oldLeg.start_address,
                    EndAddress = oldLeg.end_address,
                    StartLocation = oldLeg.start_location != null ? new Location
                    {
                        Lat = oldLeg.start_location.lat,
                        Lng = oldLeg.start_location.lng
                    } : null,
                    EndLocation = oldLeg.end_location != null ? new Location
                    {
                        Lat = oldLeg.end_location.lat,
                        Lng = oldLeg.end_location.lng
                    } : null
                };

                var routeStopId = index != route.legs.Count - 1 && route.waypoint_order != null && route.waypoint_order.Count > index
                    ? waypointIds[route.waypoint_order[index]] 
                    : 0;
                    
                var routeStop = Route.Stops.FirstOrDefault(x => x.Id == routeStopId);

                var estimatedDepartureTime = previousStop?.EstimatedDepartureTime ?? StartDetails.StartTime;
                var journeyStop = new JourneyStop(
                    journeyId: Id,
                    routeStop: routeStop ?? prevRouteStop,
                    order: index + 1,
                    leg: leg,
                    prevEstimatedDepartureTime: estimatedDepartureTime,
                    defaultServiceTime: Route.Workspace.DefaultServiceTime
                );
                result.Add(journeyStop);

                previousStop = journeyStop;
                prevRouteStop = routeStop;
                index++;
            }

            return result;
        }
    }

    public List<RouteXlRequestLocation> GetRouteXlParams()
    {
        var locations = new List<RouteXlRequestLocation>
        {
            new()
            {
                address = "start",
                lat = StartDetails.Latitude.ToString(CultureInfo.InvariantCulture),
                lng = StartDetails.Longitude.ToString(CultureInfo.InvariantCulture)
            }
        };

        locations.AddRange(Route.Stops.Select(x => new RouteXlRequestLocation
        {
            address = x.Id.ToString(),
            lat = x.Latitude.ToString(CultureInfo.InvariantCulture),
            lng = x.Longitude.ToString(CultureInfo.InvariantCulture),
            servicetime = x.ServiceTime.HasValue
                ? (int) x.ServiceTime.Value.TotalMinutes
                : (int) Route.Workspace.DefaultServiceTime.TotalMinutes,
            restrictions = new RouteXlRequestLocationRestrictions
            {
                ready = x.ArriveBetweenStart.HasValue ? (int) (x.ArriveBetweenStart.Value - StartDetails.StartTime).TotalMinutes : null,
                due = x.ArriveBetweenEnd.HasValue ? (int) (x.ArriveBetweenEnd.Value - StartDetails.StartTime).TotalMinutes : null,
                before = null,
                after = null,
            }
        }));

        locations.Add(new RouteXlRequestLocation
        {
            address = "end",
            lat = EndDetails.Latitude.ToString(CultureInfo.InvariantCulture),
            lng = EndDetails.Longitude.ToString(CultureInfo.InvariantCulture)
        });

        return locations;
    }

    public (string origin, string destination, Dictionary<int, string> waypoints) GetGoogleParams()
    {
        var waypoints = Route.Stops.ToDictionary(x => x.Id, x => x.LatLng);
        return (StartDetails.LatLng, EndDetails.LatLng, waypoints);
    }
}

// ✅ V38 - YENİ LiveLocation CLASS'I
public class LiveLocation
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime Timestamp { get; set; }
    public double? Speed { get; set; }
    public double? Heading { get; set; }
    public double? Accuracy { get; set; }
}

// JourneyStatusEnum (Entity'den farklı, Journey'nin durumu için)
public enum JourneyStatusEnum
{
    Planned = 100,
    InProgress = 200,
    Completed = 300,
    Cancelled = 400,
    OnHold = 500
}