// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using YouRata.Common.YouTube;

namespace YouRata.ConflictMonitor.Workflow;

/// <summary>
/// Used to manipulate workflow variables before any milestone runs
/// </summary>
internal static class WorkflowLogicProvider
{
    internal static void ProcessWorkflow(ConflictMonitorWorkflow workflow)
    {
        if (YouTubeAuthHelper.GetTokenResponse(workflow.StoredTokenResponse, out _))
        {
            workflow.InitialSetupComplete = true;
        }
    }
}
