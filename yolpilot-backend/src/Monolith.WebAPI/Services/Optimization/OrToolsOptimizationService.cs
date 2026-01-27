using Google.OrTools.ConstraintSolver;
using System.Globalization;
using Monolith.WebAPI.External.Google;

namespace Monolith.WebAPI.Services.Optimization
{
    public class OrToolsOptimizationService : IOptimizationService
    {
        private const int GoogleDirectionsMaxWaypoints = 25;
        private readonly ILogger<OrToolsOptimizationService> _logger;
        private readonly GoogleApiService _googleApiService;
        
        public OrToolsOptimizationService(
            ILogger<OrToolsOptimizationService> logger,
            GoogleApiService googleApiService)
        {
            _logger = logger;
            _googleApiService = googleApiService;
        }
        
        public async Task<OptimizationResultWithExclusions> OptimizeWithExclusions(
            int depotId,
            double depotLatitude,
            double depotLongitude,
            List<OptimizationStop> stops,
            TimeSpan routeStartTime,
            TimeSpan? maxRouteDuration = null)
        {
            try
            {
                _logger.LogInformation("Starting optimization with exclusion check for {Count} stops, start time: {StartTime}",
                    stops.Count, routeStartTime);

                _logger.LogInformation("STEP 1: Trying optimization WITHOUT exclusions (all stops mandatory)");
                var fullResult = await OptimizeRouteWithTimeWindows(
                    depotId, depotLatitude, depotLongitude, stops, routeStartTime, maxRouteDuration, allowExclusions: false);

                if (fullResult.Success)
                {
                    _logger.LogInformation("SUCCESS - All stops included");
                    return new OptimizationResultWithExclusions
                    {
                        Success = true,
                        OptimizedOrder = fullResult.OptimizedOrder,
                        TotalDistance = fullResult.TotalDistance,
                        TotalDuration = fullResult.TotalDuration,
                        ExcludedStops = new List<ExcludedStop>()
                    };
                }

                _logger.LogWarning("STEP 2: Retrying optimization WITH exclusions");
                var excludedResult = await OptimizeRouteWithTimeWindows(
                    depotId, depotLatitude, depotLongitude, stops, routeStartTime, maxRouteDuration, allowExclusions: true);

                if (excludedResult.Success)
                {
                    var excludedStops = BuildExcludedStops(stops, excludedResult.OptimizedOrder);
                    _logger.LogInformation("Optimization completed with {IncludedCount} stops, excluded {ExcludedCount}",
                        excludedResult.OptimizedOrder.Count, excludedStops.Count);

                    return new OptimizationResultWithExclusions
                    {
                        Success = true,
                        OptimizedOrder = excludedResult.OptimizedOrder,
                        TotalDistance = excludedResult.TotalDistance,
                        TotalDuration = excludedResult.TotalDuration,
                        ExcludedStops = excludedStops,
                        Message = excludedStops.Count > 0
                            ? $"{excludedStops.Count} durak rota disi birakildi"
                            : string.Empty
                    };
                }

                _logger.LogError("No viable solution found, all stops would need to be excluded");
                var failureMessage = string.IsNullOrWhiteSpace(excludedResult.Message)
                    ? BuildNoSolutionMessage(stops, routeStartTime)
                    : excludedResult.Message;

                return new OptimizationResultWithExclusions
                {
                    Success = false,
                    Message = failureMessage,
                    ExcludedStops = stops.Select(s => new ExcludedStop
                    {
                        Stop = s,
                        Reason = "Cozum bulunamadi",
                        TimeWindowConflict = s.TimeWindowStart.HasValue && s.TimeWindowEnd.HasValue
                            ? FormatTimeWindow(s)
                            : "N/A"
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Optimization with exclusions failed");
                return new OptimizationResultWithExclusions
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        // NEW METHOD: Smart time window reordering - test tight time windows at different positions
        private async Task<OptimizationResultWithExclusions> TryEarlyTimeWindowReordering(
            int depotId,
            double depotLatitude,
            double depotLongitude,
            List<OptimizationStop> stops,
            TimeSpan routeStartTime,
            TimeSpan? maxRouteDuration = null)
        {
            _logger.LogInformation("Attempting SMART time window reordering strategy (position-based testing)");

            // STEP 1: Cache Google Directions data for performance
            _logger.LogInformation("Pre-fetching Google Directions data for reordering attempts");
            var cachedDirectionsResponse = await GetDirectionsForAllStops(depotLatitude, depotLongitude, stops);

            if (cachedDirectionsResponse == null)
            {
                _logger.LogWarning("Failed to fetch Google Directions data");
                return new OptimizationResultWithExclusions { Success = false };
            }

            // STEP 2: Identify tight time window stops (likely to cause problems)
            // Focus on stops with narrow windows (< 4 hours) or very early deadlines
            var problematicStops = stops
                .Where(s => s.TimeWindowStart.HasValue && s.TimeWindowEnd.HasValue)
                .Where(s =>
                {
                    var windowDuration = s.TimeWindowEnd.Value - s.TimeWindowStart.Value;
                    var isTightWindow = windowDuration.TotalHours < 4; // Narrow window
                    var isEarlyDeadline = s.TimeWindowEnd.Value < routeStartTime.Add(TimeSpan.FromHours(3)); // Must arrive within 3 hours
                    return isTightWindow || isEarlyDeadline;
                })
                .OrderBy(s => s.TimeWindowEnd.Value) // Prioritize earliest deadlines
                .ToList();

            if (problematicStops.Count == 0)
            {
                _logger.LogInformation("No tight time windows found - attempting normal optimization");
                var normalResult = await OptimizeRouteWithTimeWindows(
                    depotId, depotLatitude, depotLongitude, stops, routeStartTime, maxRouteDuration,
                    allowExclusions: false, cachedDirectionsResponse: cachedDirectionsResponse);

                if (normalResult.Success)
                {
                    return new OptimizationResultWithExclusions
                    {
                        Success = true,
                        OptimizedOrder = normalResult.OptimizedOrder,
                        TotalDistance = normalResult.TotalDistance,
                        TotalDuration = normalResult.TotalDuration,
                        ExcludedStops = new List<ExcludedStop>(),
                        Message = "Normal optimizasyon başarılı"
                    };
                }

                return new OptimizationResultWithExclusions { Success = false };
            }

            _logger.LogInformation("Found {Count} problematic time window stops to test at different positions", problematicStops.Count);

            // Log details
            foreach (var stop in problematicStops)
            {
                var windowDuration = stop.TimeWindowEnd.Value - stop.TimeWindowStart.Value;
                _logger.LogInformation("Problematic stop: {Name} - Window: {Start}-{End} (Duration: {Duration} min)",
                    stop.Name,
                    stop.TimeWindowStart.Value.ToString(@"hh\:mm"),
                    stop.TimeWindowEnd.Value.ToString(@"hh\:mm"),
                    windowDuration.TotalMinutes);
            }

            // STEP 3: Try each problematic stop at different positions (0, 1, 2)
            foreach (var problematicStop in problematicStops)
            {
                _logger.LogInformation("Testing positions for: {Name} (Window: {Start}-{End})",
                    problematicStop.Name,
                    problematicStop.TimeWindowStart?.ToString(@"hh\:mm") ?? "N/A",
                    problematicStop.TimeWindowEnd?.ToString(@"hh\:mm") ?? "N/A");

                // Try first 3 positions (0=first, 1=second, 2=third)
                int maxPositions = Math.Min(3, stops.Count);

                for (int targetPosition = 0; targetPosition < maxPositions; targetPosition++)
                {
                    // Create reordered stops array with problematic stop at target position
                    var reorderedStops = stops.Where(s => s.Id != problematicStop.Id).ToList();
                    reorderedStops.Insert(targetPosition, problematicStop);

                    _logger.LogInformation("Testing {Name} FIXED at position {Position} (out of first {Max})",
                        problematicStop.Name, targetPosition + 1, maxPositions);

                    // IMPORTANT: Use fixedStopId and fixedPosition to FORCE the stop at this position
                    // OR-Tools will optimize all other stops while keeping this one fixed
                    var testResult = await OptimizeRouteWithTimeWindows(
                        depotId, depotLatitude, depotLongitude, reorderedStops, routeStartTime, maxRouteDuration,
                        allowExclusions: false,
                        cachedDirectionsResponse: cachedDirectionsResponse,
                        fixedStopId: problematicStop.Id,  // Fix this stop
                        fixedPosition: targetPosition);  // At this position

                    if (testResult.Success)
                    {
                        // Map back to original indices
                        var mappedOrder = new List<int>();
                        foreach (var optimizedIndex in testResult.OptimizedOrder)
                        {
                            var testStop = reorderedStops[optimizedIndex];
                            var originalIndex = stops.FindIndex(s => s.Id == testStop.Id);
                            if (originalIndex >= 0)
                                mappedOrder.Add(originalIndex);
                        }

                        _logger.LogInformation("✅ SUCCESS: {Name} FIXED at position {Position}, other stops optimized",
                            problematicStop.Name, targetPosition + 1);

                        return new OptimizationResultWithExclusions
                        {
                            Success = true,
                            OptimizedOrder = mappedOrder,
                            TotalDistance = testResult.TotalDistance,
                            TotalDuration = testResult.TotalDuration,
                            ExcludedStops = new List<ExcludedStop>(),
                            Message = $"Rota optimize edildi: {problematicStop.Name} {targetPosition + 1}. sıraya sabitlendi"
                        };
                    }
                    else
                    {
                        _logger.LogInformation("❌ FAILED: Position {Position} didn't work for {Name}",
                            targetPosition + 1, problematicStop.Name);
                    }
                }
            }

            _logger.LogInformation("All position tests failed for problematic stops");
            return new OptimizationResultWithExclusions { Success = false };
        }

        // Helper method: Calculate arrival times for each stop based on optimized route order
        private Dictionary<int, TimeSpan> CalculateArrivalTimes(
            List<OptimizationStop> stops,
            List<int> optimizedOrder,
            TimeSpan routeStartTime,
            long[,] timeMatrix)
        {
            var arrivalTimes = new Dictionary<int, TimeSpan>();
            var currentTime = routeStartTime;
            int previousNode = 0; // Start from depot (node 0)

            foreach (var stopIndex in optimizedOrder)
            {
                var stop = stops[stopIndex];
                var currentNode = stopIndex + 1; // Nodes are 1-indexed (0 is depot)

                // Add travel time from previous location
                var travelTimeSeconds = timeMatrix[previousNode, currentNode];
                currentTime = currentTime.Add(TimeSpan.FromSeconds(travelTimeSeconds));

                // Record arrival time at this stop
                arrivalTimes[stop.Id] = currentTime;

                _logger.LogDebug("Stop {Name}: Arrival {Arrival}", stop.Name, currentTime.ToString(@"hh\:mm"));

                // Add service time before moving to next stop
                currentTime = currentTime.Add(TimeSpan.FromMinutes(stop.ServiceTimeMinutes));

                previousNode = currentNode;
            }

            return arrivalTimes;
        }

        public async Task<OptimizationResult> OptimizeRouteWithTimeWindows(
            int depotId,
            double depotLatitude,
            double depotLongitude,
            List<OptimizationStop> stops,
            TimeSpan routeStartTime, // YENİ PARAMETRE
            TimeSpan? maxRouteDuration = null,
            bool allowExclusions = false, // YENİ PARAMETRE - exclusion izni
            External.Google.Models.DirectionsResponse cachedDirectionsResponse = null, // PERFORMANCE: Cached Google API response
            int? fixedStopId = null, // REORDERING: Stop to fix at specific position
            int? fixedPosition = null) // REORDERING: Position to fix the stop at (0-indexed)
        {
            try
            {
                if (fixedStopId.HasValue && fixedPosition.HasValue)
                {
                    _logger.LogInformation("OR-Tools optimization starting for {Count} stops with FIXED stop ID {StopId} at position {Position}",
                        stops.Count, fixedStopId.Value, fixedPosition.Value);
                }
                else
                {
                    _logger.LogInformation("OR-Tools optimization starting for {Count} stops from depot at ({Lat},{Lng}) with start time {StartTime}",
                        stops.Count, depotLatitude, depotLongitude, routeStartTime);
                }

                // 1) Google Directions API ile gerçek süreleri al (waypoints ile) - veya cache kullan
                External.Google.Models.DirectionsResponse directionsResponse;

                if (cachedDirectionsResponse != null)
                {
                    _logger.LogInformation("✅ Using CACHED Google Directions data (performance optimization - no redundant API call)");
                    directionsResponse = cachedDirectionsResponse;
                }
                else
                {
                    _logger.LogInformation("Fetching real travel times from Google Directions API...");
                    directionsResponse = await GetDirectionsForAllStops(depotLatitude, depotLongitude, stops);
                }

                // 2) Mesafe ve süre matrisleri (Directions API'den alınan gerçek verilerle)
                var distanceMatrix = BuildDistanceMatrixFromDirections(directionsResponse, depotLatitude, depotLongitude, stops);
                var timeMatrix = BuildTimeMatrixFromDirections(directionsResponse, depotLatitude, depotLongitude, stops);

                // 3) Model
                int vehicleNumber = 1;
                int depot = 0; // 0: depo düğümü
                
                var manager = new RoutingIndexManager(stops.Count + 1, vehicleNumber, depot);
                var routing = new RoutingModel(manager);
                
                // 3) Mesafe callback (arc maliyeti)
                int transitCallbackIndex = routing.RegisterTransitCallback(
                    (long fromIndex, long toIndex) =>
                    {
                        var fromNode = manager.IndexToNode(fromIndex);
                        var toNode = manager.IndexToNode(toIndex);
                        return distanceMatrix[fromNode, toNode];
                    });
                routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);
                
                // 4) Zaman callback - sadece seyahat süresi (service time hariç time window için)
                int timeCallbackIndex = routing.RegisterTransitCallback(
                    (long fromIndex, long toIndex) =>
                    {
                        var fromNode = manager.IndexToNode(fromIndex);
                        var toNode = manager.IndexToNode(toIndex);

                        return timeMatrix[fromNode, toNode]; // Sadece seyahat süresi
                    });
                var maxRouteSeconds = (int)(maxRouteDuration?.TotalSeconds ?? 12 * 3600);
                var slackSeconds = maxRouteSeconds;

                routing.AddDimension(
                    timeCallbackIndex,
                    slackSeconds,
                    maxRouteSeconds,
                    true,
                    "Time");

                var timeDimension = routing.GetDimensionOrDie("Time");

                timeDimension.SetSlackCostCoefficientForAllVehicles(1);

                // 5) Zaman pencereleri - IMPROVED: Daha flexible hesaplama
                for (int i = 0; i < stops.Count; i++)
                {
                    var stop = stops[i];
                    var index = manager.NodeToIndex(i + 1); // 1..N (0 depo)

                    bool hasStart = stop.TimeWindowStart.HasValue;
                    bool hasEnd = stop.TimeWindowEnd.HasValue;

                    if (hasStart || hasEnd)
                    {
                        // Time window'ları absolute time olarak hesapla
                        // Time window VARIŞ zamanı için uygulanır (service time dahil değil)
                        // NOT: Math.Max kaldırıldı - time window route start'tan önce başlayabilir
                        long startSeconds = hasStart
                            ? (long)(stop.TimeWindowStart.Value - routeStartTime).TotalSeconds
                            : 0;

                        long endSeconds = hasEnd
                            ? (long)(stop.TimeWindowEnd.Value - routeStartTime).TotalSeconds
                            : 12 * 3600;

                        // STRICT CHECK: If time window is in past or invalid, mark as impossible
                        if (hasEnd && stop.TimeWindowEnd.Value < routeStartTime)
                        {
                            _logger.LogWarning("Time window end {End} is before route start {Start} for stop {Name} - marking as impossible",
                                stop.TimeWindowEnd.Value, routeStartTime, stop.Name);
                            // Set impossible time window (negative range)
                            startSeconds = -1000000; // Far in past
                            endSeconds = -999999;   // Still in past but after start
                        }
                        else if (endSeconds <= startSeconds)
                        {
                            _logger.LogWarning("Invalid time window for stop {Name}: end {End} <= start {Start} - marking as impossible",
                                stop.Name, endSeconds, startSeconds);
                            // Set impossible time window
                            startSeconds = -1000000;
                            endSeconds = -999999;
                        }

                        // FIXED: Removed automatic time window expansion - strict enforcement

                        timeDimension.CumulVar(index).SetRange(startSeconds, endSeconds);
                        _logger.LogInformation("Time window set for stop {Name}: {StartSec}s - {EndSec}s (window: {WindowStart}-{WindowEnd})",
                            stop.Name, startSeconds, endSeconds,
                            routeStartTime.Add(TimeSpan.FromSeconds(startSeconds)).ToString(@"hh\:mm"),
                            routeStartTime.Add(TimeSpan.FromSeconds(endSeconds)).ToString(@"hh\:mm"));
                    }
                }

                // Depo başlangıcı sabitlenmiş (0 = routeStartTime)
                // fix_start_cumul_to_zero = true olduğu için başlangıç 0'da sabit
                // Bitiş için max süre
                timeDimension.CumulVar(routing.End(0)).SetRange(0, maxRouteSeconds);

                // REMOVED: Soft penalty - Time window'lar artık HARD constraint olarak uygulanıyor (SetRange)
                // Eğer time window'a uyulamazsa, OR-Tools çözüm bulamaz ve durak exclude edilir

                // 5.4) Make stops with time windows OPTIONAL (disjunction) - ONLY if exclusions allowed
                if (allowExclusions)
                {
                    const int dropPenalty = 1000000;
                    _logger.LogInformation("Exclusions ALLOWED - marking stops as optional");
                    for (int i = 0; i < stops.Count; i++)
                    {
                        var stop = stops[i];
                        if (stop.OrderType == Data.Journeys.OrderType.First || stop.OrderType == Data.Journeys.OrderType.Last)
                        {
                            continue;
                        }

                        var index = manager.NodeToIndex(i + 1);
                        routing.AddDisjunction(new long[] { index }, dropPenalty);
                        _logger.LogInformation("Stop {Name} marked as optional", stop.Name);
                    }
                }
                else
                {
                    _logger.LogInformation("Exclusions NOT ALLOWED - all stops are mandatory");
                }

                // 5.5) Position constraints (First/Last stops)
                var firstStops = stops.Where(s => s.OrderType == Data.Journeys.OrderType.First).ToList();
                var lastStops = stops.Where(s => s.OrderType == Data.Journeys.OrderType.Last).ToList();

                if (firstStops.Any() || lastStops.Any())
                {
                    _logger.LogInformation("Adding position constraints: {FirstCount} first stops, {LastCount} last stops",
                        firstStops.Count, lastStops.Count);

                    foreach (var firstStop in firstStops)
                    {
                        var stopIndex = stops.FindIndex(s => s.Id == firstStop.Id);
                        if (stopIndex >= 0)
                        {
                            var firstIndex = manager.NodeToIndex(stopIndex + 1); // +1 çünkü 0 depo
                            _logger.LogInformation("Adding FIRST constraint for stop: {Name} (ID: {Id}, Node: {Node})", firstStop.Name, firstStop.Id, stopIndex + 1);

                            // Force this stop to be visited directly after the depot
                            var solver = routing.solver();
                            solver.Add(solver.MakeEquality(routing.NextVar(routing.Start(0)), firstIndex));
                        }
                    }

                    foreach (var lastStop in lastStops)
                    {
                        var stopIndex = stops.FindIndex(s => s.Id == lastStop.Id);
                        if (stopIndex >= 0)
                        {
                            var lastIndex = manager.NodeToIndex(stopIndex + 1); // +1 çünkü 0 depo
                            _logger.LogInformation("Adding LAST constraint for stop: {Name} (ID: {Id}, Node: {Node})", lastStop.Name, lastStop.Id, stopIndex + 1);

                            // Force this stop to go directly to the depot
                            var solver = routing.solver();
                            solver.Add(solver.MakeEquality(routing.NextVar(lastIndex), routing.End(0)));
                        }
                    }
                }

                // 5.6) Fixed position constraint - for reordering strategy
                if (fixedStopId.HasValue && fixedPosition.HasValue)
                {
                    var fixedStopIndex = stops.FindIndex(s => s.Id == fixedStopId.Value);
                    if (fixedStopIndex >= 0)
                    {
                        var fixedNodeIndex = manager.NodeToIndex(fixedStopIndex + 1); // +1 because 0 is depot
                        var solver = routing.solver();

                        _logger.LogInformation("Adding FIXED POSITION constraint: Stop {Name} (ID: {Id}) at position {Position}",
                            stops[fixedStopIndex].Name, fixedStopId.Value, fixedPosition.Value);

                        // Fix the stop at the specified position by constraining its predecessor and successor
                        if (fixedPosition.Value == 0)
                        {
                            // First position: Depot -> FixedStop
                            _logger.LogInformation("Fixing stop at FIRST position (after depot)");
                            solver.Add(solver.MakeEquality(routing.NextVar(routing.Start(0)), fixedNodeIndex));
                        }
                        else if (fixedPosition.Value == stops.Count - 1)
                        {
                            // Last position: FixedStop -> Depot
                            _logger.LogInformation("Fixing stop at LAST position (before depot)");
                            solver.Add(solver.MakeEquality(routing.NextVar(fixedNodeIndex), routing.End(0)));
                        }
                        else
                        {
                            // Middle position: PrevStop -> FixedStop -> NextStop
                            // We need to determine which stop should come before and after
                            // Based on the reordered stops array position
                            var prevStopInArray = stops[fixedPosition.Value - 1];
                            var nextStopInArray = stops[fixedPosition.Value + 1];

                            var prevNodeIndex = manager.NodeToIndex(stops.FindIndex(s => s.Id == prevStopInArray.Id) + 1);
                            var nextNodeIndex = manager.NodeToIndex(stops.FindIndex(s => s.Id == nextStopInArray.Id) + 1);

                            _logger.LogInformation("Fixing stop at MIDDLE position {Pos}: {Prev} -> {Fixed} -> {Next}",
                                fixedPosition.Value, prevStopInArray.Name, stops[fixedStopIndex].Name, nextStopInArray.Name);

                            // Constrain: PrevStop's next must be FixedStop
                            solver.Add(solver.MakeEquality(routing.NextVar(prevNodeIndex), fixedNodeIndex));
                            // Constrain: FixedStop's next must be NextStop
                            solver.Add(solver.MakeEquality(routing.NextVar(fixedNodeIndex), nextNodeIndex));
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Fixed stop ID {StopId} not found in stops array", fixedStopId.Value);
                    }
                }

                // 6) Arama parametreleri - Time window'ları için optimize edildi
                var searchParameters = operations_research_constraint_solver.DefaultRoutingSearchParameters();

                // Time window'lu problemler için hızlı strateji
                searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;
                searchParameters.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.GuidedLocalSearch;
                searchParameters.TimeLimit = new Google.Protobuf.WellKnownTypes.Duration { Seconds = 10 }; // 10 saniye timeout - hızlı retry için

                // Solution limit: İlk uygun çözümü bulunca dur (daha hızlı)
                searchParameters.SolutionLimit = 1;
                searchParameters.LogSearch = false; // Logging'i kapat - performans için

                _logger.LogInformation("Using PathCheapestArc strategy with 10s timeout for fast optimization");
                
                // 7) Çöz
                var solution = routing.SolveWithParameters(searchParameters);
                
                if (solution != null)
                {
                    var result = ExtractSolution(routing, manager, solution, stops, distanceMatrix);
                    _logger.LogInformation("Solution found: Distance={Distance}km, Duration={Duration}min", 
                        result.TotalDistance, result.TotalDuration);
                    return result;
                }
                else
                {
                    _logger.LogWarning("OR-Tools no solution found for start time {StartTime}", routeStartTime);
                    return new OptimizationResult 
                    { 
                        Success = false, 
                        Message = BuildNoSolutionMessage(stops, routeStartTime)
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OR-Tools optimization failed");
                return new OptimizationResult 
                { 
                    Success = false, 
                    Message = ex.Message 
                };
            }
        }
        
        private async Task<long[,]> BuildDistanceMatrix(double depotLatitude, double depotLongitude, List<OptimizationStop> stops)
        {
            // PERFORMANCE: Haversine kullan (çok daha hızlı, ~2-3 saniye)
            // Distance Matrix API çok yavaş (16x16=256 element için 2+ dakika)
            _logger.LogInformation("Building distance matrix using Haversine for {Count} stops (fast mode)", stops.Count);
            return BuildDistanceMatrixHaversine(depotLatitude, depotLongitude, stops);
        }

        private long[,] BuildDistanceMatrixHaversine(double depotLatitude, double depotLongitude, List<OptimizationStop> stops)
        {
            int size = stops.Count + 1;
            var matrix = new long[size, size];

            var allPoints = new List<(double lat, double lng)>();
            allPoints.Add((depotLatitude, depotLongitude));
            allPoints.AddRange(stops.Select(s => (s.Latitude, s.Longitude)));

            _logger.LogWarning("Using Haversine fallback for distance matrix");

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    if (i == j)
                    {
                        matrix[i, j] = 0;
                    }
                    else
                    {
                        var haversineDistance = CalculateHaversineDistance(
                            allPoints[i].lat, allPoints[i].lng,
                            allPoints[j].lat, allPoints[j].lng);
                        matrix[i, j] = (long)(haversineDistance * 1.4 * 1000); // meters
                    }
                }
            }
            return matrix;
        }
        
        
        private static string BuildNoSolutionMessage(List<OptimizationStop> stops, TimeSpan routeStartTime)
        {
            var windowStops = stops
                .Where(s => s.TimeWindowStart.HasValue || s.TimeWindowEnd.HasValue)
                .ToList();

            if (windowStops.Count == 0)
            {
                return $"Rota baslangic saati {routeStartTime:hh\\:mm} icin cozum bulunamadi.";
            }

            var invalidWindows = windowStops
                .Where(s => s.TimeWindowStart.HasValue && s.TimeWindowEnd.HasValue &&
                            s.TimeWindowEnd.Value <= s.TimeWindowStart.Value)
                .ToList();

            var endBeforeStart = windowStops
                .Where(s => s.TimeWindowEnd.HasValue && s.TimeWindowEnd.Value < routeStartTime)
                .ToList();

            var exampleWindows = windowStops
                .OrderBy(s => s.TimeWindowStart.HasValue && s.TimeWindowEnd.HasValue
                    ? (s.TimeWindowEnd.Value - s.TimeWindowStart.Value).TotalMinutes
                    : double.MaxValue)
                .ThenBy(s => s.TimeWindowEnd ?? TimeSpan.MaxValue)
                .Take(3)
                .Select(s => $"{s.Name} ({FormatTimeWindow(s)})")
                .ToList();

            var parts = new List<string>
            {
                $"Zaman pencereli optimizasyon icin cozum bulunamadi (baslangic {routeStartTime:hh\\:mm}).",
                $"Zaman penceresi olan durak sayisi: {windowStops.Count}."
            };

            if (invalidWindows.Count > 0)
            {
                var invalidExamples = invalidWindows
                    .Take(3)
                    .Select(s => $"{s.Name} ({FormatTimeWindow(s)})");
                parts.Add($"Gecersiz pencere: {string.Join(", ", invalidExamples)}.");
            }

            if (endBeforeStart.Count > 0)
            {
                var endBeforeExamples = endBeforeStart
                    .Take(3)
                    .Select(s => $"{s.Name} ({FormatTimeWindow(s)})");
                parts.Add($"Baslangictan once biten pencere: {string.Join(", ", endBeforeExamples)}.");
            }

            if (exampleWindows.Count > 0)
            {
                parts.Add($"Ornek pencereler: {string.Join(", ", exampleWindows)}.");
            }

            parts.Add("Baslangic saatini degistirmeyi veya pencereleri genisletmeyi deneyin.");

            return string.Join(" ", parts);
        }

        private List<ExcludedStop> BuildExcludedStops(List<OptimizationStop> stops, List<int> optimizedOrder)
        {
            var included = new HashSet<int>(optimizedOrder);
            var excludedStops = new List<ExcludedStop>();

            for (int i = 0; i < stops.Count; i++)
            {
                if (included.Contains(i))
                {
                    continue;
                }

                var stop = stops[i];
                var hasWindow = stop.TimeWindowStart.HasValue || stop.TimeWindowEnd.HasValue;

                excludedStops.Add(new ExcludedStop
                {
                    Stop = stop,
                    Reason = hasWindow ? "Zaman penceresi uyumsuzlugu" : "Rota optimizasyonu icin cikarildi",
                    TimeWindowConflict = hasWindow ? FormatTimeWindow(stop) : string.Empty
                });
            }

            return excludedStops;
        }

        private static string FormatTimeWindow(OptimizationStop stop)
        {
            var start = stop.TimeWindowStart.HasValue
                ? stop.TimeWindowStart.Value.ToString(@"hh\:mm")
                : "--";
            var end = stop.TimeWindowEnd.HasValue
                ? stop.TimeWindowEnd.Value.ToString(@"hh\:mm")
                : "--";
            return $"{start}-{end}";
        }

        private double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // km
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }
        
        private double ToRadians(double degrees) => degrees * Math.PI / 180;
        
        private OptimizationResult ExtractSolution(
            RoutingModel routing,
            RoutingIndexManager manager,
            Assignment solution,
            List<OptimizationStop> stops,
            long[,] distanceMatrix)
        {
            var result = new OptimizationResult
            {
                Success = true,
                OptimizedOrder = new List<int>(),
                TotalDistance = 0,
                TotalDuration = 0
            };

            // Track which stops were included in the solution
            var includedStops = new HashSet<int>();

            long index = routing.Start(0);
            long totalDistance = 0;

            while (!routing.IsEnd(index))
            {
                var node = manager.IndexToNode(index);
                if (node > 0) // 0 depo
                {
                    result.OptimizedOrder.Add(node - 1);
                    includedStops.Add(node - 1);
                    _logger.LogDebug("Route order: Node {Node} -> Stop index {StopIndex}", node, node - 1);
                }

                var previousIndex = index;
                index = solution.Value(routing.NextVar(index));
                totalDistance += routing.GetArcCostForVehicle(previousIndex, index, 0);
            }

            // Check for excluded stops (stops with time windows that were dropped)
            for (int i = 0; i < stops.Count; i++)
            {
                if (!includedStops.Contains(i) && (stops[i].TimeWindowStart.HasValue || stops[i].TimeWindowEnd.HasValue))
                {
                    _logger.LogWarning("⚠️ Stop {Name} was EXCLUDED due to time window constraint: {Start}-{End}",
                        stops[i].Name,
                        stops[i].TimeWindowStart?.ToString(@"hh\:mm") ?? "N/A",
                        stops[i].TimeWindowEnd?.ToString(@"hh\:mm") ?? "N/A");
                }
            }
            
            result.TotalDistance = totalDistance / 1000.0; // km

            // Total duration: travel time + service times
            long travelTimeSeconds = solution.Max(routing.GetDimensionOrDie("Time").CumulVar(routing.End(0)));
            long serviceTimeSeconds = result.OptimizedOrder.Sum(stopIndex =>
                stops[stopIndex].ServiceTimeMinutes * 60);

            result.TotalDuration = (int)((travelTimeSeconds + serviceTimeSeconds) / 60);
            
            _logger.LogInformation("OR-Tools optimization completed. Order: {Order}, Distance: {Distance}km, Duration: {Duration}min",
                string.Join(",", result.OptimizedOrder), result.TotalDistance, result.TotalDuration);
            return result;
        }

        private async Task<External.Google.Models.DirectionsResponse> GetDirectionsForAllStops(
            double depotLatitude,
            double depotLongitude,
            List<OptimizationStop> stops)
        {
            if (stops.Count > GoogleDirectionsMaxWaypoints)
            {
                _logger.LogWarning("Skipping Google Directions API for {Count} stops (limit {Limit}); using Haversine fallback",
                    stops.Count, GoogleDirectionsMaxWaypoints);
                return null;
            }

            var origin = $"{depotLatitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{depotLongitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            var destination = origin; // Circular route

            var waypoints = stops.Select(s =>
                $"{s.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{s.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
            ).ToList();

            _logger.LogInformation("Calling Google Directions API with {Count} waypoints", waypoints.Count);

            // optimize=false çünkü biz OR-Tools ile optimize edeceğiz
            var response = await _googleApiService.GetDirections(origin, destination, waypoints, false, false);

            if (response?.Routes?.FirstOrDefault()?.Legs == null)
            {
                _logger.LogWarning("Google Directions API returned no routes or legs");
                return null;
            }

            _logger.LogInformation("✅ Google API success: {LegCount} legs received", response.Routes.First().Legs.Count);
            return response;
        }

        private long[,] BuildDistanceMatrixFromDirections(
            External.Google.Models.DirectionsResponse directionsResponse,
            double depotLatitude,
            double depotLongitude,
            List<OptimizationStop> stops)
        {
            int size = stops.Count + 1;
            var distanceMatrix = new long[size, size];

            if (directionsResponse?.Routes?.FirstOrDefault()?.Legs == null)
            {
                _logger.LogWarning("No directions response, falling back to Haversine for distance matrix");
                return BuildDistanceMatrixHaversine(depotLatitude, depotLongitude, stops);
            }

            var legs = directionsResponse.Routes.First().Legs;
            _logger.LogInformation("✅ Building distance matrix from {LegCount} Google Directions legs (FAST)", legs.Count);

            // Google'dan gelen legs sırası: depot→stop1, stop1→stop2, ..., stopN→depot
            // Bunu distance matrix'e dönüştürmemiz gerekiyor

            // İlk olarak, Google'ın verdiği sıralı mesafeleri kaydet
            for (int i = 0; i < legs.Count && i <= stops.Count; i++)
            {
                var leg = legs[i];
                if (leg.Distance != null)
                {
                    if (i == 0)
                    {
                        // Depot → First stop
                        distanceMatrix[0, 1] = leg.Distance.Value;
                    }
                    else if (i < stops.Count)
                    {
                        // Stop i-1 → Stop i
                        distanceMatrix[i, i + 1] = leg.Distance.Value;
                    }
                    else
                    {
                        // Last stop → Depot
                        distanceMatrix[stops.Count, 0] = leg.Distance.Value;
                    }
                }
            }

            // Diğer tüm kombinasyonlar için: Simetrik matrix oluştur
            // NOT: Directions API sadece sıralı legs veriyor, ama OR-Tools simetrik matrix bekliyor
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    if (i == j)
                    {
                        distanceMatrix[i, j] = 0;
                    }
                    else if (distanceMatrix[i, j] == 0) // Henüz dolduulmamış
                    {
                        // Tersi var mı kontrol et (simetrik matrix)
                        if (distanceMatrix[j, i] > 0)
                        {
                            distanceMatrix[i, j] = distanceMatrix[j, i]; // Simetrik
                        }
                        else
                        {
                            // Haversine ile direkt mesafe
                            distanceMatrix[i, j] = EstimateDistance(i, j, depotLatitude, depotLongitude, stops);
                        }
                    }
                }
            }

            _logger.LogInformation("✅ Distance matrix built from Google Directions legs");
            return distanceMatrix;
        }

        private long[,] BuildTimeMatrixFromDirections(
            External.Google.Models.DirectionsResponse directionsResponse,
            double depotLatitude,
            double depotLongitude,
            List<OptimizationStop> stops)
        {
            int size = stops.Count + 1;
            var timeMatrix = new long[size, size];

            if (directionsResponse?.Routes?.FirstOrDefault()?.Legs == null)
            {
                _logger.LogWarning("No directions response, falling back to Haversine estimation");
                // Fallback: Haversine tahmini kullan
                return BuildTimeMatrixFallback(depotLatitude, depotLongitude, stops);
            }

            var legs = directionsResponse.Routes.First().Legs;
            _logger.LogInformation("Building time matrix from {LegCount} Google Directions legs", legs.Count);

            // Google'dan gelen legs sırası: depot→stop1, stop1→stop2, ..., stopN→depot
            // Bunu time matrix'e dönüştürmemiz gerekiyor

            // İlk olarak, Google'ın verdiği sıralı süreleri kaydet
            for (int i = 0; i < legs.Count && i <= stops.Count; i++)
            {
                var leg = legs[i];
                if (leg.Duration != null)
                {
                    if (i == 0)
                    {
                        // Depot → First stop
                        timeMatrix[0, 1] = leg.Duration.Value;
                    }
                    else if (i < stops.Count)
                    {
                        // Stop i-1 → Stop i
                        timeMatrix[i, i + 1] = leg.Duration.Value;
                    }
                    else
                    {
                        // Last stop → Depot
                        timeMatrix[stops.Count, 0] = leg.Duration.Value;
                    }
                }
            }

            // Diğer tüm kombinasyonlar için: Simetrik matrix oluştur
            // NOT: Directions API sadece sıralı legs veriyor, ama OR-Tools simetrik matrix bekliyor
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    if (i == j)
                    {
                        timeMatrix[i, j] = 0;
                    }
                    else if (timeMatrix[i, j] == 0) // Henüz dolduulmamış
                    {
                        // Tersi var mı kontrol et (simetrik matrix)
                        if (timeMatrix[j, i] > 0)
                        {
                            timeMatrix[i, j] = timeMatrix[j, i]; // Simetrik
                        }
                        else
                        {
                            // Haversine ile direkt süre
                            timeMatrix[i, j] = EstimateTime(i, j, depotLatitude, depotLongitude, stops);
                        }
                    }
                }
            }

            _logger.LogInformation("Time matrix built from Google Directions legs");
            return timeMatrix;
        }

        private long[,] BuildTimeMatrixFallback(double depotLatitude, double depotLongitude, List<OptimizationStop> stops)
        {
            _logger.LogInformation("Using fallback time matrix (Haversine estimation)");
            int size = stops.Count + 1;
            var timeMatrix = new long[size, size];

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    if (i == j)
                    {
                        timeMatrix[i, j] = 0;
                    }
                    else
                    {
                        timeMatrix[i, j] = EstimateTime(i, j, depotLatitude, depotLongitude, stops);
                    }
                }
            }

            return timeMatrix;
        }

        private long EstimateDistance(int from, int to, double depotLatitude, double depotLongitude, List<OptimizationStop> stops)
        {
            double fromLat, fromLng, toLat, toLng;

            if (from == 0)
            {
                fromLat = depotLatitude;
                fromLng = depotLongitude;
            }
            else
            {
                fromLat = stops[from - 1].Latitude;
                fromLng = stops[from - 1].Longitude;
            }

            if (to == 0)
            {
                toLat = depotLatitude;
                toLng = depotLongitude;
            }
            else
            {
                toLat = stops[to - 1].Latitude;
                toLng = stops[to - 1].Longitude;
            }

            var haversineKm = CalculateHaversineDistance(fromLat, fromLng, toLat, toLng);
            return (long)(haversineKm * 1.4 * 1000); // meters with road factor
        }

        private long EstimateTime(int from, int to, double depotLatitude, double depotLongitude, List<OptimizationStop> stops)
        {
            double fromLat, fromLng, toLat, toLng;

            if (from == 0)
            {
                fromLat = depotLatitude;
                fromLng = depotLongitude;
            }
            else
            {
                fromLat = stops[from - 1].Latitude;
                fromLng = stops[from - 1].Longitude;
            }

            if (to == 0)
            {
                toLat = depotLatitude;
                toLng = depotLongitude;
            }
            else
            {
                toLat = stops[to - 1].Latitude;
                toLng = stops[to - 1].Longitude;
            }

            var distanceKm = CalculateHaversineDistance(fromLat, fromLng, toLat, toLng) * 1.4;

            // İyileştirilmiş hız tahmini
            double avgSpeedKmh;
            if (distanceKm < 1)
                avgSpeedKmh = 15;
            else if (distanceKm < 3)
                avgSpeedKmh = 25;
            else if (distanceKm < 8)
                avgSpeedKmh = 40;
            else if (distanceKm < 15)
                avgSpeedKmh = 55;
            else
                avgSpeedKmh = 70;

            var timeHours = distanceKm / avgSpeedKmh;
            return (long)(timeHours * 3600);
        }
    }
}


