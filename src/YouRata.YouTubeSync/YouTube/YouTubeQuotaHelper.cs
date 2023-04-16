// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Google.Apis.YouTube.v3.Data;
using Google.Protobuf;
using YouRata.Common;
using YouRata.Common.ActionReport;
using YouRata.Common.Configuration.YouTube;
using YouRata.Common.YouTube;
using YouRata.YouTubeSync.ConflictMonitor;
using static YouRata.Common.Proto.MilestoneActionIntelligence.Types;

namespace YouRata.YouTubeSync.YouTube;

/// <summary>
/// YouTube Data API playlist quota estimate helper class
/// </summary>
internal static class YouTubeQuotaHelper
{
    /// <summary>
    /// Estimate if there is enough calls left to update a list of videos
    /// </summary>
    /// <param name="intelligence"></param>
    /// <param name="currentVideos"></param>
    /// <returns>EnoughCalls</returns>
    public static bool HasEnoughCallsForUpdate(YouTubeSyncActionIntelligence intelligence, List<Video> currentVideos)
    {
        // Calculate the cost to update all the videos
        int estimatedCostForUpdate = ((currentVideos.Count + 1) * YouTubeConstants.VideosResourceUpdateQuotaCost);
        // Add the cost for a video list
        estimatedCostForUpdate += YouTubeConstants.VideoListQuotaCost;
        if (estimatedCostForUpdate > intelligence.CalculatedQueriesPerDayRemaining)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Check for sufficient remaining API calls
    /// </summary>
    /// <param name="intelligence"></param>
    /// <returns></returns>
    public static bool HasRemainingCalls(YouTubeSyncActionIntelligence intelligence)
    {
        if (intelligence.CalculatedQueriesPerDayRemaining is > 0 and < 100)
        {
            Console.WriteLine($"WARNING: Only {intelligence.CalculatedQueriesPerDayRemaining} YouTube API calls remaining");
        }

        if (intelligence.CalculatedQueriesPerDayRemaining < 4)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Populate the current YouTubeSyncActionIntelligence with the saved action report values
    /// </summary>
    /// <param name="config"></param>
    /// <param name="client"></param>
    /// <param name="intelligence"></param>
    /// <param name="previousActionReport"></param>
    public static void SetPreviousActionReport(YouTubeConfiguration config, YouTubeSyncCommunicationClient client,
        YouTubeSyncActionIntelligence intelligence, ActionReportLayout previousActionReport)
    {
        if (string.IsNullOrEmpty(previousActionReport.YouTubeSyncIntelligence))
        {
            // Start with a full quota
            intelligence.CalculatedQueriesPerDayRemaining = config.QueriesPerDay;
        }
        else
        {
            // Get the previous intelligence
            YouTubeSyncActionIntelligence previousIntelligence =
                JsonParser.Default.Parse<YouTubeSyncActionIntelligence>(previousActionReport.YouTubeSyncIntelligence);
            TimeZoneInfo resetTimeZone = TimeZoneInfo.FindSystemTimeZoneById(YouRataConstants.PacificTimeZone);
            long resetDateSeconds = ((DateTimeOffset)DateTime.UtcNow.Add(resetTimeZone.BaseUtcOffset).Date).ToUnixTimeSeconds();
            DateTime resetDateTime = DateTimeOffset.FromUnixTimeSeconds(resetDateSeconds).Subtract(resetTimeZone.BaseUtcOffset).DateTime;
            long resetTime = ((DateTimeOffset)resetDateTime).ToUnixTimeSeconds();
            // Determine if the last query was before midnight Pacific Time
            if (previousIntelligence.LastQueryTime == 0 || resetTime > previousIntelligence.LastQueryTime)
            {
                // Daily quota has reset
                intelligence.CalculatedQueriesPerDayRemaining = config.QueriesPerDay;
            }
            else
            {
                intelligence.CalculatedQueriesPerDayRemaining = previousIntelligence.CalculatedQueriesPerDayRemaining;
            }

            // Copy the remaining values
            intelligence.FirstVideoPublishTime = previousIntelligence.FirstVideoPublishTime;
            intelligence.OutstandingVideoPublishTime = previousIntelligence.OutstandingVideoPublishTime;
            intelligence.HasOutstandingVideos = previousIntelligence.HasOutstandingVideos;
            intelligence.LastQueryTime = previousIntelligence.LastQueryTime;
        }

        client.SetMilestoneActionIntelligence(intelligence);
    }
}
