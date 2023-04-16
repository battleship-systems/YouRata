// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using System.Net.Http;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.YouTube.v3;
using Newtonsoft.Json;
using YouRata.Common.GitHub;
using YouRata.Common.Milestone;
using YouRata.Common.Proto;

namespace YouRata.Common.YouTube;

/// <summary>
/// YouTube Data API authorization helper class
/// </summary>
public static class YouTubeAuthHelper
{
    /// <summary>
    /// Get a TokenResponse from an authorization code
    /// </summary>
    /// <param name="request"></param>
    /// <param name="flow"></param>
    /// <returns>Used during initial setup</returns>
    public static TokenResponse? ExchangeAuthorizationCode(AuthorizationCodeTokenRequest request, GoogleAuthorizationCodeFlow flow)
    {
        TokenResponse? authorizationCodeTokenResponse;
        using (HttpClient tokenHttpClient = new HttpClient())
        {
            tokenHttpClient.Timeout = YouTubeConstants.RequestTimeout;
            authorizationCodeTokenResponse =
                request.ExecuteAsync(tokenHttpClient, flow.TokenServerUrl, CancellationToken.None, flow.Clock).Result;
        }

        return authorizationCodeTokenResponse;
    }

    /// <summary>
    /// Get the Google specific authorization code flow for the project
    /// </summary>
    /// <param name="clientId"></param>
    /// <param name="clientSecret"></param>
    /// <returns></returns>
    public static GoogleAuthorizationCodeFlow GetFlow(string clientId, string clientSecret)
    {
        GoogleAuthorizationCodeFlow flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets { ClientId = clientId, ClientSecret = clientSecret },
            Scopes = new[] { YouTubeService.Scope.YoutubeForceSsl }
        });
        return flow;
    }

    /// <summary>
    /// Generate a request for an access token based on the user supplied code
    /// </summary>
    /// <param name="code"></param>
    /// <param name="clientId"></param>
    /// <param name="clientSecret"></param>
    /// <returns></returns>
    public static AuthorizationCodeTokenRequest GetTokenRequest(string code, string clientId, string clientSecret)
    {
        AuthorizationCodeTokenRequest authorizationCodeTokenRequest = new AuthorizationCodeTokenRequest
        {
            Scope = YouTubeService.Scope.YoutubeForceSsl,
            RedirectUri = GoogleAuthConsts.LocalhostRedirectUri,
            Code = code,
            ClientId = clientId,
            ClientSecret = clientSecret
        };
        return authorizationCodeTokenRequest;
    }

    /// <summary>
    /// Get a saved TokenResponse from a string
    /// </summary>
    /// <param name="response"></param>
    /// <param name="savedTokenResponse"></param>
    /// <returns>Success</returns>
    public static bool GetTokenResponse(string response, out TokenResponse savedTokenResponse)
    {
        savedTokenResponse = new TokenResponse();
        if (IsValidTokenResponse(response))
        {
            TokenResponse? deserializedTokenResponse = JsonConvert.DeserializeObject<TokenResponse>(response);
            if (deserializedTokenResponse == null) return false;
            savedTokenResponse = deserializedTokenResponse;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Get a new TokenResponse from a refresh token
    /// </summary>
    /// <param name="authFlow"></param>
    /// <param name="refreshToken"></param>
    /// <param name="logger"></param>
    /// <returns></returns>
    /// <exception cref="MilestoneException"></exception>
    public static TokenResponse RefreshToken(GoogleAuthorizationCodeFlow authFlow, string refreshToken, Action<string> logger)
    {
        Func<TokenResponse> getTokenResponse = (() =>
        {
            return authFlow.RefreshTokenAsync(null, refreshToken, CancellationToken.None).Result;
        });
        TokenResponse? tokenResponse = YouTubeRetryHelper.RetryCommand(null, 0, getTokenResponse, TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10), logger);
        if (tokenResponse == null) throw new MilestoneException("Could not refresh authorization token");
        return tokenResponse;
    }

    /// <summary>
    /// Save a TokenResponse to an action secret
    /// </summary>
    /// <param name="response"></param>
    /// <param name="actionEnvironment"></param>
    /// <param name="logger"></param>
    public static void SaveTokenResponse(TokenResponse response, GitHubActionEnvironment actionEnvironment, Action<string> logger)
    {
        string tokenResponseString = JsonConvert.SerializeObject(response, Formatting.None);
        GitHubAPIClient.CreateOrUpdateSecret(actionEnvironment, YouRataConstants.StoredTokenResponseVariable, tokenResponseString, logger);
        // Ignore errors if code secret does not exist
        GitHubAPIClient.DeleteSecret(actionEnvironment, YouRataConstants.RedirectCodeVariable, (_) => { });
    }

    /// <summary>
    /// Determines if a string represents a valid TokenResponse
    /// </summary>
    /// <param name="response"></param>
    /// <returns>Valid</returns>
    private static bool IsValidTokenResponse(string response)
    {
        if (string.IsNullOrEmpty(response)) return false;
        JsonSerializerSettings serializerSettings = new JsonSerializerSettings() { MissingMemberHandling = MissingMemberHandling.Error };
        try
        {
            TokenResponse? tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(response, serializerSettings);
            if (tokenResponse == null) return false;
            if (string.IsNullOrEmpty(tokenResponse.AccessToken)) return false;
            if (string.IsNullOrEmpty(tokenResponse.RefreshToken)) return false;
        }
        catch
        {
            return false;
        }

        return true;
    }
}
