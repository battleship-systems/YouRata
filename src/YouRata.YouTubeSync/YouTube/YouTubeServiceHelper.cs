// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using YouRata.Common.YouTube;
using YouRata.YouTubeSync.Workflow;

namespace YouRata.YouTubeSync.YouTube;

/// <summary>
/// YouTube Data API (REST) client helper class
/// </summary>
internal static class YouTubeServiceHelper
{
    public static YouTubeService GetService(YouTubeSyncWorkflow workflow, UserCredential userCred)
    {
        YouTubeService ytService = new YouTubeService(
            new BaseClientService.Initializer()
            {
                ApiKey = workflow.ProjectApiKey,
                ApplicationName = YouTubeConstants.RequestApplicationName,
                HttpClientInitializer = userCred
            });
        ytService.HttpClient.Timeout = YouTubeConstants.RequestTimeout;
        return ytService;
    }
}
