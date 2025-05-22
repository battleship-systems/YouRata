// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using System.Net;
using System.Text.RegularExpressions;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;
using YouRata.Common;
using YouRata.Common.Configuration;
using YouRata.Common.GitHub;
using YouRata.Common.Milestone;
using YouRata.Common.Proto;
using YouRata.Common.YouTube;
using YouRata.InitialSetup.ConflictMonitor;
using YouRata.InitialSetup.Workflow;
using static YouRata.Common.Proto.MilestoneActionIntelligence.Types;

/// ---------------------------------------------------------------------------------------------
/// The InitialSetup milestone verifies the existence of action secrets and variables.
/// Control is started from the Run YouRata action in the event TOKEN_RESPONSE is not a populated
/// environment variable. Step feedback is generated in the GITHUB_STEP_SUMMARY workflow
/// command and provides further instructions after each run. Finally the program allows the
/// workflow to copy errata contributing instructions to the repository root.
/// ---------------------------------------------------------------------------------------------

using (InitialSetupCommunicationClient client = new InitialSetupCommunicationClient())
{
    // Notify ConflictMonitor that the InitialSetup milestone is starting
    if (!client.Activate(out InitialSetupActionIntelligence milestoneInt)) return;
    // Stop if InitialSetup is disabled
    if (client.GetYouRataConfiguration().ActionCutOuts.DisableInitialSetupMilestone) return;
    // Fill runtime variables
    MilestoneVariablesHelper.CreateRuntimeVariables(client, out ActionIntelligence actionInt, out YouRataConfiguration config,
        out GitHubActionEnvironment actionEnvironment);
    // Get workflow variables
    InitialSetupWorkflow workflow = new InitialSetupWorkflow();
    try
    {
        bool canContinue = true;
        // Add summary ATX heading
        GitHubWorkflowHelper.WriteStepSummary("# InitialSetup\n\n");
        if (string.IsNullOrEmpty(actionEnvironment.ApiToken))
        {
            // GitHub personal access token is missing
            Console.WriteLine("Entering actions secrets section");
            Announce($"Create an action secret {YouRataConstants.ApiTokenVariable} to store GitHub personal access token");
            GitHubWorkflowHelper.WriteStepSummary(
                $"{actionEnvironment.EnvGitHubServerUrl}/{actionEnvironment.EnvGitHubRepository}/settings/secrets/actions\n");
            canContinue = false;
        }

        if (canContinue && (!Regex.IsMatch(actionEnvironment.ApiToken, @"^ghp_[a-zA-Z0-9]{36}$")))
        {
            // GitHub personal access token is invalid
            Console.WriteLine("Entering actions secrets verify section");
            Announce($"Action secret {YouRataConstants.ApiTokenVariable} is not a GitHub personal access token");
            canContinue = false;
        }

        if (canContinue && ((string.IsNullOrEmpty(workflow.ProjectClientSecret) || string.IsNullOrEmpty(workflow.ProjectClientId)) ||
                            (workflow.ProjectClientSecret.Equals("empty") || workflow.ProjectClientId.Equals("empty"))))
        {
            // YouTube Data API credentials are missing
            Console.WriteLine("Entering actions variables section");
            // Create empty action variables for the credentials
            GitHubAPIClient.CreateVariable(actionEnvironment, YouRataConstants.ProjectClientIdVariable, "empty",
                client.LogMessage);
            client.Keepalive();
            GitHubAPIClient.CreateVariable(actionEnvironment, YouRataConstants.ProjectClientSecretsVariable, "empty",
                client.LogMessage);
            client.Keepalive();
            Announce("Fill repository variables and run action again");
            // Add URL for action variables
            GitHubWorkflowHelper.WriteStepSummary(
                $"{actionEnvironment.EnvGitHubServerUrl}/{actionEnvironment.EnvGitHubRepository}/settings/variables/actions\n");

            canContinue = false;
        }

        if (canContinue && (string.IsNullOrEmpty(workflow.ProjectApiKey) || workflow.ProjectApiKey.Equals("empty")))
        {
            // YouTube Data API key is missing
            Console.WriteLine("Entering Google API key section");
            // Create an empty action secret for the key
            GitHubAPIClient.CreateOrUpdateSecret(actionEnvironment, YouRataConstants.ProjectApiKeyVariable, "empty", client.LogMessage);
            client.Keepalive();
            Announce($"Paste Google API key in action secret {YouRataConstants.ProjectApiKeyVariable}");
            // Add URL for the action secret key
            GitHubWorkflowHelper.WriteStepSummary(
                $"{actionEnvironment.EnvGitHubServerUrl}/{actionEnvironment.EnvGitHubRepository}/settings/secrets/actions/{YouRataConstants.ProjectApiKeyVariable}\n");
            canContinue = false;
        }

        if (canContinue && (!YouTubeAuthHelper.GetTokenResponse(workflow.StoredTokenResponse, out _)))
        {
            // TOKEN_RESPONSE is missing
            Console.WriteLine("Entering Google API stored token response section");
            GoogleAuthorizationCodeFlow flow = YouTubeAuthHelper.GetFlow(workflow.ProjectClientId, workflow.ProjectClientSecret);
            if (!string.IsNullOrEmpty(workflow.RedirectCode) && workflow.RedirectCode != "empty")
            {
                // Authorization code is valid
                string redirectTokenCode = workflow.RedirectCode.Trim().Replace("=", "").Replace("&", "");
                AuthorizationCodeTokenRequest authorizationCodeTokenRequest =
                    YouTubeAuthHelper.GetTokenRequest(redirectTokenCode, workflow.ProjectClientId, workflow.ProjectClientSecret);
                // Get a token response from the authorization code
                TokenResponse? authorizationCodeTokenResponse =
                    YouTubeAuthHelper.ExchangeAuthorizationCode(authorizationCodeTokenRequest, flow);
                client.Keepalive();
                if (authorizationCodeTokenResponse != null)
                {
                    // Token response is valid, save it to an action secret
                    YouTubeAuthHelper.SaveTokenResponse(authorizationCodeTokenResponse, actionEnvironment, client.LogMessage);
                    client.Keepalive();
                    Announce($"Google API stored token response has been saved to {YouRataConstants.StoredTokenResponseVariable}");
                    // InitialSetup is complete, prepare the copy directions readme step
                    string repositoryContentUrl =
                        $"{GitHubConstants.RawContentUrl}/{actionEnvironment.EnvGitHubRepository}/{actionEnvironment.EnvGitHubRefName}/";
                    workflow.RepositoryContentUrl = WebUtility.UrlEncode(repositoryContentUrl);
                    workflow.CopyDirectionsReadme = true;
                }
                else
                {
                    Console.WriteLine("Could not exchange authorization code");
                    canContinue = false;
                }
            }
            // Create an empty action secret for the token response
            else if (GitHubAPIClient.CreateOrUpdateSecret(actionEnvironment, YouRataConstants.StoredTokenResponseVariable, "empty",
                         client.LogMessage))
            {
                client.Keepalive();
                Uri url = flow.CreateAuthorizationCodeRequest(GoogleAuthConsts.LocalhostRedirectUri).Build();
                GitHubAPIClient.CreateOrUpdateSecret(actionEnvironment, YouRataConstants.RedirectCodeVariable, "empty", client.LogMessage);
                client.Keepalive();
                // Tell the user to follow the link, allow the app, and paste the code from the URL
                Announce("Follow this link to authorize this GitHub application");
                Announce(url.ToString());
                Announce("<ins>When finished it is normal that the site can't be reached</ins>");
                Announce("Copy everything in the website URL between |code=| and |&|");
                Announce($"Paste this value in action secret {YouRataConstants.RedirectCodeVariable}");
                Announce("Run the action again within 30 seconds");
                // Add URL for the action secret code
                GitHubWorkflowHelper.WriteStepSummary(
                    $"{actionEnvironment.EnvGitHubServerUrl}/{actionEnvironment.EnvGitHubRepository}/settings/secrets/actions/{YouRataConstants.RedirectCodeVariable}\n");
                canContinue = false;
            }
            else
            {
                Console.WriteLine("Failed to create repository secrets");
                canContinue = false;
            }
        }

        if (!canContinue)
        {
            // Block all other milestones from startingx
            client.BlockAllMilestones();
        }
    }
    catch (Exception ex)
    {
        client.SetStatus(MilestoneCondition.MilestoneFailed);
        throw new MilestoneException("InitialSetup failed", ex);
    }

    client.SetStatus(MilestoneCondition.MilestoneCompleted);
}

// Write to action step summary and console
void Announce(string announcement)
{
    Console.WriteLine(announcement);
    GitHubWorkflowHelper.WriteStepSummary($"{announcement}\n");
}
