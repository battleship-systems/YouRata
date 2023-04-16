// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using System.Globalization;
using YouRata.Common.Configuration.YouTube;
using YouRata.Common.Proto;
using YouRata.Common.YouTube;

namespace YouRata.YouTubeSync.YouTube;

/// <summary>
/// Static methods for manipulating video descriptions
/// </summary>
internal static class YouTubeDescriptionErattaPublisher
{
    // Generate the updated video description with the errata link text
    public static string GetAmendedDescription(string description, string erattaLink, YouTubeConfiguration config)
    {
        string errataNotice = string.Format(CultureInfo.InvariantCulture, config.ErrataLinkTemplate, erattaLink);
        if ((errataNotice.Length + description.Length) > YouTubeConstants.MaxDescriptionLength && config.TruncateDescriptionOverflow)
        {
            // Old description is too long to add errata link text, truncate it
            description = description.Substring(0, description.Length - errataNotice.Length);
        }

        switch (config.ErattaLinkLocation)
        {
            case "top":
                return errataNotice + Environment.NewLine + description;
            case "bottom":
                return description + Environment.NewLine + errataNotice;
        }

        return string.Empty;
    }

    // Generate the URL link to the GitHub repository markdown file
    public static string GetErrataLink(GitHubActionEnvironment actionEnvironment, string errataBulletinPath)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0}/{1}/blob/{2}/{3}",
            actionEnvironment.EnvGitHubServerUrl,
            actionEnvironment.EnvGitHubRepository,
            actionEnvironment.EnvGitHubRefName,
            errataBulletinPath);
    }
}
