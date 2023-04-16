// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Xml;
using Google.Apis.Util;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using YouRata.Common.Configuration.YouTube;
using YouRata.Common.Milestone;
using YouRata.Common.YouTube;
using YouRata.YouTubeSync.ConflictMonitor;
using static Google.Apis.YouTube.v3.SearchResource.ListRequest;
using static YouRata.Common.Proto.MilestoneActionIntelligence.Types;

namespace YouRata.YouTubeSync.YouTube;

/// <summary>
/// YouTube video action helper class
/// </summary>
internal static class YouTubeVideoHelper
{
    /// <summary>
    /// Get a list of videos published before the first outstanding video time
    /// </summary>
    /// <param name="config"></param>
    /// <param name="excludeVideos"></param>
    /// <param name="intelligence"></param>
    /// <param name="service"></param>
    /// <param name="client"></param>
    /// <returns></returns>
    public static List<Video> GetOutstandingChannelVideos(YouTubeConfiguration config, List<ResourceId> excludeVideos,
        YouTubeSyncActionIntelligence intelligence, YouTubeService service, YouTubeSyncCommunicationClient client)
    {
        long firstOutstandingPublishTime = (intelligence.OutstandingVideoPublishTime - 1);
        long? lastPublishTime;
        List<Video> outstandingChannelVideos = GetChannelVideos(config, firstOutstandingPublishTime, null, out lastPublishTime,
            excludeVideos, intelligence, service, client);
        if (lastPublishTime.HasValue)
        {
            // Save the last publish time to the milestone intelligence
            intelligence.OutstandingVideoPublishTime = lastPublishTime.Value;
        }
        else
        {
            // No outstanding videos exist
            intelligence.OutstandingVideoPublishTime = 0;
        }

        return outstandingChannelVideos;
    }

    /// <summary>
    /// Get a list of videos published after the first video time
    /// </summary>
    /// <param name="config"></param>
    /// <param name="excludeVideos"></param>
    /// <param name="intelligence"></param>
    /// <param name="service"></param>
    /// <param name="client"></param>
    /// <returns></returns>
    public static List<Video> GetRecentChannelVideos(YouTubeConfiguration config, List<ResourceId> excludeVideos,
        YouTubeSyncActionIntelligence intelligence, YouTubeService service, YouTubeSyncCommunicationClient client)
    {
        long firstRecentPublishTime = intelligence.FirstVideoPublishTime;
        long lastRecentPublishTime;
        List<Video> recentChannelVideos = GetChannelVideos(config, null, firstRecentPublishTime, out long? lastPublishTime, excludeVideos,
            intelligence, service, client);
        if (lastPublishTime.HasValue)
        {
            // Save in case this is the first run
            lastRecentPublishTime = lastPublishTime.Value;
        }
        else
        {
            // No outstanding videos exist
            lastRecentPublishTime = 0;
        }

        // Determine if this is the first run
        if (firstRecentPublishTime == 0 && recentChannelVideos.Count > 0)
        {
            // Find the first videos publish time
            DateTime? firstPublishDateTime = recentChannelVideos.First().Snippet.PublishedAt;
            if (firstPublishDateTime.HasValue)
            {
                DateTimeOffset publishTimeOffset = new DateTimeOffset(firstPublishDateTime.Value);
                firstRecentPublishTime = publishTimeOffset.ToUnixTimeSeconds();
                // The next run will pull videos after the last recent publish time
                intelligence.OutstandingVideoPublishTime = lastRecentPublishTime;
            }
        }

        // Save the first video publish time to the milestone intelligence
        intelligence.FirstVideoPublishTime = firstRecentPublishTime;
        return recentChannelVideos;
    }

    public static VideoDurationEnum GetVideoDurationFromConfig(YouTubeConfiguration config)
    {
        switch (config.VideoDurationFilter)
        {
            case "any":
                // Do not filter video search results based on their duration
                return VideoDurationEnum.Any;
            case "long":
                // Only include videos longer than 20 minutes
                return VideoDurationEnum.Long__;
            case "medium":
                // Only include videos that are between four and 20 minutes long (inclusive)
                return VideoDurationEnum.Medium;
            case "short":
                // Only include videos that are less than four minutes long
                return VideoDurationEnum.Short__;
        }

        return VideoDurationEnum.Any;
    }

    /// <summary>
    /// Update a video snippet description
    /// </summary>
    /// <param name="video"></param>
    /// <param name="description"></param>
    /// <param name="intelligence"></param>
    /// <param name="service"></param>
    /// <param name="client"></param>
    /// <exception cref="MilestoneException"></exception>
    public static void UpdateVideoDescription(Video video, string description, YouTubeSyncActionIntelligence intelligence,
        YouTubeService service, YouTubeSyncCommunicationClient client)
    {
        if (!YouTubeQuotaHelper.HasRemainingCalls(intelligence)) throw new MilestoneException("YouTube API rate limit exceeded");
        if (video.Snippet == null) return;
        foreach (PropertyInfo videoProperty in video.GetType().GetProperties())
        {
            if (videoProperty.CanWrite && videoProperty.PropertyType.IsClass)
            {
                // Remove every part from the video instance except id and snippet
                if (!videoProperty.Name.Equals("Id") && !videoProperty.Name.Equals("Snippet"))
                {
                    videoProperty.SetValue(video, null);
                }
            }
        }

        video.Snippet.Description = description;
        VideosResource.UpdateRequest videoUpdateRequest =
            new VideosResource.UpdateRequest(service, video, new[] { YouTubeConstants.RequestSnippetPart });
        // Construct a VideoUpdate action
        Action videoUpdate = (() => { videoUpdateRequest.Execute(); });
        YouTubeRetryHelper.RetryCommand(intelligence, YouTubeConstants.VideosResourceUpdateQuotaCost, videoUpdate, TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10), client.LogMessage, HttpStatusCode.Forbidden, out bool forbidden);
        if (forbidden) throw new MilestoneException("YouTube API update video forbidden");
    }

    /// <summary>
    /// Get all videos for a channel published between two times
    /// </summary>
    /// <param name="config"></param>
    /// <param name="publishedBefore"></param>
    /// <param name="publishedAfter"></param>
    /// <param name="lastPublishTime"></param>
    /// <param name="excludeVideos"></param>
    /// <param name="intelligence"></param>
    /// <param name="service"></param>
    /// <param name="client"></param>
    /// <returns></returns>
    /// <exception cref="MilestoneException"></exception>
    internal static List<Video> GetChannelVideos(YouTubeConfiguration config, long? publishedBefore, long? publishedAfter,
        out long? lastPublishTime, List<ResourceId> excludeVideos, YouTubeSyncActionIntelligence intelligence, YouTubeService service,
        YouTubeSyncCommunicationClient client)
    {
        List<Video> channelVideos = new List<Video>();
        lastPublishTime = null;
        if (!YouTubeQuotaHelper.HasRemainingCalls(intelligence)) throw new MilestoneException("YouTube API rate limit exceeded");
        SearchResource.ListRequest searchRequest = new SearchResource.ListRequest(service, new[] { YouTubeConstants.RequestSnippetPart });
        if (string.IsNullOrEmpty(config.ChannelId)) return channelVideos;
        // Build the list request
        searchRequest.ChannelId = config.ChannelId;
        // YouTube Data API only allows 50 items per page
        searchRequest.MaxResults = 50;
        searchRequest.VideoDuration = GetVideoDurationFromConfig(config);
        searchRequest.Type = YouTubeConstants.VideoType;
        searchRequest.Order = OrderEnum.Date;
        if (publishedBefore != null)
        {
            searchRequest.PublishedBefore =
                Utilities.GetStringFromDateTime(DateTimeOffset.FromUnixTimeSeconds(publishedBefore.Value).DateTime);
        }

        if (publishedAfter != null)
        {
            searchRequest.PublishedAfter =
                Utilities.GetStringFromDateTime(DateTimeOffset.FromUnixTimeSeconds(publishedAfter.Value).DateTime);
        }

        bool requestNextPage = true;
        while (requestNextPage)
        {
            // Construct a GetSearchResponse function
            Func<SearchListResponse> getSearchResponse = (() => { return searchRequest.Execute(); });
            SearchListResponse? searchResponse = YouTubeRetryHelper.RetryCommand(intelligence, YouTubeConstants.SearchListQuotaCost,
                getSearchResponse, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), client.LogMessage);
            client.Keepalive();
            if (searchResponse == null)
                throw new MilestoneException($"Could not get YouTube video list for channel id {searchRequest.ChannelId}");
            foreach (SearchResult searchResult in searchResponse.Items)
            {
                if (!YouTubeQuotaHelper.HasEnoughCallsForUpdate(intelligence, channelVideos)) return channelVideos;
                // Skip results without a publish time
                if (searchResult.Snippet.PublishedAt == null) continue;
                // Skip resources that are not a video
                if (searchResult.Id.Kind != YouTubeConstants.VideoKind) continue;
                // Skip all configured excluded videos
                if (config.ExcludeVideos?.Contains(searchResult.Id.VideoId) == true)
                {
                    intelligence.VideosSkipped++;
                    continue;
                }
                VideosResource.ListRequest videoRequest = new VideosResource.ListRequest(service,
                    new[] { YouTubeConstants.RequestContentDetailsPart, YouTubeConstants.RequestSnippetPart });
                // Build the video request
                videoRequest.Id = searchResult.Id.VideoId;
                videoRequest.MaxResults = 1;
                // Construct a GetVideoResponse function
                Func<VideoListResponse> getVideoResponse = (() => { return videoRequest.Execute(); });
                VideoListResponse? videoResponse = YouTubeRetryHelper.RetryCommand(intelligence, YouTubeConstants.VideoListQuotaCost,
                    getVideoResponse, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), client.LogMessage);
                client.Keepalive();
                if (videoResponse == null) throw new MilestoneException($"Could not get YouTube video {videoRequest.Id}");
                Video videoDetails = videoResponse.Items.First();
                // Skip all excluded videos
                if (excludeVideos != null && excludeVideos.Find(resourceId => resourceId.VideoId == videoDetails.Id) != null)
                {
                    intelligence.VideosSkipped++;
                    continue;
                }

                TimeSpan contentDuration = XmlConvert.ToTimeSpan(videoDetails.ContentDetails.Duration);
                // Skip videos shorter than the minimum time
                if (contentDuration.TotalSeconds < config.MinVideoSeconds)
                {
                    intelligence.VideosSkipped++;
                    continue;
                }

                channelVideos.Add(videoDetails);
                // Update the last publish time
                DateTimeOffset publishTimeOffset = new DateTimeOffset(searchResult.Snippet.PublishedAt.Value);
                lastPublishTime = publishTimeOffset.ToUnixTimeSeconds();
            }

            requestNextPage = !string.IsNullOrEmpty(searchResponse.NextPageToken);
            searchRequest.PageToken = searchResponse.NextPageToken;
        }

        return channelVideos;
    }
}
