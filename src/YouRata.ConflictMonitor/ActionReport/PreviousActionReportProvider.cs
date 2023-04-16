// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using System.IO;
using Newtonsoft.Json;
using YouRata.Common;
using YouRata.Common.ActionReport;

namespace YouRata.ConflictMonitor.ActionReport;

/// <summary>
/// Searches for a previously committed action report and stores the ActionReportLayout
/// </summary>
internal class PreviousActionReportProvider
{
    private readonly ActionReportRoot _actionReportRoot;
    private readonly bool _missingActionReport;

    public PreviousActionReportProvider()
    {
        _missingActionReport = true;
        _actionReportRoot = new ActionReportRoot();
        // Determine the working directory for the checkout
        string? workspace = Environment.GetEnvironmentVariable(YouRataConstants.GitHubWorkspaceVariable);
        if (workspace == null) return;
        // Combining the workspace with the filename produces the full path
        string fileName = Path.Combine(workspace, YouRataConstants.ActionReportFileName);
        try
        {
            ActionReportRoot? deserializeActionReportRoot = JsonConvert.DeserializeObject<ActionReportRoot>(File.ReadAllText(fileName));
            if (deserializeActionReportRoot != null)
            {
                _actionReportRoot = deserializeActionReportRoot;
                _missingActionReport = false;
            }
        }
        catch
        {
            // This is normal during the initial setup
            Console.WriteLine("No previous action report found");
        }
    }

    public ActionReportLayout ActionReport => _actionReportRoot.ActionReport;

    public bool IsMissing => _missingActionReport;
}
