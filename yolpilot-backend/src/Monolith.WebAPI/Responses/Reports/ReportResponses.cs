namespace Monolith.WebAPI.Responses.Reports;

public class DeliveryTrendResponse
{
    public string Date { get; set; } = "";
    public int Completed { get; set; }
    public int Failed { get; set; }
    public int Total { get; set; }
    public double SuccessRate => Total > 0 ? Math.Round((double)Completed / Total * 100, 2) : 0;
}

public class DriverPerformanceResponse
{
    public int DriverId { get; set; }
    public string DriverName { get; set; } = "";
    public int TotalDeliveries { get; set; }
    public int CompletedDeliveries { get; set; }
    public int FailedDeliveries { get; set; }
    public double AvgDeliveryTime { get; set; } // dakika
    public double TotalDistance { get; set; } // km
    public double Rating { get; set; }
    public double SuccessRate => TotalDeliveries > 0 ? Math.Round((double)CompletedDeliveries / TotalDeliveries * 100, 2) : 0;
}

public class VehicleUtilizationResponse
{
    public int VehicleId { get; set; }
    public string PlateNumber { get; set; } = "";
    public string VehicleType { get; set; } = "";
    public int TotalRoutes { get; set; }
    public int TotalDeliveries { get; set; }
    public double TotalDistance { get; set; } // km
    public double UtilizationRate { get; set; } // yüzde
    public string Status { get; set; } = "";
}

public class SummaryStatsResponse
{
    public int TotalDeliveries { get; set; }
    public int CompletedDeliveries { get; set; }
    public int FailedDeliveries { get; set; }
    public double SuccessRate { get; set; }
    public double AvgDeliveryTime { get; set; } // dakika
    public double TotalDistance { get; set; } // km
    public int ActiveDrivers { get; set; }
    public int ActiveVehicles { get; set; }
    public int TotalCustomers { get; set; }
    public int ActiveRoutes { get; set; }
    
    // Trend verileri (önceki dönem karşılaştırması)
    public double DeliveryChange { get; set; } // yüzde değişim
    public double SuccessRateChange { get; set; }
    public double AvgTimeChange { get; set; }
}

public class CustomerSatisfactionResponse
{
    public double OverallRating { get; set; }
    public int TotalRatings { get; set; }
    public Dictionary<string, int> RatingDistribution { get; set; } = new();
    public List<CustomerPriorityData> PriorityDistribution { get; set; } = new();
    public List<TopCustomerData> TopCustomers { get; set; } = new();
}

public class CustomerPriorityData
{
    public string Priority { get; set; } = "";
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class TopCustomerData
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    public string Address { get; set; } = "";
    public int TotalDeliveries { get; set; }
    public string Priority { get; set; } = "";
}

public class FailureReasonResponse
{
    public string Reason { get; set; } = "";
    public int Count { get; set; }
    public double Percentage { get; set; }
}