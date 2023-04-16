// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using YouRata.Common;
using YouRata.Common.ActionReport;
using YouRata.Common.Configuration;
using YouRata.Common.GitHub;
using YouRata.Common.Milestone;
using YouRata.Common.Proto;
using YouRata.Common.YouTube;
using YouRata.YouTubeSync.ConflictMonitor;
using YouRata.YouTubeSync.ErrataBulletin;
using YouRata.YouTubeSync.Workflow;
using YouRata.YouTubeSync.YouTube;
using static YouRata.Common.Proto.MilestoneActionIntelligence.Types;

/// ---------------------------------------------------------------------------------------------
/// The YouTubeSync milestone makes a video errata bulletin for each video in a YouTube channel.
/// Each YouTube video description is updated to add a link to the errata bulletin on GitHub.
/// Control is started from the Run YouRata action in the event the TOKEN_RESPONSE environment
/// variable contains a valid token response. Channels with extensive video history will require
/// multiple days/runs to create all errata bulletins.
/// ---------------------------------------------------------------------------------------------

using (YouTubeSyncCommunicationClient client = new YouTubeSyncCommunicationClient())
{
    // Notify ConflictMonitor that the YouTubeSync milestone is starting
    if (!client.Activate(out YouTubeSyncActionIntelligence milestoneInt)) return;
    // Stop if YouTubeSync is disabled
    if (client.GetYouRataConfiguration().ActionCutOuts.DisableYouTubeSyncMilestone) return;
    // Fill runtime variables
    MilestoneVariablesHelper.CreateRuntimeVariables(client, out ActionIntelligence actionInt, out YouRataConfiguration config,
        out GitHubActionEnvironment actionEnvironment);
    // Get workflow variables
    YouTubeSyncWorkflow workflow = new YouTubeSyncWorkflow();
    if (!YouTubeAuthHelper.GetTokenResponse(workflow.StoredTokenResponse, out TokenResponse savedTokenResponse)) return;
    try
    {
        // TOKEN_RESPONSE is valid
        ActionReportLayout previousActionReport = client.GetPreviousActionReport();
        // Copy the old action report to our milestone intelligence
        YouTubeQuotaHelper.SetPreviousActionReport(config.YouTube, client, milestoneInt, previousActionReport);
        GoogleAuthorizationCodeFlow authFlow = YouTubeAuthHelper.GetFlow(workflow.ProjectClientId, workflow.ProjectClientSecret);
        if (savedTokenResponse.IsExpired(authFlow.Clock))
        {
            // Token has expired, refresh it
            savedTokenResponse = YouTubeAuthHelper.RefreshToken(authFlow, savedTokenResponse.RefreshToken, client.LogMessage);
            client.Keepalive();
            // Save the new token to TOKEN_RESPONSE
            YouTubeAuthHelper.SaveTokenResponse(savedTokenResponse, actionInt.GitHubActionEnvironment, client.LogMessage);
            client.Keepalive();
        }

        // Create credentials for the YouTube Data API
        UserCredential userCred = new UserCredential(authFlow, null, savedTokenResponse);
        List<string> processedVideos = new List<string>();
        using (YouTubeService ytService = YouTubeServiceHelper.GetService(workflow, userCred))
        {
            // Get the videos to ignore from the playlist(s)
            List<ResourceId> ignoreVideos = YouTubePlaylistHelper.GetPlaylistVideos(config.YouTube, milestoneInt, ytService, client);
            // Get recently published videos
            List<Video> videoList =
                YouTubeVideoHelper.GetRecentChannelVideos(config.YouTube, ignoreVideos, milestoneInt, ytService, client);
            List<Video> oustandingVideoList = new List<Video>();
            if (milestoneInt.HasOutstandingVideos)
            {
                // Get any older videos not picked up by the last run
                oustandingVideoList =
                    YouTubeVideoHelper.GetOutstandingChannelVideos(config.YouTube, ignoreVideos, milestoneInt, ytService, client);
                videoList.AddRange(oustandingVideoList
                    .Where(outVideo => videoList.FirstOrDefault(recentVideo => recentVideo.Id == outVideo.Id) == null).ToList());
            }

            foreach (Video video in videoList)
            {
                if (video.ContentDetails == null) continue;
                if (video.Snippet == null) continue;
                if (processedVideos.Contains(video.Id)) continue;
                // Build a path to the new errata bulletin
                string errataBulletinPath = $"{YouRataConstants.ErrataRootDirectory}" +
                                            $"{video.Id}.md";
                if (!Path.Exists(Path.Combine(workflow.Workspace, errataBulletinPath)))
                {
                    // Errata bulletin file does not exist in our checkout, build a new one
                    ErrataBulletinBuilder errataBulletinBuilder = ErrataBulletinFactory.CreateBuilder(video, config.ErrataBulletin);
                    // Add the file to the repository
                    if (GitHubAPIClient.CreateContentFile(actionEnvironment,
                            errataBulletinBuilder.SnippetTitle,
                            errataBulletinBuilder.Build(),
                            errataBulletinPath,
                            client.LogMessage))
                    {
                        client.Keepalive();
                        if (!config.ActionCutOuts.DisableYouTubeVideoUpdate)
                        {
                            // Create a link for YouTube visitors to access the errata page
                            string erattaLink = YouTubeDescriptionErattaPublisher.GetErrataLink(actionEnvironment, errataBulletinPath);
                            // Append the link to the description
                            string newDescription =
                                YouTubeDescriptionErattaPublisher.GetAmendedDescription(video.Snippet.Description, erattaLink,
                                    config.YouTube);
                            if (newDescription.Length <= YouTubeConstants.MaxDescriptionLength)
                            {
                                // Enough characters are left to update the description
                                YouTubeVideoHelper.UpdateVideoDescription(video, newDescription, milestoneInt, ytService, client);
                            }

                            client.Keepalive();
                        }
                    }

                    processedVideos.Add(video.Id);
                    milestoneInt.VideosProcessed++;
                }
            }

            // Save status to the milestone intelligence
            milestoneInt.HasOutstandingVideos = (oustandingVideoList.Count > 0 || milestoneInt.LastQueryTime == 0);
            milestoneInt.LastQueryTime = DateTimeOffset.Now.ToUnixTimeSeconds();
            client.SetMilestoneActionIntelligence(milestoneInt);
        }
    }
    catch (Exception ex)
    {
        client.LogAPIQueries(milestoneInt);
        client.SetStatus(MilestoneCondition.MilestoneFailed);
        throw new MilestoneException("YouTubeSync failed", ex);
    }

    client.SetStatus(MilestoneCondition.MilestoneCompleted);
}
