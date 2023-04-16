// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using Octokit;

namespace YouRata.Common.GitHub;

public static class GitHubConstants
{
    // YouRata GitHub API user agent string
    public const string ProductHeaderName = "YouRata";

    // GitHub raw content address
    public const string RawContentUrl = @"https://raw.githubusercontent.com";

    // GitHub API http request timeout
    public static readonly TimeSpan RequestTimeout = TimeSpan.FromMilliseconds(1000 * 30);

    public static ProductHeaderValue ProductHeader => new(ProductHeaderName);
}
