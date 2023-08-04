using Azure;
using BrewHub.Dashboard.Core.Providers;

namespace BrewHub.DigitalTwins.Replicator;

public class Worker : BackgroundService
{
    private readonly IDataSource _datasource;
    private readonly ITwinsClient _twins;
    private readonly ILogger _logger;
    private readonly TimeSpan _warminterval = TimeSpan.FromMinutes(5);

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
            await Task.Delay(_warminterval, stoppingToken);
        }
    }

    protected async Task DoReplicationAsync()
    {
        try
        {
            // Which devices does the twins instance know about?
            var devices = await _twins.QueryDevicesOfModel("dtmi:com:brewhub:machinery:distilateur;1");

            // Which models does the twins instance have?
            // TODO: This can be removed when the twins instance has EVERYTHING in the solution
            var models = new[] { "dtmi:brewhub:prototypes:still_6_unit;1", "dtmi:brewhub:sensors:TH;1", "dtmi:brewhub:controls:Thermostat;1" };

            // Do this for each one
            foreach(var device in devices)
            {
                // Get last properties on this device and all its components
                var data = await _datasource.GetLatestDevicePropertiesAsync(device);

                // If we don't have any data for this device, skip the rest!
                if (!data.Any())
                {
                    _logger.LogWarning("Digital Twins has device {device}, but we have no data for it",device);
                    continue;
                }

                // Prepare ONE patch document for EACH component
                var updates = data.Select(x => x.__Component).Distinct().ToDictionary(x => x ?? string.Empty, x => new JsonPatchDocument());

                // Add a patch for each property
                foreach(var point in data.Where(x => models.Contains(x.__Model) ))
                {
                    updates[point.__Component ?? string.Empty].AppendReplace($"/{point.__Field}", point.__Value);
                }

                // Get last properties on this device and all its components
                var values = await _datasource.GetSingleDeviceTelemetryAsync(device, _warminterval, _warminterval);

                // Use the first timeslice from these results
                var firstvalues = values.ToLookup(x => x.__Time).OrderBy(x=>x.Key).FirstOrDefault();
                if (firstvalues is not null)
                {
                    foreach(var point in firstvalues)
                    {
                        // Patch telemetry as "Current{Metric}" property
                        updates[point.__Component ?? string.Empty].AppendReplace($"/Current{point.__Field}", point.__Value);
                    }
                }

                // Update them!
                foreach(var kvp in updates)
                {
                    var twinId = device + "-" + (string.IsNullOrEmpty(kvp.Key) ? "Device" : kvp.Key);
                    await _twins.UpdateDigitalTwinAsync(twinId, kvp.Value);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Replication failed");
        }
    }
}
