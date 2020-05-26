// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Services {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Serializers;
    using Serilog;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Threading;
    using System.Collections.Generic;

    /// <summary>
    /// Publisher registry which uses the IoT Hub twin services for publisher
    /// and writer group identity management.
    /// </summary>
    public sealed class PublisherRegistry : IPublisherRegistry,
        IWriterGroupRegistryListener, IDataSetWriterRegistryListener {

        /// <summary>
        /// Create registry services
        /// </summary>
        /// <param name="iothub"></param>
        /// <param name="broker"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        public PublisherRegistry(IIoTHubTwinServices iothub, IJsonSerializer serializer,
            IRegistryEventBroker<IPublisherRegistryListener> broker, ILogger logger) {
            _iothub = iothub ?? throw new ArgumentNullException(nameof(iothub));
            _broker = broker ?? throw new ArgumentNullException(nameof(broker));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        /// <inheritdoc/>
        public async Task<PublisherModel> GetPublisherAsync(string id,
            bool onlyServerState, CancellationToken ct) {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentException(nameof(id));
            }
            var deviceId = PublisherModelEx.ParseDeviceId(id, out var moduleId);
            var device = await _iothub.GetAsync(deviceId, moduleId, ct);
            var registration = device.ToEntityRegistration(onlyServerState)
                as PublisherRegistration;
            if (registration == null) {
                throw new ResourceNotFoundException(
                    $"{id} is not a publisher registration.");
            }
            return registration.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task UpdatePublisherAsync(string publisherId,
            PublisherUpdateModel request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(publisherId)) {
                throw new ArgumentException(nameof(publisherId));
            }

            // Get existing endpoint and compare to see if we need to patch.
            var deviceId = SupervisorModelEx.ParseDeviceId(publisherId, out var moduleId);

            while (true) {
                try {
                    var twin = await _iothub.GetAsync(deviceId, moduleId, ct);
                    if (twin.Id != deviceId && twin.ModuleId != moduleId) {
                        throw new ArgumentException("Id must be same as twin to patch",
                            nameof(publisherId));
                    }

                    var registration = twin.ToEntityRegistration(true) as PublisherRegistration;
                    if (registration == null) {
                        throw new ResourceNotFoundException(
                            $"{publisherId} is not a publisher registration.");
                    }
                    // Update registration from update request
                    var patched = registration.ToServiceModel();
                    if (request.SiteId != null) {
                        patched.SiteId = string.IsNullOrEmpty(request.SiteId) ?
                            null : request.SiteId;
                    }

                    if (request.LogLevel != null) {
                        patched.LogLevel = request.LogLevel == TraceLogLevel.Information ?
                            null : request.LogLevel;
                    }

                    if (request.Configuration != null) {
                        if (patched.Configuration == null) {
                            patched.Configuration = new PublisherConfigModel();
                        }
                        if (request.Configuration.JobOrchestratorUrl != null) {
                            patched.Configuration.JobOrchestratorUrl =
                                string.IsNullOrEmpty(
                                    request.Configuration.JobOrchestratorUrl.Trim()) ?
                                        null : request.Configuration.JobOrchestratorUrl;
                        }
                        if (request.Configuration.HeartbeatInterval != null) {
                            patched.Configuration.HeartbeatInterval =
                                request.Configuration.HeartbeatInterval;
                        }
                        if (request.Configuration.JobCheckInterval != null) {
                            patched.Configuration.JobCheckInterval =
                                request.Configuration.JobCheckInterval.Value.Ticks == 0 ?
                                    null : request.Configuration.JobCheckInterval;
                        }
                        if (request.Configuration.MaxWorkers != null) {
                            patched.Configuration.MaxWorkers =
                                request.Configuration.MaxWorkers <= 0 ?
                                    null : request.Configuration.MaxWorkers;
                        }
                        if (request.Configuration.Capabilities != null) {
                            patched.Configuration.Capabilities =
                                request.Configuration.Capabilities.Count == 0 ?
                                    null : request.Configuration.Capabilities;
                        }
                    }
                    // Patch
                    twin = await _iothub.PatchAsync(registration.Patch(
                        patched.ToPublisherRegistration(), _serializer), false, ct);

                    // Send update to through broker
                    registration = twin.ToEntityRegistration(true) as PublisherRegistration;
                    await _broker.NotifyAllAsync(l => l.OnPublisherUpdatedAsync(null,
                        registration.ToServiceModel()));
                    return;
                }
                catch (ResourceOutOfDateException ex) {
                    _logger.Debug(ex, "Retrying updating publisher...");
                    continue;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<PublisherListModel> ListPublishersAsync(
            string continuation, bool onlyServerState, int? pageSize, CancellationToken ct) {
            var query = "SELECT * FROM devices.modules WHERE " +
                $"properties.reported.{TwinProperty.Type} = '{IdentityType.Publisher}' " +
                $"AND NOT IS_DEFINED(tags.{nameof(EntityRegistration.NotSeenSince)})";
            var devices = await _iothub.QueryDeviceTwinsAsync(query, continuation, pageSize, ct);
            return new PublisherListModel {
                ContinuationToken = devices.ContinuationToken,
                Items = devices.Items
                    .Select(t => t.ToPublisherRegistration(onlyServerState))
                    .Select(s => s.ToServiceModel())
                    .ToList()
            };
        }

        /// <inheritdoc/>
        public async Task<PublisherListModel> QueryPublishersAsync(
            PublisherQueryModel model, bool onlyServerState, int? pageSize, CancellationToken ct) {

            var query = "SELECT * FROM devices.modules WHERE " +
                $"properties.reported.{TwinProperty.Type} = '{IdentityType.Publisher}'";

            if (model?.SiteId != null) {
                // If site id provided, include it in search
                query += $"AND (properties.reported.{TwinProperty.SiteId} = " +
                    $"'{model.SiteId}' OR properties.desired.{TwinProperty.SiteId} = " +
                    $"'{model.SiteId}' OR deviceId = '{model.SiteId}') ";
            }

            if (model?.Connected != null) {
                // If flag provided, include it in search
                if (model.Connected.Value) {
                    query += $"AND connectionState = 'Connected' ";
                }
                else {
                    query += $"AND connectionState != 'Connected' ";
                }
            }

            var queryResult = await _iothub.QueryDeviceTwinsAsync(query, null, pageSize, ct);
            return new PublisherListModel {
                ContinuationToken = queryResult.ContinuationToken,
                Items = queryResult.Items
                    .Select(t => t.ToPublisherRegistration(onlyServerState))
                    .Select(s => s.ToServiceModel())
                    .ToList()
            };
        }

        /// <inheritdoc/>
        public async Task OnWriterGroupAddedAsync(PublisherOperationContextModel context,
            WriterGroupInfoModel writerGroup) {

            // Add new group
            var group = writerGroup.ToWriterGroupRegistration();
            await _iothub.CreateOrUpdateAsync(group.ToDeviceTwin(_serializer),
                false, CancellationToken.None);

            // assign writer group to a publisher


        }

        /// <inheritdoc/>
        public async Task OnWriterGroupUpdatedAsync(PublisherOperationContextModel context,
            WriterGroupInfoModel writerGroup) {
            if (writerGroup?.WriterGroupId == null) {
                // Should not happen
                throw new ArgumentNullException(nameof(writerGroup.WriterGroupId));
            }
            while (true) {
                try {
                    var twin = await _iothub.FindAsync(
                        WriterGroupRegistrationEx.ToDeviceId(writerGroup.WriterGroupId));
                    if (twin == null) {
                        _logger.Warning("Missed add group event - try recreating twin...");
                        twin = await _iothub.CreateOrUpdateAsync(
                            writerGroup.ToWriterGroupRegistration().ToDeviceTwin(_serializer),
                            false, CancellationToken.None);
                        return; // done
                    }
                    // Convert to writerGroup registration
                    var registration = twin.ToEntityRegistration() as WriterGroupRegistration;
                    if (registration == null) {
                        _logger.Fatal("Unexpected - twin is not a writerGroup registration.");
                        return; // nothing else to do other than delete and recreate.
                    }
                    twin = await _iothub.PatchAsync(registration.Patch(
                        writerGroup.ToWriterGroupRegistration(), _serializer));
                    break;
                }
                catch (ResourceOutOfDateException ex) {
                    // Retry create/update
                    _logger.Debug(ex, "Retry updating writerGroup...");
                }
            }
        }

        /// <inheritdoc/>
        public async Task OnDataSetWriterAddedAsync(PublisherOperationContextModel context,
            DataSetWriterInfoModel dataSetWriter) {
            var writerGroupId = dataSetWriter?.WriterGroupId;
            if (string.IsNullOrEmpty(writerGroupId)) {
                // Should not happen
                throw new ArgumentNullException(nameof(dataSetWriter.WriterGroupId));
            }
            await AddRemoveWriterFromWriterGroupTwinAsync(
                WriterGroupRegistrationEx.ToDeviceId(writerGroupId), dataSetWriter.DataSetWriterId);
        }

        /// <inheritdoc/>
        public async Task OnDataSetWriterUpdatedAsync(PublisherOperationContextModel context,
            string dataSetWriterId, DataSetWriterInfoModel dataSetWriter) {
            var writerGroupId = dataSetWriter?.WriterGroupId;
            if (string.IsNullOrEmpty(writerGroupId)) {
                //
                // The variable and event updates do not carry a full model but we can quickly
                // get the missing information by finding twins where this dataset is defined
                // - should only be one -
                // and patch them
                //
                var twins = await _iothub.QueryAllDeviceTwinsAsync(
                    $"SELECT * FROM devices WHERE " +
                    $"IS_DEFINED(properties.desired.{IdentityType.DataSet}_{dataSetWriterId}) AND " +
                    $"tags.{nameof(EntityRegistration.DeviceType)} = '{IdentityType.WriterGroup}' ");
                foreach (var twin in twins) {
                    await AddRemoveWriterFromWriterGroupTwinAsync(twin.Id, dataSetWriterId);
                }
                return;
            }
            await AddRemoveWriterFromWriterGroupTwinAsync(
                WriterGroupRegistrationEx.ToDeviceId(writerGroupId), dataSetWriterId);
        }

        /// <inheritdoc/>
        public async Task OnDataSetWriterRemovedAsync(PublisherOperationContextModel context,
            DataSetWriterInfoModel dataSetWriter) {
            if (string.IsNullOrEmpty(dataSetWriter?.WriterGroupId)) {
                // Should not happen
                throw new ArgumentNullException(nameof(dataSetWriter.WriterGroupId));
            }
            await AddRemoveWriterFromWriterGroupTwinAsync(
                WriterGroupRegistrationEx.ToDeviceId(dataSetWriter.WriterGroupId),
                dataSetWriter.DataSetWriterId, true);
        }

        /// <inheritdoc/>
        public async Task OnWriterGroupRemovedAsync(PublisherOperationContextModel context,
            string writerGroupId) {
            try {
                // Force delete
                await _iothub.DeleteAsync(WriterGroupRegistrationEx.ToDeviceId(writerGroupId));
            }
            catch (Exception ex) {
                // Retry create/update
                _logger.Error(ex, "Deleting writerGroup failed...");
            }
        }

        /// <summary>
        /// Add or remove the writer from the group
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="dataSetWriterId"></param>
        /// <param name="remove"></param>
        /// <returns></returns>
        private async Task AddRemoveWriterFromWriterGroupTwinAsync(string deviceId,
            string dataSetWriterId, bool remove = false) {
            try {
                await _iothub.PatchAsync(new DeviceTwinModel {
                    Id = deviceId,
                    Properties = new TwinPropertiesModel {
                        Desired = new Dictionary<string, VariantValue> {
                            [IdentityType.DataSet + "_" + dataSetWriterId] =
                        remove ? null : _serializer.FromObject(
                            new {
                                DataSetWriterId = dataSetWriterId,
                                // This will cause an update
                                Tag = Guid.NewGuid().ToString(),
                            })
                        }
                    }
                });
            }
            catch (Exception ex) {
                // Retry create/update
                _logger.Error(ex, "Updating writer table in writerGroup failed...");
            }
        }

        private readonly IIoTHubTwinServices _iothub;
        private readonly IJsonSerializer _serializer;
        private readonly IRegistryEventBroker<IPublisherRegistryListener> _broker;
        private readonly ILogger _logger;
    }
}
