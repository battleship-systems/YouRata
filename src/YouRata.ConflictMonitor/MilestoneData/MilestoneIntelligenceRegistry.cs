// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace YouRata.ConflictMonitor.MilestoneData;

/// <summary>
/// This holds the intelligence for all the milestones
/// </summary>
internal class MilestoneIntelligenceRegistry
{
    private readonly IReadOnlyCollection<BaseMilestoneIntelligence> _milestones;

    internal MilestoneIntelligenceRegistry()
    {
        InitialSetup = new InitialSetupMilestoneIntelligence();
        YouTubeSync = new YouTubeSyncMilestoneIntelligence();
        ActionReport = new ActionReportMilestoneIntelligence();
        _milestones = new List<BaseMilestoneIntelligence> { InitialSetup, YouTubeSync, ActionReport };
    }

    internal ActionReportMilestoneIntelligence ActionReport { get; set; }
    internal InitialSetupMilestoneIntelligence InitialSetup { get; set; }
    internal IReadOnlyCollection<BaseMilestoneIntelligence> Milestones => _milestones;
    internal YouTubeSyncMilestoneIntelligence YouTubeSync { get; set; }
}
