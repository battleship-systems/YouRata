// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using YouRata.Common.Configuration;
using YouRata.ConflictMonitor.MilestoneData;

namespace YouRata.ConflictMonitor.MilestoneInterface.Services;

/// <summary>
/// Extension methods used to register services to an IServiceCollection
/// </summary>
internal static class ServiceExtensions
{
    public static void AddAppConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // Add transient ConfigurationsValidator
        services.AddTransient<IValidator, ConfigurationsValidator>();
        // Prepare services for options
        services.AddOptions();
        // Add YouRataConfiguration
        services.Configure<YouRataConfiguration>(configuration);
        // Set up dependency injection for YouRataConfiguration
        services.AddSingleton<IValidatableConfiguration>(resolver => resolver.GetRequiredService<IOptions<YouRataConfiguration>>().Value);
    }

    public static void AddGitHubEnvironment(this IServiceCollection services, IConfiguration configuration)
    {
        // Add GitHubEnvironment
        services.Configure<GitHubEnvironment>(configuration);
    }
}
