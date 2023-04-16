// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using YouRata.ActionReport.ActionReportFile;
using YouRata.ActionReport.ConflictMonitor;
using YouRata.Common;
using YouRata.Common.Configuration;
using YouRata.Common.GitHub;
using YouRata.Common.Milestone;
using YouRata.Common.Proto;
using static YouRata.Common.Proto.MilestoneActionIntelligence.Types;

/// ---------------------------------------------------------------------------------------------
/// The ActionReport milestone saves all milestone intelligence to a file on the repository.
/// Control is started from the Run YouRata action in the event the TOKEN_RESPONSE environment
/// variable contains a valid token response. 
/// ---------------------------------------------------------------------------------------------

using (ActionReportCommunicationClient client = new ActionReportCommunicationClient())
{
    // Notify ConflictMonitor that the ActionReport milestone is starting
    if (!client.Activate(out ActionReportActionIntelligence milestoneInt)) return;
    // Stop if ActionReport is disabled
    if (client.GetYouRataConfiguration().ActionCutOuts.DisableActionReportMilestone) return;
    // Fill runtime variables
    MilestoneVariablesHelper.CreateRuntimeVariables(client, out ActionIntelligence actionInt, out YouRataConfiguration config,
        out GitHubActionEnvironment actionEnvironment);
    try
    {
        // Pull log messages from ConflictMonitor
        string logMessages = client.GetLogMessages();
        // Build a new action report
        ActionReportBuilder builder = new ActionReportBuilder(client.GetActionIntelligence(), logMessages);
        // Add the file to the repository
        GitHubAPIClient.UpdateContentFile(actionEnvironment.OverrideRateLimit(), YouRataConstants.ActionReportMessage, builder.Build(),
            YouRataConstants.ActionReportFileName, client.LogMessage);
        client.Keepalive();
    }
    catch (Exception ex)
    {
        client.SetStatus(MilestoneCondition.MilestoneFailed);
        throw new MilestoneException("ActionReport failed", ex);
    }

    client.SetStatus(MilestoneCondition.MilestoneCompleted);
}
