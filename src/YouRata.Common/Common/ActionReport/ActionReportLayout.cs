// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using Newtonsoft.Json;

namespace YouRata.Common.ActionReport;

/// <summary>
/// Represents the definition of a YouRata action report
/// </summary>
[JsonObject(ItemRequired = Required.Always, Title = "ActionReport")]
public class ActionReportLayout
{
    public ActionReportLayout()
    {
        Status = string.Empty;
        InitialSetupIntelligence = string.Empty;
        YouTubeSyncIntelligence = string.Empty;
        ActionReportIntelligence = string.Empty;
        Logs = string.Empty;
    }

    // Previous ActionReportActionIntelligence
    public string ActionReportIntelligence { get; set; }
    // Previous InitialSetupActionIntelligence
    public string InitialSetupIntelligence { get; set; }
    // Previous logs from CallHandler
    public string Logs { get; set; }
    // Text to display on the status badge
    public string Status { get; set; }
    // Previous YouTubeSyncActionIntelligence
    public string YouTubeSyncIntelligence { get; set; }
}
