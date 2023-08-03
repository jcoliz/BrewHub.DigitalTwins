using Azure;
using BrewHub.Dashboard.Core.Models;
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

            // Do this for each one
            foreach(var device in devices)
            {
                // Get last values for all metrics on this device and all its components
                var data = await _datasource.GetLatestDevicePropertiesAsync(device);

                // If we don't have any data for this device, skip the rest!
                if (!data.Any())
                {
                    _logger.LogWarning("Digital Twins has device {device}, but we have no data for it",device);
                    continue;
                }

                // We will have ONE patch document for EACH component
                var updates = data.Select(x => x.__Component).Distinct().ToDictionary(x => x ?? string.Empty, x => new JsonPatchDocument());

                // Translate into a patch document
                var updateTwinData = new JsonPatchDocument();

                // Identify telemetry, which has to be handled differently
                var telemetrys = new Dictionary<string, string[]>()
                {
                    { "dtmi:brewhub:prototypes:still_6_unit;1", new[] { "WorkingSet", "CpuLoad", "Status" } },
                    { "dtmi:brewhub:sensors:TH;1", new[] { "t", "h" } },
                    { "dtmi:brewhub:controls:Thermostat;1", new[] { "t", "Status" } }
                };

                // Add a patch for each non-telemetry
                foreach(var point in data.Where(x => telemetrys.ContainsKey(x.__Model) && !telemetrys[x.__Model].Contains(x.__Field) ))
                {
                    updates[point.__Component ?? string.Empty].AppendReplace($"/{point.__Field}", point.__Value);
                }

                var metrics = data
                    .Where(x => telemetrys.ContainsKey(x.__Model) && telemetrys[x.__Model].Contains(x.__Field));

                // Upload telemetry to "Current{Metric}" property
                var values = await _datasource.GetSingleDeviceMetricsAsync(device, metrics, _warminterval, _warminterval);

                // Just need the early values
                var firstvalues = values.ToLookup(x => x.__Time).OrderBy(x=>x.Key).FirstOrDefault();
                if (firstvalues is not null)
                {
                    foreach(var point in firstvalues)
                    {
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
