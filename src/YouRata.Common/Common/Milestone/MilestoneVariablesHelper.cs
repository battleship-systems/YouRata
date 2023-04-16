// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using YouRata.Common.Configuration;
using YouRata.Common.Proto;

namespace YouRata.Common.Milestone;

/// <summary>
/// Static methods for creating milestone variables
/// </summary>
public static class MilestoneVariablesHelper
{
    public static void CreateRuntimeVariables(MilestoneCommunicationClient client, out ActionIntelligence actionInt,
        out YouRataConfiguration config, out GitHubActionEnvironment actionEnvironment)
    {
        actionInt = client.GetActionIntelligence();
        config = client.GetYouRataConfiguration();
        actionEnvironment = actionInt.GitHubActionEnvironment;
    }
}
