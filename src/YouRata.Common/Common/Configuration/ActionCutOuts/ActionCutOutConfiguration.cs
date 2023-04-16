// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;

namespace YouRata.Common.Configuration.ActionCutOuts;

/// <summary>
/// YouRata configuration for removing certain functionality from the action
/// </summary>
public class ActionCutOutConfiguration : BaseValidatableConfiguration
{
    // Disable the entire ActionReport milestone
    [Required]
    public bool DisableActionReportMilestone { get; set; }

    // Disable any API call from ConflictMonitor
    [Required]
    public bool DisableConflictMonitorGitHubOperations { get; set; }

    // Disable the entire InitialSetup milestone
    [Required]
    public bool DisableInitialSetupMilestone { get; set; }

    // Disable any API call not implemented in Octokit
    [Required]
    public bool DisableUnsupportedGitHubAPI { get; set; }

    // Disable the entire YouTubeSync milestone
    [Required]
    public bool DisableYouTubeSyncMilestone { get; set; }

    // Disable updating the YouTube video description
    [Required]
    public bool DisableYouTubeVideoUpdate { get; set; }
}
