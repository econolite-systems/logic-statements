// SPDX-License-Identifier: MIT
// Copyright: 2023 Econolite Systems, Inc.

using System.Text.Json;
using Econolite.Dto.Repository.LogicStatement;
using Econolite.Ode.Messaging;
using Econolite.Ode.Messaging.Elements;
using Econolite.Ode.Models.LogicStatement;
using Econolite.Ode.Models.LogicStatement.Dto;
using Econolite.Ode.Monitoring.Events;
using Econolite.Ode.Monitoring.Events.Extensions;
using Econolite.Ode.Monitoring.Metrics;
using Econolite.Ode.Router.ActionSet.Messaging;
using Econolite.Ode.Service.LogicStatement;
using Econolite.Ode.Status.Common;
using Econolite.Ode.Status.Common.Messaging;

namespace Worker.ProcessActionSet;

public class ProcessStatusService : BackgroundService
{
    private readonly ISink<ActionRequest> _sink;
    private readonly MessageFactory<StatementActionStatus> _messageFactory;
    private readonly IConsumer<Guid, ActionEventStatus> _consumer;
    private readonly IActionSetStatusRepository _actionSetStatusRepository;
    private readonly IEnumerable<IActionEventStatusHandler> _actionEventStatusHandlers;
    private readonly IConfigServiceCall _configServiceCall;
    private readonly ILogger<ProcessStatusService> _logger;
    private readonly string _actionRouterTopic;
    private readonly string _actionSetTopic;
    private readonly IMetricsCounter _loopCounter;
    private readonly UserEventFactory _userEventFactory;


    public ProcessStatusService(ISink<ActionRequest> sink,
        MessageFactory<StatementActionStatus> messageFactory, IConsumer<Guid, ActionEventStatus> consumer,
        IActionSetStatusRepository actionSetStatusRepository, IEnumerable<IActionEventStatusHandler> actionEventStatusHandlers,
        IConfigServiceCall configServiceCall, IConfiguration configuration, ILogger<ProcessStatusService> logger,
        IMetricsFactory metricsFactory, UserEventFactory userEventFactory)
    {
        _sink = sink;
        _messageFactory = messageFactory;
        _consumer = consumer;
        _actionSetStatusRepository = actionSetStatusRepository;
        _actionEventStatusHandlers = actionEventStatusHandlers;
        _configServiceCall = configServiceCall;
        _logger = logger;
        _userEventFactory = userEventFactory;
        _actionSetTopic = configuration["Topics:ActionSetEvent"] ?? "actionset.event.status";
        _actionRouterTopic = configuration["Topics:ActionEventRouter"] ?? "actionset.event.router";
        _consumer.Subscribe(_actionSetTopic);

        _loopCounter = metricsFactory.GetMetricsCounter("Action Set");
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var result = _consumer.Consume(stoppingToken);
                        
                        // Handle the result
                        var actionEventStatus = result.ToObject<ActionEventStatus?>();
                        if (actionEventStatus is null) continue;
                        
                        // Find the handler for the action event
                        var actionEventHandler =
                            _actionEventStatusHandlers.FirstOrDefault(sink => sink.Type == result.Type);
                        if (actionEventHandler is null)
                        {
                            _logger.LogError("No handler found for action event type {Type}", result.Type);
                            continue;
                        }
                        // Get the event status from the handler
                        actionEventStatus = actionEventHandler.ToObject(result);
                        // DeviceId is for action set by device currently the ESS is the only device type
                        var deviceId = result.DeviceId ?? Guid.Empty;
                        // Get the source id for the event which is used for
                        var sourceId = actionEventHandler.GetSourceId(actionEventStatus);
                        if (deviceId == Guid.Empty && sourceId != Guid.Empty)
                        {
                            deviceId = sourceId;
                        }
                        // Get the location for the event from the handler
                        var (latitude, longitude) = await actionEventHandler.GetLocation(actionEventStatus,
                             GetDeviceLocation(stoppingToken), deviceId);
                        
                        // Get the action sets for the device and event type
                        var actionSetResult =
                            await _configServiceCall.GetActionSetsByEntityIdAndType(
                                deviceId,
                                actionEventStatus.ActionEventType,
                                stoppingToken);
                        
                        // Get the action set status for the source id by event id
                        var actionSetStatusResult = Array.Empty<ActionSetStatus>() as IEnumerable<ActionSetStatus>;
                        if (sourceId != Guid.Empty)
                        {
                            actionSetStatusResult = await _actionSetStatusRepository.GetAllByEventIdAsync(sourceId);
                        }
                        
                        var actionSets = actionSetResult.ToArray();
                        var actionSetStatuses = actionSetStatusResult.ToArray();

                        bool AddActionSetStatus(ActionSetStatus actionSetStatus)
                        {
                            if (actionSetStatus.EventId == Guid.Empty) return true;
                            _actionSetStatusRepository.Add(actionSetStatus);
                            return true;
                        }

                        bool UpdateActionSetStatus(ActionSetStatus actionSetStatus)
                        {
                            _actionSetStatusRepository.Update(actionSetStatus);
                            return true;
                        }
                        
                        try
                        {
                            var actionSetsUpdated = actionEventStatus.ToUpdatedActionSetStatuses(actionSets,
                                actionSetStatuses, actionEventHandler.HandleActionEventStatus, AddActionSetStatus,
                                UpdateActionSetStatus, sourceId, (latitude, longitude));

                            var (success, errors) = await _actionSetStatusRepository.DbContext.SaveChangesAsync();

                            if (!success && errors is not null)
                            {
                                _logger.LogError("Failed to save changes to database errors: {Error}", errors);
                            }

                            // Send actions
                            var actions = actionSetsUpdated.ActionsToSend(UpdateActionSetStatus).ToArray();
                            if (sourceId != Guid.Empty)
                            {
                                var (successFullySavedAction, errorSavingActions) = await _actionSetStatusRepository.DbContext.SaveChangesAsync();
                                if (!successFullySavedAction && errorSavingActions is not null)
                                {
                                    _logger.LogError("Failed to save changes to database errors: {Error}", errorSavingActions);
                                }
                            }
                            
                            if (actions.Length > 0)
                            {
                                foreach (var action in actions)
                                {
                                    var request = JsonSerializer.Deserialize<ActionRequest>(action.Action, JsonPayloadSerializerOptions.Options);
                                    if (request is null) continue;
                                    request.ActionSetId = action.ActionSetId;
                                    request.Latitude = action.Latitude;
                                    request.Longitude = action.Longitude;
                                    request.Cancel = action.EndTime.HasValue;
                                    request.Id = sourceId;
                                    await _sink.SinkAsync(sourceId, request, stoppingToken);
                                }

                                _logger.ExposeUserEvent(_userEventFactory.BuildUserEvent(EventLevel.Debug, string.Format("Sent {0} logic statement actions.", actions.Length)));
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Unhandled exception while processing status");
                        }

                        _loopCounter.Increment();
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Exception thrown while trying to consume status");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Stopping");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Processing loop stopped");
            }
        }, stoppingToken);
        return Task.CompletedTask;
    }

    private Func<Guid, Task<(double Latitude, double Logitude)>> GetDeviceLocation(CancellationToken stoppingToken)
    {
        return async (id) => await _configServiceCall.GetLocation(id, stoppingToken);
    }
}