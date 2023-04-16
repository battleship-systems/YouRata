// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using YouRata.Common.Milestone;
using static YouRata.Common.Proto.MilestoneActionIntelligence.Types;

namespace YouRata.ActionReport.ConflictMonitor;

/// <summary>
/// ActionReport specific implementation of MilestoneCommunicationClient
/// </summary>
internal class ActionReportCommunicationClient : MilestoneCommunicationClient
{
    private static readonly string _milestoneName = "ActionReport";
    private readonly Type _milestoneType = typeof(ActionReportActionIntelligence);

    public ActionReportCommunicationClient() : base()
    {
    }

    /// <summary>
    /// Signal ConflictMonitor that the milestone has started
    /// </summary>
    /// <param name="intelligence"></param>
    /// <returns>Success</returns>
    public bool Activate(out ActionReportActionIntelligence intelligence)
    {
        intelligence = new ActionReportActionIntelligence();
        if (base.IsBlocked(_milestoneType)) return false;
        intelligence = base.Activate<ActionReportActionIntelligence>(_milestoneType, _milestoneName);
        return true;
    }

    /// <summary>
    /// Get the milestone intelligence
    /// </summary>
    /// <returns></returns>
    public ActionReportActionIntelligence? GetMilestoneActionIntelligence()
    {
        ActionReportActionIntelligence? initialSetupActionIntelligence = null;
        object? milestoneActionIntelligence = base.GetMilestoneActionIntelligence(_milestoneType);
        if (milestoneActionIntelligence != null)
        {
            initialSetupActionIntelligence = (ActionReportActionIntelligence?)milestoneActionIntelligence;
        }

        return initialSetupActionIntelligence;
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
    public void SetMilestoneActionIntelligence(ActionReportActionIntelligence initialSetupActionIntelligence)
    {
        base.SetMilestoneActionIntelligence(initialSetupActionIntelligence, _milestoneType, _milestoneName);
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
