// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace YouRata.Common.Configuration.YouTube;

/// <summary>
/// YouRata configuration for interacting with the YouTube Data API
/// </summary>
public class YouTubeConfiguration : BaseValidatableConfiguration
{
#pragma warning disable CS8618
    // The YouTube channel ID to search for videos
    [RegularExpression(@"^UC[-_0-9A-Za-z]{21}[AQgw]$", ErrorMessage = "Invalid YouTubeConfiguration.ChannelId")]
    public string ChannelId { get; set; }

    // Where to place the errata link in the video description
    [Required]
    [DefaultValue("bottom")]
    [RegularExpression(@"^(top|bottom)$", ErrorMessage = "YouTubeConfiguration.ErattaLinkLocation must be top or bottom")]
    public string ErattaLinkLocation { get; set; }

    // Errata link text template
    [Required]
    [DefaultValue("To see errata for this video: {0}")]
    public string ErrataLinkTemplate { get; set; }

    // Playlist IDs containing videos to exclude
    public string[]? ExcludePlaylists { get; set; }

    // Video IDs to exclude
    public string[]? ExcludeVideos { get; set; }

    // Minimum length of the video to include
    [Required]
    [DefaultValue(60)]
    public int MinVideoSeconds { get; set; }

    // YouTube Data API quota (PerDayPerProject)
    [Required]
    [DefaultValue(10000)]
    public int QueriesPerDay { get; set; }

    // Remove description text greater than maximum to insert errata link
    [Required]
    [DefaultValue(true)]
    public bool TruncateDescriptionOverflow { get; set; }

    // Video duration filter query
    [Required]
    [DefaultValue("any")]
    [RegularExpression(@"^(any|long|medium|short)$",
        ErrorMessage = "YouTubeConfiguration.VideoDurationFilter must be any, long, medium, or short")]
    public string VideoDurationFilter { get; set; }

#pragma warning restore CS8618
}
