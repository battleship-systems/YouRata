// Copyright (c) 2023 batteship-systems.
// Licensed under the MIT license.

using System;
using Microsoft.Extensions.Configuration;

namespace YouRata.Common.GitHub;

/// <summary>
/// Builds a configuration with all GitHub environment variables
/// </summary>
public sealed class GitHubEnvironmentBuilder
{
    public IConfiguration Build()
    {
        return new ConfigurationBuilder()
            // Add GITHUB_ prefixed environment variables
            .AddEnvironmentVariables("GITHUB_")
            .Build();
    }
}
