// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System.IO;
using System.IO.Compression;

namespace YouRata.Common;

/// <summary>
/// Constant values for the GitHub action
/// </summary>
public static class YouRataConstants
{
    // Previous action report file name
    public const string ActionReportFileName = "action-report.json";
    // Git commit message for updating the action report
    public const string ActionReportMessage = "ActionReport update";
    // GITHUB_TOKEN variable name
    public const string ActionTokenVariable = "ACTION_TOKEN";
    // API_TOKEN variable name
    public const string ApiTokenVariable = "API_TOKEN";
    // Variable to trigger README.md overwrite
    public const string CopyDirectionsReadmeVariable = "COPY_DIRECTIONS_README";
    // Directory for errata markdowns
    public const string ErrataRootDirectory = "errata/";
    // Last commit SHA for the push variable name
    public const string GitHubEventBeforeVariable = "GITHUB_EVENT_BEFORE";
    // Working directory on the runner variable name
    public const string GitHubWorkspaceVariable = "GITHUB_WORKSPACE";
    // gRPC compression level
    public const CompressionLevel GrpcResponseCompressionLevel = CompressionLevel.NoCompression;
    // Variable to trigger skipping InitialSetup
    public const string InitialSetupCompleteVariable = "INITIAL_SETUP_COMPLETE";
    // Milliseconds to check for inert milestones
    public const int MilestoneLifetimeCheckInterval = 200;
    // Time zone for YouTube Data API quota reset
    public const string PacificTimeZone = "US/Pacific";
    // Google Cloud project credential API key variable name
    public const string ProjectApiKeyVariable = "PROJECT_API_KEY";
    // OAuth 2.0 client ID variable name
    public const string ProjectClientIdVariable = "PROJECT_CLIENT_ID";
    // OAuth 2.0 client secret variable name
    public const string ProjectClientSecretsVariable = "PROJECT_CLIENT_SECRET";
    // Authentication redirect code variable name
    public const string RedirectCodeVariable = "CODE";
    // URL-encoded URL for GitHub repository raw content variable name
    public const string RepositoryContentUrlVariable = "REPOSITORY_CONTENT_URL";
    // Configuration file name
    public const string SettingsFileName = "yourata-settings.json";
    // Saved token response variable name
    public const string StoredTokenResponseVariable = "TOKEN_RESPONSE";
    // Zulu time format template
    public const string ZuluTimeFormat = "yyyy-MM-dd HHmmZ";
    // YouRata socket path
    public static readonly string GrpcUnixSocketPath = Path.Combine(Path.GetTempPath(), "yourata.sock");
}
