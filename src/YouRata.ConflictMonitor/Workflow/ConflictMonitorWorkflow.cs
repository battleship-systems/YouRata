// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using YouRata.Common;
using YouRata.Common.GitHub;

namespace YouRata.ConflictMonitor.Workflow;

/// <summary>
/// Represents a GitHub Actions workflow
/// </summary>
internal class ConflictMonitorWorkflow
{
    // GitHub App installation access token
    private readonly string _actionToken;
    // GitHub Personal access token
    private readonly string _apiToken;
    // Saved YouTube token response
    private readonly string _tokenResponse;

#pragma warning disable CS8601
#pragma warning disable CS8618
    public ConflictMonitorWorkflow()
    {
        _actionToken = Environment
            .GetEnvironmentVariable(YouRataConstants.ActionTokenVariable);
        _apiToken = Environment
            .GetEnvironmentVariable(YouRataConstants.ApiTokenVariable);
        _tokenResponse = Environment
            .GetEnvironmentVariable(YouRataConstants.StoredTokenResponseVariable);
        InitialSetupComplete = false;
    }
#pragma warning restore CS8601
#pragma warning restore CS8618

    public string ApiToken => _apiToken;

    public string GitHubToken => _actionToken;

    public bool InitialSetupComplete
    {
        set => GitHubWorkflowHelper
            .PushVariable(YouRataConstants.InitialSetupCompleteVariable, value.ToString());
    }

    public string StoredTokenResponse => _tokenResponse;
}
