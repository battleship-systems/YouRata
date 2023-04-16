// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using Octokit;
using YouRata.Common;
using YouRata.Common.Configuration;
using YouRata.Common.GitHub;
using YouRata.Common.Proto;
using YouRata.ConflictMonitor.ActionReport;
using YouRata.ConflictMonitor.MilestoneData;
using YouRata.ConflictMonitor.Workflow;
using static YouRata.Common.Proto.MilestoneActionIntelligence.Types;

namespace YouRata.ConflictMonitor.MilestoneCall;

/// <summary>
/// Handler for milestone calls from gRPC messages
/// </summary>
/// <remarks>
/// This object instance resides in ConflictMonitor as a singleton
/// </remarks>
internal class CallHandler
{
    // In-memory logs
    private readonly List<string> _logBuilder;

    internal CallHandler()
    {
        _logBuilder = new List<string>();
    }

    internal void AppendLog(string message)
    {
        lock (_logBuilder)
        {
            _logBuilder.Add(message);
        }
    }

    /// <summary>
    /// Serializes the YouRataConfiguration for transport over gRPC
    /// </summary>
    /// <param name="appConfig"></param>
    /// <returns></returns>
    internal string GetConfigJson(YouRataConfiguration appConfig)
    {
        return JsonConvert.SerializeObject(appConfig, Formatting.None);
    }

    /// <summary>
    /// Returns GitHubActionEnvironment from information retrieved from
    /// workflow environment variables and API calls
    /// </summary>
    /// <param name="appConfig"></param>
    /// <param name="environment"></param>
    /// <param name="workflow"></param>
    /// <returns></returns>
    internal GitHubActionEnvironment GetGithubActionEnvironment(YouRataConfiguration appConfig, GitHubEnvironment environment,
        ConflictMonitorWorkflow workflow)
    {
        // Populate the GitHubActionEnvironment from GitHubEnvironment
        GitHubActionEnvironment actionEnvironment = environment.GetActionEnvironment();
        if (!appConfig.ActionCutOuts.DisableConflictMonitorGitHubOperations && workflow.GitHubToken?.Length > 0 &&
            workflow.ApiToken?.Length > 0)
        {
            Func<ResourceRateLimit> getResourceRateLimit = (() =>
            {
                GitHubClient ghClient = new GitHubClient(GitHubConstants.ProductHeader)
                {
                    Credentials = new Credentials(workflow.GitHubToken, AuthenticationType.Bearer)
                };
                return ghClient.RateLimit.GetRateLimits().Result.Resources;
            });
            // Get the ResourceRateLimit from the API call
            ResourceRateLimit? ghRateLimit =
                GitHubRetryHelper.RetryCommand(actionEnvironment.OverrideRateLimit(), getResourceRateLimit, AppendLog);
            if (ghRateLimit != null)
            {
                actionEnvironment.RateLimitCoreRemaining = ghRateLimit.Core.Remaining;
                actionEnvironment.RateLimitCoreLimit = ghRateLimit.Core.Limit;
                actionEnvironment.RateLimitCoreReset = ghRateLimit.Core.Reset.ToUnixTimeSeconds();
            }

            actionEnvironment.GitHubToken = workflow.GitHubToken;
            actionEnvironment.ApiToken = workflow.ApiToken;
        }

        return actionEnvironment;
    }

    internal List<string> GetLogs()
    {
        lock (_logBuilder)
        {
            return _logBuilder;
        }
    }

    /// <summary>
    /// Returns MilestoneActionIntelligence for all milestones from MilestoneIntelligenceRegistry
    /// </summary>
    /// <param name="milestoneIntelligence"></param>
    /// <returns></returns>
    internal MilestoneActionIntelligence GetMilestoneActionIntelligence(MilestoneIntelligenceRegistry milestoneIntelligence)
    {
        // Populate the MilestoneActionIntelligence from the MilestoneIntelligenceRegistry instance variable
        MilestoneActionIntelligence actionIntelligence = new MilestoneActionIntelligence
        {
            InitialSetup =
                new InitialSetupActionIntelligence
                {
                    Condition = milestoneIntelligence.InitialSetup.Condition, ProcessId = milestoneIntelligence.InitialSetup.ProcessId
                },
            YouTubeSync = new YouTubeSyncActionIntelligence
            {
                Condition = milestoneIntelligence.YouTubeSync.Condition,
                ProcessId = milestoneIntelligence.YouTubeSync.ProcessId,
                VideosProcessed = milestoneIntelligence.YouTubeSync.VideosProcessed,
                VideosSkipped = milestoneIntelligence.YouTubeSync.VideosSkipped,
                CalculatedQueriesPerDayRemaining =
                    milestoneIntelligence.YouTubeSync.CalculatedQueriesPerDayRemaining,
                LastQueryTime = milestoneIntelligence.YouTubeSync.LastQueryTime,
                FirstVideoPublishTime = milestoneIntelligence.YouTubeSync.FirstVideoPublishTime,
                OutstandingVideoPublishTime = milestoneIntelligence.YouTubeSync.OutstandingVideoPublishTime,
                HasOutstandingVideos = milestoneIntelligence.YouTubeSync.HasOutstandingVideos
            },
            ActionReport = new ActionReportActionIntelligence
            {
                Condition = milestoneIntelligence.ActionReport.Condition, ProcessId = milestoneIntelligence.ActionReport.ProcessId
            }
        };

        return actionIntelligence;
    }

    /// <summary>
    /// Returns a string representation of the previous action report JSON
    /// </summary>
    /// <param name="appConfig"></param>
    /// <param name="actionReportProvider"></param>
    /// <returns></returns>
    internal string GetPreviousActionReport(YouRataConfiguration appConfig, PreviousActionReportProvider actionReportProvider)
    {
        return (actionReportProvider.IsMissing)
            ? string.Empty // Normal for initial setup
            : JsonConvert.SerializeObject(actionReportProvider.ActionReport, Formatting.None);
    }

    internal void KeepaliveActionReport(MilestoneIntelligenceRegistry milestoneIntelligence)
    {
        long updateTime = DateTimeOffset.Now.ToUnixTimeSeconds();
        milestoneIntelligence.ActionReport.LastUpdate = updateTime;
    }

    internal void KeepaliveInitialSetup(MilestoneIntelligenceRegistry milestoneIntelligence)
    {
        long updateTime = DateTimeOffset.Now.ToUnixTimeSeconds();
        milestoneIntelligence.InitialSetup.LastUpdate = updateTime;
    }

    internal void KeepaliveYouTubeSync(MilestoneIntelligenceRegistry milestoneIntelligence)
    {
        long updateTime = DateTimeOffset.Now.ToUnixTimeSeconds();
        milestoneIntelligence.YouTubeSync.LastUpdate = updateTime;
    }

    internal void LogMessage(string message)
    {
        StringBuilder lineBuilder = new StringBuilder();
        lineBuilder.Append("[");
        lineBuilder.Append(DateTime.Now.ToString(YouRataConstants.ZuluTimeFormat, CultureInfo.InvariantCulture));
        lineBuilder.Append("] ");
        lineBuilder.Append("[Message] ");
        lineBuilder.Append(message);
        AppendLog(lineBuilder.ToString());
    }

    /// <summary>
    /// Saves ActionReportActionIntelligence data to the MilestoneIntelligenceRegistry object instance
    /// </summary>
    /// <param name="milestoneIntelligence"></param>
    /// <param name="actionIntelligence"></param>
    internal void UpdateActionReportMilestoneIntelligence(MilestoneIntelligenceRegistry milestoneIntelligence,
        ActionReportActionIntelligence actionIntelligence)
    {
        long updateTime = DateTimeOffset.Now.ToUnixTimeSeconds();
        milestoneIntelligence.ActionReport.Condition = actionIntelligence.Condition;
        milestoneIntelligence.ActionReport.ProcessId = actionIntelligence.ProcessId;
        if (milestoneIntelligence.ActionReport.StartTime == 0)
        {
            milestoneIntelligence.ActionReport.StartTime = updateTime;
        }

        milestoneIntelligence.ActionReport.LastUpdate = updateTime;
    }

    /// <summary>
    /// Saves InitialSetupActionIntelligence data to the MilestoneIntelligenceRegistry object instance
    /// </summary>
    /// <param name="milestoneIntelligence"></param>
    /// <param name="actionIntelligence"></param>
    internal void UpdateInitialSetupActionIntelligence(MilestoneIntelligenceRegistry milestoneIntelligence,
        InitialSetupActionIntelligence actionIntelligence)
    {
        long updateTime = DateTimeOffset.Now.ToUnixTimeSeconds();
        milestoneIntelligence.InitialSetup.Condition = actionIntelligence.Condition;
        milestoneIntelligence.InitialSetup.ProcessId = actionIntelligence.ProcessId;
        if (milestoneIntelligence.InitialSetup.StartTime == 0)
        {
            milestoneIntelligence.InitialSetup.StartTime = updateTime;
        }

        milestoneIntelligence.InitialSetup.LastUpdate = updateTime;
    }

    /// <summary>
    /// Saves YouTubeSyncActionIntelligence data to the MilestoneIntelligenceRegistry object instance
    /// </summary>
    /// <param name="milestoneIntelligence"></param>
    /// <param name="actionIntelligence"></param>
    internal void UpdateYouTubeSyncActionIntelligence(MilestoneIntelligenceRegistry milestoneIntelligence,
        YouTubeSyncActionIntelligence actionIntelligence)
    {
        long updateTime = DateTimeOffset.Now.ToUnixTimeSeconds();
        milestoneIntelligence.YouTubeSync.Condition = actionIntelligence.Condition;
        milestoneIntelligence.YouTubeSync.ProcessId = actionIntelligence.ProcessId;
        if (milestoneIntelligence.YouTubeSync.StartTime == 0)
        {
            milestoneIntelligence.YouTubeSync.StartTime = updateTime;
        }

        milestoneIntelligence.YouTubeSync.LastUpdate = updateTime;
        milestoneIntelligence.YouTubeSync.VideosProcessed = actionIntelligence.VideosProcessed;
        milestoneIntelligence.YouTubeSync.VideosSkipped = actionIntelligence.VideosSkipped;
        milestoneIntelligence.YouTubeSync.CalculatedQueriesPerDayRemaining = actionIntelligence.CalculatedQueriesPerDayRemaining;
        milestoneIntelligence.YouTubeSync.LastQueryTime = actionIntelligence.LastQueryTime;
        milestoneIntelligence.YouTubeSync.FirstVideoPublishTime = actionIntelligence.FirstVideoPublishTime;
        milestoneIntelligence.YouTubeSync.OutstandingVideoPublishTime = actionIntelligence.OutstandingVideoPublishTime;
        milestoneIntelligence.YouTubeSync.HasOutstandingVideos = actionIntelligence.HasOutstandingVideos;
    }
}
