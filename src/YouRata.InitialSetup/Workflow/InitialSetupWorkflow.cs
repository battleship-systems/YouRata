// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using YouRata.Common;
using YouRata.Common.GitHub;

namespace YouRata.InitialSetup.Workflow;

/// <summary>
/// Represents a GitHub Actions workflow
/// </summary>
internal class InitialSetupWorkflow
{
    // Google Cloud project credential API key
    private readonly string _apiKey;
    // OAuth 2.0 client ID
    private readonly string _clientId;
    // OAuth 2.0 client secret
    private readonly string _clientSecret;
    // Authentication redirect code
    private readonly string _redirectCode;
    // Saved token response
    private readonly string _tokenResponse;

#pragma warning disable CS8601
#pragma warning disable CS8618
    public InitialSetupWorkflow()
    {
        _apiKey = Environment
            .GetEnvironmentVariable(YouRataConstants.ProjectApiKeyVariable);
        _clientId = Environment
            .GetEnvironmentVariable(YouRataConstants.ProjectClientIdVariable);
        _clientSecret = Environment
            .GetEnvironmentVariable(YouRataConstants.ProjectClientSecretsVariable);
        _redirectCode = Environment
            .GetEnvironmentVariable(YouRataConstants.RedirectCodeVariable);
        _tokenResponse = Environment
            .GetEnvironmentVariable(YouRataConstants.StoredTokenResponseVariable);
    }
#pragma warning restore CS8601
#pragma warning restore CS8618

    public bool CopyDirectionsReadme
    {
        set => GitHubWorkflowHelper
            .PushVariable(YouRataConstants.CopyDirectionsReadmeVariable, value.ToString());
    }

    public string ProjectClientId => _clientId;

    public string ProjectClientSecret => _clientSecret;

    public string ProjectApiKey => _apiKey;

    public string RedirectCode => _redirectCode;

    public string RepositoryContentUrl
    {
        set => GitHubWorkflowHelper
            .PushVariable(YouRataConstants.RepositoryContentUrlVariable, value.ToString());
    }

    public string StoredTokenResponse => _tokenResponse;
}
