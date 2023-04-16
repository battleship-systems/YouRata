// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using YouRata.Common.Configuration.YouTube;
using YouRata.Common.Milestone;
using YouRata.Common.YouTube;
using YouRata.YouTubeSync.ConflictMonitor;
using static YouRata.Common.Proto.MilestoneActionIntelligence.Types;

namespace YouRata.YouTubeSync.YouTube;

/// <summary>
/// YouTube Data API playlist helper class
/// </summary>
internal static class YouTubePlaylistHelper
{
    /// <summary>
    /// Get a list of resource ID's from a YouTube playlist
    /// </summary>
    /// <param name="config"></param>
    /// <param name="intelligence"></param>
    /// <param name="service"></param>
    /// <param name="client"></param>
    /// <returns></returns>
    /// <exception cref="MilestoneException"></exception>
    public static List<ResourceId> GetPlaylistVideos(YouTubeConfiguration config, YouTubeSyncActionIntelligence intelligence,
        YouTubeService service, YouTubeSyncCommunicationClient client)
    {
        if (!YouTubeQuotaHelper.HasRemainingCalls(intelligence)) throw new MilestoneException("YouTube API rate limit exceeded");
        List<string> videoIds = new List<string>();
        List<ResourceId> playlistResourceIds = new List<ResourceId>();
        if (config.ExcludePlaylists == null) return playlistResourceIds;
        string[] playlists = config.ExcludePlaylists;
        if (playlists == null) return playlistResourceIds;
        foreach (string playlist in playlists)
        {
            if (string.IsNullOrEmpty(playlist)) continue;
            PlaylistItemsResource.ListRequest playlistRequest =
                new PlaylistItemsResource.ListRequest(service,
                    new[] { YouTubeConstants.RequestSnippetPart, YouTubeConstants.RequestStatusPart })
                {
                    // YouTube Data API only allows 50 items per page
                    PlaylistId = playlist, MaxResults = 50
                };
            bool requestNextPage = true;
            while (requestNextPage)
            {
                // Construct a GetPlaylistResponse function
                Func<PlaylistItemListResponse> getPlaylistResponse = (() => { return playlistRequest.Execute(); });
                PlaylistItemListResponse? playlistResponse = YouTubeRetryHelper.RetryCommand(intelligence,
                    YouTubeConstants.PlaylistItemListQuotaCost, getPlaylistResponse, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10),
                    client.LogMessage);
                client.Keepalive();
                if (playlistResponse == null) throw new MilestoneException("Could not get YouTube playlist items");
                foreach (PlaylistItem playlistItem in playlistResponse.Items)
                {
                    // Skip resources that are not a video
                    if (playlistItem.Snippet.ResourceId.Kind != YouTubeConstants.VideoKind) continue;
                    // Skip videos already retrieved
                    if (videoIds.Contains(playlistItem.Snippet.ResourceId.VideoId)) continue;
                    // Skip private videos
                    if (playlistItem.Status.PrivacyStatus == YouTubeConstants.PrivacyStatusPrivate) continue;
                    playlistResourceIds.Add(playlistItem.Snippet.ResourceId);
                    videoIds.Add(playlistItem.Snippet.ResourceId.VideoId);
                }

                requestNextPage = !string.IsNullOrEmpty(playlistResponse.NextPageToken);
                playlistRequest.PageToken = playlistResponse.NextPageToken;
            }
        }

        return playlistResourceIds;
    }
}
