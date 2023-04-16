// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace YouRata.Common.Configuration.MilestoneLifetime;

/// <summary>
/// YouRata configuration for finding inert process
/// </summary>
public class MilestoneLifetimeConfiguration : BaseValidatableConfiguration
{
    // Maximum time since started
    [Required]
    [Range(5, 9000)]
    [DefaultValue(3600)]
    public int MaxRunTime { get; set; }

    // Maximum time since last update
    [Required]
    [Range(5, 9000)]
    [DefaultValue(900)]
    public int MaxUpdateDwellTime { get; set; }
}
