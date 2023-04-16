// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using static YouRata.Common.Proto.MilestoneActionIntelligence.Types;

namespace YouRata.ConflictMonitor.MilestoneData;

/// <summary>
/// Provides a base class for all milestone intelligence
/// </summary>
internal abstract class BaseMilestoneIntelligence
{
    private protected BaseMilestoneIntelligence()
    {
        // All milesones start with MilestonePending condition
        Condition = MilestoneCondition.MilestonePending;
    }

    internal MilestoneCondition Condition { get; set; }

    // Unix epoch time of last update from the milestone
    internal long LastUpdate { get; set; }

    // Process ID of the running milestone
    internal int ProcessId { get; set; }

    // Unix epoch time the milestone was activated
    internal long StartTime { get; set; }
}
