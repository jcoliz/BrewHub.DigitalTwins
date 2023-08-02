namespace BrewHub.DigitalTwins.Replicator;

public class Worker : BackgroundService
{
    private readonly ILogger _logger;

    public Worker(ILoggerFactory logfact)
    {
        _logger = logfact.CreateLogger(nameof(Worker));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("OK");
            await Task.Delay(1000, stoppingToken);
        }
    }

    protected async Task DoReplicationAsync()
    {
        // Authenticate with Digital Twins
        // Open connection to InfluxDB
        // Get last values for all metrics on this device and all its components
        // Translate into a patch document
        // Just for the device right now
        // Update it!
    }
}
