// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YouRata.Common.Configuration;
using YouRata.Common.GitHub;
using YouRata.Common.Proto;
using YouRata.ConflictMonitor.ActionReport;
using YouRata.ConflictMonitor.MilestoneData;
using YouRata.ConflictMonitor.Workflow;

namespace YouRata.ConflictMonitor.MilestoneCall.Messages;

/// <summary>
/// ActionIntelligenceMessage service impl
/// </summary>
internal class ActionIntelligenceMessage : ActionIntelligenceService.ActionIntelligenceServiceBase
{
    private readonly CallManager _callManager;
    private readonly IOptions<YouRataConfiguration> _configuration;
    private readonly ConflictMonitorWorkflow _conflictMonitorWorkflow;
    private readonly IOptions<GitHubEnvironment> _environment;
    private readonly ILogger<ActionIntelligenceMessage> _logger;
    private readonly MilestoneIntelligenceRegistry _milestoneIntelligence;
    private readonly PreviousActionReportProvider _previousActionReport;

    public ActionIntelligenceMessage(ILoggerFactory loggerFactory, CallManager callManager, IOptions<YouRataConfiguration> configuration,
        IOptions<GitHubEnvironment> environment, ConflictMonitorWorkflow conflictMonitorWorkflow,
        MilestoneIntelligenceRegistry milestoneIntelligence, PreviousActionReportProvider actionReportProvider)
    {
        _logger = loggerFactory.CreateLogger<ActionIntelligenceMessage>();
        _callManager = callManager;
        _configuration = configuration;
        _environment = environment;
        _conflictMonitorWorkflow = conflictMonitorWorkflow;
        _milestoneIntelligence = milestoneIntelligence;
        _previousActionReport = actionReportProvider;
    }

    public override Task<ActionIntelligence> GetActionIntelligence(Empty request, ServerCallContext context)
    {
        TaskCompletionSource<ActionIntelligence> actionIntelligenceResult = new TaskCompletionSource<ActionIntelligence>();
        _callManager.ActionCallbacks.Enqueue((CallHandler callHandler) =>
        {
            // Create a blank ActionIntelligence
            ActionIntelligence actionIntelligence = new ActionIntelligence();
            try
            {
                // Fill the ActionIntelligence with instance variables
                actionIntelligence.GitHubActionEnvironment =
                    callHandler.GetGithubActionEnvironment(_configuration.Value, _environment.Value, _conflictMonitorWorkflow);
                actionIntelligence.MilestoneIntelligence = callHandler.GetMilestoneActionIntelligence(_milestoneIntelligence);
                actionIntelligence.ConfigJson = callHandler.GetConfigJson(_configuration.Value);
                actionIntelligence.PreviousActionReport = callHandler.GetPreviousActionReport(_configuration.Value, _previousActionReport);
            }
            catch (Exception e)
            {
                _logger.LogError($"Error on GetActionIntelligence: {e.Message}");
            }

            actionIntelligenceResult.SetResult(actionIntelligence);
        });
        // Let CallManager know it needs to run an action
        _callManager.ActionReady.Set();
        return actionIntelligenceResult.Task;
    }
}
