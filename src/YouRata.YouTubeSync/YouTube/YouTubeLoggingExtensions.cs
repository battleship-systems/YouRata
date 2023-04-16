// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using YouRata.YouTubeSync.ConflictMonitor;
using static YouRata.Common.Proto.MilestoneActionIntelligence.Types;

namespace YouRata.YouTubeSync.YouTube;

/// <summary>
/// Extension methods used to log the number of YouTube Data API queries
/// </summary>
internal static class YouTubeLoggingExtensions
{
    internal static void LogAPIQueries(this YouTubeSyncCommunicationClient client, YouTubeSyncActionIntelligence intelligence)
    {
        YouTubeSyncActionIntelligence? milestoneActionIntelligence = client.GetMilestoneActionIntelligence();
        if (milestoneActionIntelligence == null) return;
        milestoneActionIntelligence.CalculatedQueriesPerDayRemaining = intelligence.CalculatedQueriesPerDayRemaining;
        milestoneActionIntelligence.LastQueryTime = DateTimeOffset.Now.ToUnixTimeSeconds();
        client.SetMilestoneActionIntelligence(milestoneActionIntelligence);
    }
}
