// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using YouRata.Common.Proto;

namespace YouRata.Common.GitHub;

/// <summary>
/// Extension methods used for creating fake GitHubActionEnvironment instances
/// </summary>
public static class GitHubProtoExtensions
{
    /// <summary>
    /// Returns a new GitHubActionEnvironment with the default rate limit counter
    /// </summary>
    /// <param name="actionEnvironment"></param>
    /// <returns>GitHubActionEnvironment</returns>
    public static GitHubActionEnvironment OverrideRateLimit(this GitHubActionEnvironment actionEnvironment)
    {
        GitHubActionEnvironment newActionEnvironment =
            new GitHubActionEnvironment(actionEnvironment) { RateLimitCoreRemaining = 1000 };
        return newActionEnvironment;
    }
}
