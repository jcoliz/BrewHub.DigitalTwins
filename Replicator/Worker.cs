using Azure;
using BrewHub.Dashboard.Core.Providers;

namespace BrewHub.DigitalTwins.Replicator;

public class Worker : BackgroundService
{
    private readonly IDataSource _datasource;
    private readonly ITwinsClient _twins;
    private readonly ILogger _logger;

    public Worker(IDataSource datasource, ITwinsClient twins, ILoggerFactory logfact)
    {
        _datasource = datasource;
        _twins = twins;
        _logger = logfact.CreateLogger(nameof(Worker));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await DoReplicationAsync();
            _logger.LogInformation("OK");
            await Task.Delay(1000, stoppingToken);
        }
    }

    protected async Task DoReplicationAsync()
    {
        try
        {
            // Get last values for all metrics on this device and all its components
            var device = "west-1";
            var data = await _datasource.GetLatestDevicePropertiesAsync(device);

            // Translate into a patch document
            var updateTwinData = new JsonPatchDocument();

            // Just for the device right now
            // And exclude telemetry
            var telemetry = new[] { "WorkingSet", "CpuLoad", "Status" };
            foreach(var point in data.Where(x=>x.__Component == null && !telemetry.Contains(x.__Field)))
            {
                updateTwinData.AppendReplace($"/{point.__Field}", point.__Value);
            }

            // Update it!
            var twinId = "west-1-Device";
            await _twins.UpdateDigitalTwinAsync(twinId, updateTwinData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Replication failed");
        }
    }
}
