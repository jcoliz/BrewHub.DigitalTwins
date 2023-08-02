// Copyright (C) 2023 James Coliz, Jr. <jcoliz@outlook.com> All rights reserved
// Use of this source code is governed by the MIT license (see LICENSE file)

using InfluxDB.Client;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Text.RegularExpressions;
using BrewHub.Dashboard.Core.Models;
using BrewHub.Dashboard.Core.Providers;

namespace DashboardIoT.InfluxDB
{
    public class InfluxDBDataSource : IDataSource, IDisposable
    {
        private readonly ILogger _logger;
        private readonly InfluxDBClient _influxdbclient;
        private readonly Options _options;

        public class Options
        {
            public const string Section = "InfluxDB";
            public string? Url { get; set; }
            public string? Token { get; set; }
            public string? Org { get; set; }
            public string? Bucket { get; set; }
        }

        public class QueryVariables
        {
            public QueryVariables(Options options, TimeSpan span, int divisions)
            {
                var now = DateTime.Now;
                _SetTimeRangeStart = now - span;
                _SetTimeRangeStop = now;
                _SetWindowPeriod = span/divisions;

                Organization = options.Org!;
                Bucket = options.Bucket!;
            }

            private readonly DateTime _SetTimeRangeStart;

            public string TimeRangeStart => _SetTimeRangeStart.ToString("O");

            private readonly DateTime _SetTimeRangeStop;

            public string TimeRangeStop => _SetTimeRangeStop.ToString("O");

            private readonly TimeSpan _SetWindowPeriod;

            public string WindowPeriod => Math.Round(_SetWindowPeriod.TotalSeconds) + "s";

            public string Organization { get; private set; }
            public string Bucket { get; private set; }
        }

        public InfluxDBDataSource(IOptions<Options> options, ILoggerFactory logfact)
        {
            _logger = logfact.CreateLogger(nameof(InfluxDBDataSource));
            _options = options.Value;
            try
            {
                _influxdbclient = InfluxDBClientFactory.Create(_options.Url, _options.Token);
                _logger.LogInformation("Created client OK on {url}",_options.Url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Create client failed");
                throw;
            }
        }

        public void Dispose()
        {
            _influxdbclient.Dispose();
        }

        private Datapoint FluxToDatapoint(Dictionary<string,object> d)
        {
            return new Datapoint() 
            { 
                __Device = d["device"].ToString()!, 
                __Model = d["_measurement"].ToString()!, 
                __Component = d.GetValueOrDefault("component")?.ToString(),
                __Time = d.ContainsKey("_time") ? ((NodaTime.Instant)d["_time"]).ToDateTimeOffset() : DateTimeOffset.MinValue,
                __Field = d["_field"].ToString()!, 
                __Value = d["_value"]
            };
        }

        private async Task<IEnumerable<Datapoint>> DoFluxQueryAsync(string query)
        {
            try
            {
                var fluxTables = await _influxdbclient.GetQueryApi().QueryAsync(query, _options.Org);

                return fluxTables
                    .SelectMany(x => x.Records)
                    .Select(x => FluxToDatapoint(x.Values));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InfluxDB: Query Failed");
                throw;
            }
        }

        // Actually, I'm going to return ALL the metrics, and let the caller sort
        // out what's telemetry and waht's not. Because right now, the DB doesn't
        // have a great way of telling.
        public async Task<IEnumerable<Datapoint>> GetLatestDeviceTelemetryAllAsync()
        {
            // TODO: This is where it would be great to have a tag for type=telemetry

            var flux = $"from(bucket:\"{_options.Bucket}\")" +
                " |> range(start: -10m)" +
                " |> filter(fn: (r) => exists r[\"device\"] )" +
                " |> filter(fn: (r) => r[\"msgtype\"] != \"NCMD\")" +
                " |> last()" +
                " |> keep(columns: [ \"device\", \"component\", \"_field\", \"_value\", \"_measurement\" ])";

            return await DoFluxQueryAsync(flux);
        }

        /// <summary>
        /// Get latest value for all metrics for one device
        /// </summary>
        /// <param name="deviceid">Which device</param>
        /// <returns>
        /// Dictionary of component names (or string.empty) to telemetry key-value pairs
        /// </returns>
        public async Task<IEnumerable<Datapoint>> GetLatestDevicePropertiesAsync(string deviceid)
        {
            var flux = $"from(bucket:\"{_options.Bucket}\")" +
                " |> range(start: -24h)" +
                $" |> filter(fn: (r) => r[\"device\"] == \"{deviceid}\")" +
                " |> filter(fn: (r) => r[\"msgtype\"] != \"NCMD\")" +
                " |> filter(fn: (r) => r[\"_field\"] != \"Seq\" and r[\"_field\"] != \"__t\")" +
                " |> last()" +
                " |> keep(columns: [ \"device\", \"component\", \"_field\", \"_value\", \"_measurement\" ])";

            return await DoFluxQueryAsync(flux);
        }
        /// <summary>
        /// Get all metrics for one device over time
        /// </summary>
        /// <param name="deviceid">Which device</param>
        /// <param name="lookback">How far back from now to look</param>
        /// <param name="interval">How much time should each data point cover</param>
        /// <remarks>
        /// That this gets ALL metrics in the lookback window, not just telemetry
        /// Then it's up to the caller to sort out what to do with that.
        /// </remarks>
        /// <returns>
        /// Dictionary of component/field names to list of time/values
        /// </returns>
        public async Task<IEnumerable<Datapoint>> GetSingleDeviceTelemetryAsync(string deviceid, TimeSpan lookback, TimeSpan interval)
        {
            try
            {
                // Convert timespan into flux time construct
                Regex regex = new Regex("^[PT]+(?<value>.+)");
                string lookbackstr = regex.Match(XmlConvert.ToString(lookback)).Groups["value"].Value.ToLowerInvariant();
                string intervalstr = regex.Match(XmlConvert.ToString(interval)).Groups["value"].Value.ToLowerInvariant();

                // TODO: This is where it would be great to have a tag for type=telemetry
                // Right now, this is a MASSIVE overfetch.
                var flux = 
                     "import \"types\" " + 
                    $"from(bucket:\"{_options.Bucket}\")" +
                    $" |> range(start: -{lookbackstr})" +
                    $" |> filter(fn: (r) => r[\"device\"] == \"{deviceid}\")" +
                    " |> filter(fn: (r) => r[\"msgtype\"] != \"NCMD\")" +
                    "  |> filter(fn: (r) => types.isType(v: r._value, type: \"int\") or types.isType(v: r._value, type: \"float\"))" +
                     " |> keep(columns: [ \"device\", \"component\", \"_field\", \"_value\", \"_time\", \"_measurement\" ])" +
                    $" |> aggregateWindow(every: {intervalstr}, fn: mean, createEmpty: false)" +
                     " |> yield(name: \"mean\")";

                return await DoFluxQueryAsync(flux);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InfluxDB: Query Composition Failed");
                throw;
            }
        }

        /// <summary>
        /// Get only selected metrics for one device over time
        /// </summary>
        /// <param name="deviceid">Which device</param>
        /// <param name="metrics">Which metrics. Fill in: __Model, __Component, __Field</param>
        /// <param name="lookback">How far back from now to look</param>
        /// <param name="interval">How much time should each data point cover</param>
        /// <returns>
        /// Dictionary of component/field names to list of time/values
        /// </returns>
        public async Task<IEnumerable<Datapoint>> GetSingleDeviceMetricsAsync(string deviceid, IEnumerable<Datapoint> metrics, TimeSpan lookback, TimeSpan interval)
        {
            try
            {
                // Convert timespan into flux time construct
                Regex regex = new Regex("^[PT]+(?<value>.+)");
                string lookbackstr = regex.Match(XmlConvert.ToString(lookback)).Groups["value"].Value.ToLowerInvariant();
                string intervalstr = regex.Match(XmlConvert.ToString(interval)).Groups["value"].Value.ToLowerInvariant();

                // Fixes Bug 1648: Dashboard timeout on BrewBox with timespan=Day
                // 
                // We will craft a single query for each metric, and run them separately.
                // I think this will be faster than the over-fetching we did previously.
                //
                // Could be optimized further with a single query PER MODEL in the supplied metrics.
                var result = new List<Datapoint>();

                // Need to have a common end time, so the last point always has the same timestamp
                long endtime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                foreach(var metric in metrics)
                {
                    var flux = 
                        $"from(bucket:\"{_options.Bucket}\")" +
                        $" |> range(start: -{lookbackstr}, stop:{endtime})" +
                        " |> filter(fn: (r) => r[\"msgtype\"] != \"NCMD\")" +
                        $" |> filter(fn: (r) => r[\"_measurement\"] == \"{metric.__Model}\")" +
                        $" |> filter(fn: (r) => r[\"_field\"] == \"{metric.__Field}\")" +
                        $" |> filter(fn: (r) => r[\"device\"] == \"{deviceid}\")" +
                        (
                            (metric.__Component is null) ?
                            " |> filter(fn: (r) => not exists r[\"component\"] )" :
                            $" |> filter(fn: (r) => r[\"component\"] == \"{metric.__Component}\")"
                        ) +
                        $" |> aggregateWindow(every: {intervalstr}, fn: mean, createEmpty: false)" +
                        " |> yield(name: \"mean\")";

                    var data = await DoFluxQueryAsync(flux);
                    result.AddRange(data);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InfluxDB: Query Composition Failed");
                throw;
            }
        }


        public async Task<IEnumerable<Datapoint>> GetSingleComponentTelemetryAsync(string deviceid, string? componentid, TimeSpan lookback, TimeSpan interval)
        {
            try
            {
                // Convert timespan into flux time construct
                Regex regex = new Regex("^[PT]+(?<value>.+)");
                string lookbackstr = regex.Match(XmlConvert.ToString(lookback)).Groups["value"].Value.ToLowerInvariant();
                string intervalstr = regex.Match(XmlConvert.ToString(interval)).Groups["value"].Value.ToLowerInvariant();

                // TODO: This is where it would be great to have a tag for type=telemetry
                // Right now, this is a MASSIVE overfetch.
                var flux = 
                     "import \"types\" " + 
                    $"from(bucket:\"{_options.Bucket}\")" +
                    $" |> range(start: -{lookbackstr})" +
                    " |> filter(fn: (r) => r[\"msgtype\"] != \"NCMD\")" +
                    $" |> filter(fn: (r) => r[\"device\"] == \"{deviceid}\")" +
                    (
                        (componentid is null) ?
                        " |> filter(fn: (r) => not exists r[\"component\"] )" :
                        $" |> filter(fn: (r) => r[\"component\"] == \"{componentid}\")"
                    ) +                    
                    "  |> filter(fn: (r) => types.isType(v: r._value, type: \"int\") or types.isType(v: r._value, type: \"float\"))" +
                     " |> keep(columns: [ \"device\", \"component\", \"_field\", \"_value\", \"_time\", \"_measurement\" ])" +
                    $" |> aggregateWindow(every: {intervalstr}, fn: mean, createEmpty: false)" +
                     " |> yield(name: \"mean\")";

                return await DoFluxQueryAsync(flux);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InfluxDB: Query Composition Failed");
                throw;
            }

        }
    }
}
