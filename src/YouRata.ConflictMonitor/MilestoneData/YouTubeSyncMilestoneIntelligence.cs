// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;

namespace YouRata.ConflictMonitor.MilestoneData;

/// <summary>
/// ConflictMonitor abstraction of YouTubeSyncActionIntelligence
/// </summary>
internal class YouTubeSyncMilestoneIntelligence : BaseMilestoneIntelligence
{
    public YouTubeSyncMilestoneIntelligence()
    {
        VideosProcessed = 0;
        VideosSkipped = 0;
    }

    public int CalculatedQueriesPerDayRemaining { get; set; }
    public long FirstVideoPublishTime { get; set; }
    public bool HasOutstandingVideos { get; set; }
    public long LastQueryTime { get; set; }
    public long OutstandingVideoPublishTime { get; set; }
    public int VideosProcessed { get; set; }
    public int VideosSkipped { get; set; }
}
