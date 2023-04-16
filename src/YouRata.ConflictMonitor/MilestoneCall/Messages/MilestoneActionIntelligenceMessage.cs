// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using YouRata.Common.Proto;
using YouRata.ConflictMonitor.MilestoneData;
using static YouRata.Common.Proto.MilestoneActionIntelligence.Types;

namespace YouRata.ConflictMonitor.MilestoneCall.Messages;

/// <summary>
/// MilestoneActionIntelligenceMessage service impl
/// </summary>
internal class MilestoneActionIntelligenceMessage : MilestoneActionIntelligenceService.MilestoneActionIntelligenceServiceBase
{
    private readonly CallManager _callManager;
    private readonly ILogger<MilestoneActionIntelligenceMessage> _logger;
    private readonly MilestoneIntelligenceRegistry _milestoneIntelligence;

    public MilestoneActionIntelligenceMessage(ILoggerFactory loggerFactory, CallManager callManager,
        MilestoneIntelligenceRegistry milestoneIntelligence)
    {
        _logger = loggerFactory.CreateLogger<MilestoneActionIntelligenceMessage>();
        _callManager = callManager;
        _milestoneIntelligence = milestoneIntelligence;
    }

    public override Task<Empty> KeepaliveActionReport(Empty request, ServerCallContext context)
    {
        TaskCompletionSource<Empty> emptyResult = new TaskCompletionSource<Empty>();
        _callManager.ActionCallbacks.Enqueue((callHandler) =>
        {
            try
            {
                // Update ActionReport recent action timer
                callHandler.KeepaliveActionReport(_milestoneIntelligence);
            }
            catch (Exception e)
            {
                _logger.LogError($"Error on KeepaliveActionReport: {e.Message}");
            }

            emptyResult.SetResult(new Empty());
        });
        // Let CallManager know it needs to run an action
        _callManager.ActionReady.Set();
        return emptyResult.Task;
    }

    public override Task<Empty> KeepaliveInitialSetup(Empty request, ServerCallContext context)
    {
        TaskCompletionSource<Empty> emptyResult = new TaskCompletionSource<Empty>();
        _callManager.ActionCallbacks.Enqueue((callHandler) =>
        {
            try
            {
                // Update InitialSetup recent action timer
                callHandler.KeepaliveInitialSetup(_milestoneIntelligence);
            }
            catch (Exception e)
            {
                _logger.LogError($"Error on KeepaliveInitialSetup: {e.Message}");
            }

            emptyResult.SetResult(new Empty());
        });
        // Let CallManager know it needs to run an action
        _callManager.ActionReady.Set();
        return emptyResult.Task;
    }

    public override Task<Empty> KeepaliveYouTubeSync(Empty request, ServerCallContext context)
    {
        TaskCompletionSource<Empty> emptyResult = new TaskCompletionSource<Empty>();
        _callManager.ActionCallbacks.Enqueue((callHandler) =>
        {
            try
            {
                // Update YouTubeSync recent action timer
                callHandler.KeepaliveYouTubeSync(_milestoneIntelligence);
            }
            catch (Exception e)
            {
                _logger.LogError($"Error on KeepaliveYouTubeSync: {e.Message}");
            }

            emptyResult.SetResult(new Empty());
        });
        // Let CallManager know it needs to run an action
        _callManager.ActionReady.Set();
        return emptyResult.Task;
    }

    public override Task<Empty> UpdateActionReportActionIntelligence(ActionReportActionIntelligence request, ServerCallContext context)
    {
        TaskCompletionSource<Empty> emptyResult = new TaskCompletionSource<Empty>();
        _callManager.ActionCallbacks.Enqueue((CallHandler callHandler) =>
        {
            try
            {
                // Update ActionReport intelligence
                callHandler.UpdateActionReportMilestoneIntelligence(_milestoneIntelligence, request);
            }
            catch (Exception e)
            {
                _logger.LogError($"Error on UpdateActionReportActionIntelligence: {e.Message}");
            }

            emptyResult.SetResult(new Empty());
        });
        if (request.Condition == MilestoneCondition.MilestoneCompleted)
        {
            // ActionReport is the last milestone, stop ConflictMonitor
            _callManager.ActionStop.Set();
        }
        // Let CallManager know it needs to run an action
        _callManager.ActionReady.Set();
        return emptyResult.Task;
    }

    public override Task<Empty> UpdateInitialSetupActionIntelligence(InitialSetupActionIntelligence request, ServerCallContext context)
    {
        TaskCompletionSource<Empty> emptyResult = new TaskCompletionSource<Empty>();
        _callManager.ActionCallbacks.Enqueue((CallHandler callHandler) =>
        {
            try
            {
                // Update InitialSetup intelligence
                callHandler.UpdateInitialSetupActionIntelligence(_milestoneIntelligence, request);
            }
            catch (Exception e)
            {
                _logger.LogError($"Error on UpdateInitialSetupActionIntelligence: {e.Message}");
            }

            emptyResult.SetResult(new Empty());
        });
        // Let CallManager know it needs to run an action
        _callManager.ActionReady.Set();
        return emptyResult.Task;
    }

    public override Task<Empty> UpdateYouTubeSyncActionIntelligence(YouTubeSyncActionIntelligence request, ServerCallContext context)
    {
        TaskCompletionSource<Empty> emptyResult = new TaskCompletionSource<Empty>();
        _callManager.ActionCallbacks.Enqueue((CallHandler callHandler) =>
        {
            try
            {
                // Update YouTubeSync intelligence
                callHandler.UpdateYouTubeSyncActionIntelligence(_milestoneIntelligence, request);
            }
            catch (Exception e)
            {
                _logger.LogError($"Error on UpdateYouTubeSyncActionIntelligence: {e.Message}");
            }

            emptyResult.SetResult(new Empty());
        });
        // Let CallManager know it needs to run an action
        _callManager.ActionReady.Set();
        return emptyResult.Task;
    }
}
