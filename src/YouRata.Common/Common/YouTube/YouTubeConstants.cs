// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using Octokit;

namespace YouRata.Common.YouTube;

/// <summary>
/// Constant values for the YouTube Data API
/// </summary>
public static class YouTubeConstants
{
    // Maximim length for a video description
    public const int MaxDescriptionLength = 5000;
    // YouTube Data API quota cost for PlaylistItems: list
    public const int PlaylistItemListQuotaCost = 1;
    // Private video status
    public const string PrivacyStatusPrivate = "private";
    // YouTube Data API user agent string
    public const string RequestApplicationName = "YouRata";
    // Request PlaylistItems: list part for content details
    public const string RequestContentDetailsPart = "contentDetails";
    // Request PlaylistItems: list part for snippet
    public const string RequestSnippetPart = "snippet";
    // Request PlaylistItems: list part for status
    public const string RequestStatusPart = "status";
    // YouTube Data API quota cost for Search: list
    public const int SearchListQuotaCost = 100;
    // Request Search: list part for video
    public const string VideoKind = "youtube#video";
    // YouTube Data API quota cost for Videos: list
    public const int VideoListQuotaCost = 1;
    // YouTube Data API quota cost for Videos: update
    public const int VideosResourceUpdateQuotaCost = 50;
    // Request Search: list type for video
    public const string VideoType = "video";
    // YouTube Data API http request timeout
    public static readonly TimeSpan RequestTimeout = TimeSpan.FromMilliseconds(1000 * 45);
}
