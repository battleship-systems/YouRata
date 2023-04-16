// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using System.Globalization;
using Google.Protobuf;
using Newtonsoft.Json;
using YouRata.Common;
using YouRata.Common.ActionReport;
using YouRata.Common.Proto;
using static YouRata.Common.Proto.MilestoneActionIntelligence.Types;

namespace YouRata.ActionReport.ActionReportFile;

/// <summary>
/// Builds an action report JSON string from ActionIntelligence
/// </summary>
internal sealed class ActionReportBuilder
{
    private readonly ActionIntelligence _actionIntelligence;
    private readonly string _logMessages;

    internal ActionReportBuilder(ActionIntelligence actionIntelligence, string logMessages)
    {
        _actionIntelligence = actionIntelligence;
        _logMessages = logMessages;
    }

    public string Build()
    {
        string zuluTime = DateTime.Now.ToString(YouRataConstants.ZuluTimeFormat, CultureInfo.InvariantCulture);
        string status = "Unknown";
        JsonFormatter intelligenceFormatter = new JsonFormatter(JsonFormatter.Settings.Default.WithFormatDefaultValues(true));
        MilestoneActionIntelligence milestoneInt = _actionIntelligence.MilestoneIntelligence;
        if (milestoneInt.InitialSetup.Condition != MilestoneCondition.MilestoneFailed &&
            milestoneInt.InitialSetup.Condition != MilestoneCondition.MilestoneBlocked &&
            milestoneInt.YouTubeSync.Condition == MilestoneCondition.MilestoneCompleted)
        {
            // Success status
            status = $"Last Run {zuluTime}";
        }
        else if (milestoneInt.InitialSetup.Condition == MilestoneCondition.MilestoneFailed)
        {
            status = "Initial Setup Failed";
        }
        else if (milestoneInt.YouTubeSync.Condition == MilestoneCondition.MilestoneFailed)
        {
            status = "YouTube Sync Failed";
        }

        ActionReportLayout actionReport = new ActionReportLayout
        {
            Status = status,
            InitialSetupIntelligence = intelligenceFormatter.Format(milestoneInt.InitialSetup),
            YouTubeSyncIntelligence = intelligenceFormatter.Format(milestoneInt.YouTubeSync),
            ActionReportIntelligence = intelligenceFormatter.Format(milestoneInt.ActionReport),
            Logs = _logMessages
        };

        ActionReportRoot actionReportRoot = new ActionReportRoot { ActionReport = actionReport };

        return JsonConvert.SerializeObject(actionReportRoot, Formatting.Indented);
    }
}
