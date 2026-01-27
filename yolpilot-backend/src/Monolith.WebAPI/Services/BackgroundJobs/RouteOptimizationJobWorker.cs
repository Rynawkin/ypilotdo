using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands.Journeys;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Journeys;

namespace Monolith.WebAPI.Services.BackgroundJobs;

public class RouteOptimizationJobWorker : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RouteOptimizationJobWorker> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(2);
    private readonly TimeSpan _jobTimeout = TimeSpan.FromMinutes(10);
    private readonly SemaphoreSlim _jobSemaphore = new(2, 2);

    public RouteOptimizationJobWorker(IServiceProvider serviceProvider, ILogger<RouteOptimizationJobWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RouteOptimizationJobWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessNextJob(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing optimization jobs");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }
    }

    private async Task ProcessNextJob(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await CleanupStuckJobs(dbContext, cancellationToken);

        if (!await _jobSemaphore.WaitAsync(0, cancellationToken))
        {
            return;
        }

        var job = await dbContext.RouteOptimizationJobs
            .OrderBy(j => j.CreatedAt)
            .FirstOrDefaultAsync(j => j.Status == RouteOptimizationJobStatus.Pending, cancellationToken);

        if (job == null)
        {
            _jobSemaphore.Release();
            return;
        }

        _logger.LogInformation("Processing optimization job (JobId: {JobId}, RouteId: {RouteId})", job.PublicId, job.RouteId);

        var now = DateTime.UtcNow;
        job.Status = RouteOptimizationJobStatus.Running;
        job.StartedAt = now;
        job.UpdatedAt = now;
        job.Message = "Optimization running.";
        await dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Optimization job started (JobId: {JobId}, RouteId: {RouteId})", job.PublicId, job.RouteId);

        _ = Task.Run(() => RunJobAsync(job.PublicId, cancellationToken), CancellationToken.None);
    }

    private async Task RunJobAsync(Guid jobPublicId, CancellationToken cancellationToken)
    {
        RouteOptimizationJob job = null;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();

            job = await dbContext.RouteOptimizationJobs
                .FirstOrDefaultAsync(j => j.PublicId == jobPublicId, cancellationToken);

            if (job == null)
            {
                _logger.LogWarning("Optimization job not found when starting RunJobAsync (JobId: {JobId})", jobPublicId);
                return;
            }

            var command = new OptimizeRouteCommand
            {
                RouteId = job.RouteId,
                OptimizationMode = string.IsNullOrWhiteSpace(job.OptimizationMode) ? "distance" : job.OptimizationMode,
                AvoidTolls = job.AvoidTolls,
                PreserveOrder = job.PreserveOrder,
                IsTimeDeviationOptimization = job.IsTimeDeviationOptimization,
                AuthenticatedUserId = job.RequestedByUserId
            };

            var result = await RunOptimizationWithTimeout(sender, command, cancellationToken);

            job.Status = RouteOptimizationJobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            job.Message = TrimMessage(result.Message);
            job.ResultJson = JsonSerializer.Serialize(result, JsonOptions);

            _logger.LogInformation("Optimization job finished (JobId: {JobId}, Status: {Status})", job.PublicId, job.Status);
        }
        catch (TimeoutException ex)
        {
            if (job != null)
            {
                job.Status = RouteOptimizationJobStatus.Failed;
                job.CompletedAt = DateTime.UtcNow;
                job.Message = "Optimization timed out. Job exceeded server timeout.";
                job.Error = ex.ToString();
            }

            _logger.LogWarning(ex, "Optimization job timed out (JobId: {JobId})", jobPublicId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (job != null)
            {
                job.Status = RouteOptimizationJobStatus.Failed;
                job.CompletedAt = DateTime.UtcNow;
                job.Message = "Optimization failed.";
                job.Error = ex.ToString();
            }

            _logger.LogError(ex, "Optimization job failed (JobId: {JobId})", jobPublicId);
        }
        finally
        {
            if (job != null)
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var trackedJob = await dbContext.RouteOptimizationJobs
                    .FirstOrDefaultAsync(j => j.PublicId == jobPublicId, cancellationToken);

                if (trackedJob != null)
                {
                    trackedJob.Status = job.Status;
                    trackedJob.CompletedAt = job.CompletedAt;
                    trackedJob.Message = job.Message;
                    trackedJob.Error = job.Error;
                    trackedJob.ResultJson = job.ResultJson;
                    trackedJob.UpdatedAt = DateTime.UtcNow;

                    await dbContext.SaveChangesAsync(cancellationToken);
                }
            }

            _jobSemaphore.Release();
        }
    }

    private static string TrimMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return string.Empty;
        }

        return message.Length <= 1000 ? message : message[..1000];
    }

    private async Task<OptimizeRouteResponse> RunOptimizationWithTimeout(
        ISender sender,
        OptimizeRouteCommand command,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_jobTimeout);

        var optimizationTask = Task.Run(() => sender.Send(command, timeoutCts.Token), timeoutCts.Token);
        var timeoutTask = Task.Delay(_jobTimeout, CancellationToken.None);
        var completedTask = await Task.WhenAny(optimizationTask, timeoutTask);

        if (completedTask == timeoutTask)
        {
            timeoutCts.Cancel();

            _ = optimizationTask.ContinueWith(task =>
            {
                if (task.Exception != null)
                {
                    _logger.LogError(task.Exception, "Optimization task faulted after timeout");
                }
            }, TaskContinuationOptions.OnlyOnFaulted);

            throw new TimeoutException($"Optimization exceeded {_jobTimeout.TotalMinutes:F0} minutes.");
        }

        return await optimizationTask;
    }

    private async Task CleanupStuckJobs(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow - _jobTimeout;

        var stuckJobs = await dbContext.RouteOptimizationJobs
            .Where(j => j.Status == RouteOptimizationJobStatus.Running
                && ((j.StartedAt != null && j.StartedAt < cutoff) || (j.StartedAt == null && j.CreatedAt < cutoff)))
            .ToListAsync(cancellationToken);

        if (!stuckJobs.Any())
        {
            return;
        }

        foreach (var stuckJob in stuckJobs)
        {
            stuckJob.Status = RouteOptimizationJobStatus.Failed;
            stuckJob.CompletedAt = DateTime.UtcNow;
            stuckJob.Message = "Optimization timed out. Job exceeded server timeout.";
            stuckJob.Error = "Job exceeded server timeout.";
            stuckJob.UpdatedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogWarning("Reset {Count} stuck optimization job(s) as failed", stuckJobs.Count);
    }
}
