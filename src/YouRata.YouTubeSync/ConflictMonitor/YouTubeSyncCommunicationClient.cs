// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using YouRata.Common.Milestone;
using static YouRata.Common.Proto.MilestoneActionIntelligence.Types;

namespace YouRata.YouTubeSync.ConflictMonitor;

/// <summary>
/// YouTubeSync specific implementation of MilestoneCommunicationClient
/// </summary>
internal class YouTubeSyncCommunicationClient : MilestoneCommunicationClient
{
    private static readonly string _milestoneName = "YouTubeSync";
    private readonly Type _milestoneType = typeof(YouTubeSyncActionIntelligence);

    public YouTubeSyncCommunicationClient() : base()
    {
    }

    /// <summary>
    /// Signal ConflictMonitor that the milestone has started
    /// </summary>
    /// <param name="intelligence"></param>
    /// <returns>Success</returns>
    public bool Activate(out YouTubeSyncActionIntelligence intelligence)
    {
        intelligence = new YouTubeSyncActionIntelligence();
        if (base.IsBlocked(_milestoneType)) return false;
        intelligence = base.Activate<YouTubeSyncActionIntelligence>(_milestoneType, _milestoneName);
        return true;
    }

    /// <summary>
    /// Get the milestone intelligence
    /// </summary>
    /// <returns></returns>
    public YouTubeSyncActionIntelligence? GetMilestoneActionIntelligence()
    {
        YouTubeSyncActionIntelligence? youTubeSyncActionIntelligence = null;
        object? milestoneActionIntelligence = base.GetMilestoneActionIntelligence(_milestoneType);
        if (milestoneActionIntelligence != null)
        {
            youTubeSyncActionIntelligence = (YouTubeSyncActionIntelligence?)milestoneActionIntelligence;
        }

        return youTubeSyncActionIntelligence;
    }

    /// <summary>
    /// Signal ConflictMonitor that the milestone is alive
    /// </summary>
    public void Keepalive()
    {
        base.Keepalive(_milestoneType, _milestoneName);
    }

    /// <summary>
    /// Log a message to the in-memory log
    /// </summary>
    /// <param name="message"></param>
    public void LogMessage(string message)
    {
        base.LogMessage(message, _milestoneName);
    }

    /// <summary>
    /// Set the milestone intelligence
    /// </summary>
    /// <param name="initialSetupActionIntelligence"></param>
    public void SetMilestoneActionIntelligence(YouTubeSyncActionIntelligence youTubeSyncActionIntelligence)
    {
        base.SetMilestoneActionIntelligence(youTubeSyncActionIntelligence, _milestoneType, _milestoneName);
    }

    /// <summary>
    /// Set the milestone MilestoneCondition
    /// </summary>
    /// <param name="status"></param>
    public void SetStatus(MilestoneCondition status)
    {
        base.SetStatus(status, _milestoneType, _milestoneName);
    }
}
