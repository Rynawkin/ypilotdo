using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Monolith.WebAPI.Applications.Queries.Reports;
using Monolith.WebAPI.Extensions;
using Monolith.WebAPI.Responses.Reports;
using Swashbuckle.AspNetCore.Annotations;

namespace Monolith.WebAPI.Controllers;

[Route("api/reports")]
[ApiController]
[Authorize(AuthenticationSchemes = "Bearer")]
[SwaggerControllerOrder(15)]
public class ReportsController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Get delivery trends for specified days
    /// Driver sees only their own data, others see all
    /// </summary>
    [HttpGet("delivery-trends")]
    [SwaggerOperation(Summary = "Get delivery trends for analytics")]
    public async Task<IEnumerable<DeliveryTrendResponse>> GetDeliveryTrends([FromQuery] int days = 7)
    {
        var query = new GetDeliveryTrendsQuery
        {
            AuthenticatedUserId = User.GetId(),
            Days = days
        };
        return await sender.Send(query);
    }

    /// <summary>
    /// Get driver performance metrics
    /// Driver sees only their own performance, Dispatcher+ sees all
    /// </summary>
    [HttpGet("driver-performance")]
    [SwaggerOperation(Summary = "Get driver performance analytics")]
    public async Task<IEnumerable<DriverPerformanceResponse>> GetDriverPerformance(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var query = new GetDriverPerformanceQuery
        {
            AuthenticatedUserId = User.GetId(),
            FromDate = from,
            ToDate = to
        };
        return await sender.Send(query);
    }

    /// <summary>
    /// Get vehicle utilization metrics
    /// </summary>
    [HttpGet("vehicle-utilization")]
    [SwaggerOperation(Summary = "Get vehicle utilization analytics")]
    public async Task<IEnumerable<VehicleUtilizationResponse>> GetVehicleUtilization(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var query = new GetVehicleUtilizationQuery
        {
            AuthenticatedUserId = User.GetId(),
            FromDate = from,
            ToDate = to
        };
        return await sender.Send(query);
    }

    /// <summary>
    /// Get summary statistics for dashboard
    /// Driver sees only their own stats, others see all
    /// </summary>
    [HttpGet("summary")]
    [SwaggerOperation(Summary = "Get summary statistics")]
    public async Task<SummaryStatsResponse> GetSummaryStats([FromQuery] string period = "month")
    {
        var query = new GetSummaryStatsQuery
        {
            AuthenticatedUserId = User.GetId(),
            Period = period
        };
        return await sender.Send(query);
    }

    /// <summary>
    /// Get failure reasons statistics for analytics
    /// Driver sees only their own data, others see all
    /// </summary>
    [HttpGet("failure-reasons")]
    [SwaggerOperation(Summary = "Get failure reasons statistics")]
    public async Task<IEnumerable<FailureReasonResponse>> GetFailureReasons(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string period = "month")
    {
        var query = new GetFailureReasonsQuery
        {
            AuthenticatedUserId = User.GetId(),
            FromDate = from,
            ToDate = to,
            Period = period
        };
        return await sender.Send(query);
    }

    /// <summary>
    /// Get customer satisfaction metrics
    /// Only Dispatcher and above can see
    /// </summary>
    [HttpGet("customer-satisfaction")]
    [SwaggerOperation(Summary = "Get customer satisfaction metrics")]
    [Authorize(Roles = "Dispatcher,Admin,SuperAdmin")]
    public async Task<CustomerSatisfactionResponse> GetCustomerSatisfaction(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var query = new GetCustomerSatisfactionQuery
        {
            AuthenticatedUserId = User.GetId(),
            FromDate = from,
            ToDate = to
        };
        return await sender.Send(query);
    }
}