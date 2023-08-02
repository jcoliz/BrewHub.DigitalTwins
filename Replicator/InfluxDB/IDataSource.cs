// Copyright (C) 2023 James Coliz, Jr. <jcoliz@outlook.com> All rights reserved
// Use of this source code is governed by the MIT license (see LICENSE file)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BrewHub.Dashboard.Core.Models;

namespace BrewHub.Dashboard.Core.Providers
{
    public interface IDataSource
    {
        /// <summary>
        /// Get latest value for all telemetry from all devices
        /// </summary>
        /// <returns>
        /// Dictionary of device names to component/metric key-value pairs
        /// </returns>
        public Task<IEnumerable<Datapoint>> GetLatestDeviceTelemetryAllAsync();

        /// <summary>
        /// Get latest value for all metrics for one device
        /// </summary>
        /// <param name="deviceid">Which device</param>
        /// <returns>
        /// Dictionary of component names (or string.empty) to metric key-value pairs
        /// </returns>
        public Task<IEnumerable<Datapoint>> GetLatestDevicePropertiesAsync(string deviceid);

        /// <summary>
        /// Get all metrics for one device over time
        /// </summary>
        /// <param name="deviceid">Which device</param>
        /// <param name="lookback">How far back from now to look</param>
        /// <param name="interval">How much time should each data point cover</param>
        /// <returns>
        /// Dictionary of component/field names to list of time/values
        /// </returns>
        public Task<IEnumerable<Datapoint>> GetSingleDeviceTelemetryAsync(string deviceid, TimeSpan lookback, TimeSpan interval);

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
        public Task<IEnumerable<Datapoint>> GetSingleDeviceMetricsAsync(string deviceid, IEnumerable<Datapoint> metrics, TimeSpan lookback, TimeSpan interval);

        /// <summary>
        /// Get all metrics for one component over time
        /// </summary>
        /// <param name="deviceid">Which device</param>
        /// <param name="componentid">Which component or null</param>
        /// <param name="lookback">How far back from now to look</param>
        /// <param name="interval">How much time should each data point cover</param>
        /// <returns>
        /// Dictionary of component/field names to list of time/values
        /// </returns>
        public Task<IEnumerable<Datapoint>> GetSingleComponentTelemetryAsync(string deviceid, string? componentid, TimeSpan lookback, TimeSpan interval);
    }
}
