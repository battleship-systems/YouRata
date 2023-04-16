// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using Newtonsoft.Json;
using YouRata.Common.Milestone;
using YouRata.Common.Proto;

namespace YouRata.Common.GitHub;

/// <summary>
/// GitHub API calls not implemented in Octokit
/// </summary>
public static class UnsupportedGitHubAPIClient
{
    /// <summary>
    /// Create an action variable
    /// </summary>
    /// <param name="environment"></param>
    /// <param name="variableName"></param>
    /// <param name="variableValue"></param>
    /// <param name="logger"></param>
    /// <returns>Success</returns>
    public static bool CreateVariable(GitHubActionEnvironment environment, string variableName, string variableValue, Action<string> logger)
    {
        if (!HasRemainingCalls(environment)) return false;
        if (environment.EnvGitHubRepository == null) return false;
        if (environment.EnvGitHubRepository.Split("/").Length != 2) return false;
        HttpResponseMessage? response = null;
        Action createVariable = (() =>
        {
            using (HttpClient client = new HttpClient())
            {
                client.Timeout = GitHubConstants.RequestTimeout;
                client.BaseAddress = new Uri($"{environment.EnvGitHubApiUrl}/");
                client.DefaultRequestHeaders.Add("User-Agent", GitHubConstants.ProductHeaderName);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", environment.ApiToken);
                RepositoryVariable variable = new RepositoryVariable { Name = variableName, Value = variableValue };
                variable.Name = variableName;
                variable.Value = variableValue;
                string contentData = JsonConvert.SerializeObject(variable);
                HttpContent content = new StringContent(contentData);
                // Directly POST the request to the API
                response = client.PostAsync($"repos/{environment.EnvGitHubRepository}/actions/variables", content).Result;
            }
        });
        RetryCommand(environment, createVariable, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), logger);
        if (response?.IsSuccessStatusCode == true) return true;
        return false;
    }

    /// <summary>
    /// Check for sufficient remaining API calls
    /// </summary>
    /// <param name="environment"></param>
    /// <returns>RemainingCalls</returns>
    internal static bool HasRemainingCalls(GitHubActionEnvironment environment)
    {
        if (environment.RateLimitCoreRemaining is > 0 and < 100)
        {
            Console.WriteLine($"WARNING: Only {environment.RateLimitCoreRemaining} GitHub API calls remaining");
        }

        return (environment.RateLimitCoreRemaining >= 3);
    }

    /// <summary>
    /// Used for retrying actions
    /// </summary>
    /// <param name="environment"></param>
    /// <param name="command"></param>
    /// <param name="minRetry"></param>
    /// <param name="maxRetry"></param>
    /// <param name="logger"></param>
    /// <exception cref="MilestoneException"></exception>
    private static void RetryCommand(GitHubActionEnvironment environment, Action command, TimeSpan minRetry, TimeSpan maxRetry,
        Action<string> logger)
    {
        int retryCount = 0;
        while (retryCount < 3)
        {
            try
            {
                {
                    // Decrement rate limit before the call
                    environment.RateLimitCoreRemaining--;
                    command.Invoke();
                }
                break;
            }
            catch (Exception ex)
            {
                retryCount++;
                logger.Invoke(ex.Message);
                if (retryCount > 1)
                {
                    throw new MilestoneException("GitHub API failure", ex);
                }
            }

            // Wait for a random amount of time before the next attempt
            TimeSpan backOff = APIBackoffHelper.GetRandomBackoff(minRetry, maxRetry);
            Thread.Sleep(backOff);
        }
    }

    /// <summary>
    /// GitHub repository variable root
    /// </summary>
    public class RepositoryVariable
    {
        [JsonProperty(PropertyName = "name")] public string? Name { get; set; }

        [JsonProperty(PropertyName = "value")] public string? Value { get; set; }
    }
}
