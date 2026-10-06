namespace KnowledgeCopilot.Worker;

/// <summary>Logs once a minute so the dashboard shows the worker is alive. Replaced by queue consumers in Step 3.</summary>
internal sealed partial class HeartbeatService(TimeProvider timeProvider, ILogger<HeartbeatService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, timeProvider);
        do
        {
            LogHeartbeat(logger);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Worker heartbeat")]
    private static partial void LogHeartbeat(ILogger logger);
}
