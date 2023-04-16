// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace YouRata.Common.Configuration.ErrataBulletin;

/// <summary>
/// YouRata configuration for video errata markdown files
/// </summary>
public class ErrataBulletinConfiguration : BaseValidatableConfiguration
{
#pragma warning disable CS8618
    // The number of blank lines between each time marker
    [Required]
    [Range(1, 50)]
    [DefaultValue(3)]
    public int EmptyLinesPerMark { get; set; }

    // Include a link to the original video
    [Required]
    [DefaultValue(true)]
    public bool IncludeVideoLink { get; set; }

    // Instructions for submitting errata
    [Required]
    [DefaultValue("Enter your errata between the time slots  \nFor full instructions see [README](../../..#readme)")]
    public string Instructions { get; set; }

    // Seconds between each mark in the errata markdown file
    [Required]
    [Range(1, 50)]
    [DefaultValue(30)]
    public int SecondsPerMark { get; set; }

    // Where to place the title of the YouTube video
    [Required]
    [DefaultValue("top")]
    [RegularExpression(@"^(top|bottom)$", ErrorMessage = "ErrataBulletinConfiguration.TitleLocation must be top or bottom")]
    public string TitleLocation { get; set; }
#pragma warning restore CS8618
}
