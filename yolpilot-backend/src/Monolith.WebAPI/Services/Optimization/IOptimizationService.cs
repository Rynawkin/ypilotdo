using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Monolith.WebAPI.Data.Journeys;

namespace Monolith.WebAPI.Services.Optimization
{
    public interface IOptimizationService
    {
        Task<OptimizationResult> OptimizeRouteWithTimeWindows(
            int depotId,
            double depotLatitude,
            double depotLongitude,
            List<OptimizationStop> stops,
            TimeSpan routeStartTime, // YENİ PARAMETRE
            TimeSpan? maxRouteDuration = null,
            bool allowExclusions = false, // YENİ PARAMETRE - exclusion izni
            External.Google.Models.DirectionsResponse cachedDirectionsResponse = null, // PERFORMANCE: Cached Google API response
            int? fixedStopId = null, // REORDERING: Stop to fix at specific position
            int? fixedPosition = null); // REORDERING: Position to fix the stop at (0-indexed)
            
        Task<OptimizationResultWithExclusions> OptimizeWithExclusions(
            int depotId,
            double depotLatitude,
            double depotLongitude,
            List<OptimizationStop> stops,
            TimeSpan routeStartTime,
            TimeSpan? maxRouteDuration = null);
    }

    public class OptimizationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<int> OptimizedOrder { get; set; } = new();
        public double TotalDistance { get; set; }
        public int TotalDuration { get; set; }
    }
    
    public class OptimizationResultWithExclusions
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<int> OptimizedOrder { get; set; } = new();
        public List<ExcludedStop> ExcludedStops { get; set; } = new();
        public double TotalDistance { get; set; }
        public int TotalDuration { get; set; }
        public bool HasExclusions => ExcludedStops?.Any() ?? false;
    }
    
    public class ExcludedStop
    {
        public OptimizationStop Stop { get; set; }
        public string Reason { get; set; }
        public string TimeWindowConflict { get; set; }
    }
    
    public class OptimizationStop
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int ServiceTimeMinutes { get; set; }
        public TimeSpan? TimeWindowStart { get; set; }
        public TimeSpan? TimeWindowEnd { get; set; }
        public int? CustomerId { get; set; }
        public OrderType OrderType { get; set; } = OrderType.Auto;
    }
}