// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using YouRata.Common.Configuration.YouTube;
using YouRata.Common.YouTube;

namespace YouRata.YouTubeSync.YouTube
{
    /// <summary>
    /// Static methods for manipulating video descriptions to add corrections
    /// </summary>
    internal static class YouTubeDescriptionCorrectionsPublisher
    {
        // Generate the updated video description with the corrections text
        public static string GetUpdatedDescription(string description, string corrections, YouTubeConfiguration config)
        {
            if (!string.IsNullOrWhiteSpace(description))
            {
                int existingCorrectionsStart = description.IndexOf(YouTubeConstants.CorrectionBegin);
                if (existingCorrectionsStart >= 0)
                {
                    // There is already a corrections section in the description
                    int existingCorrectionsLength = 0;
                    int existingCorrectionsEnd = description.IndexOf(config.CorrectionsCloser, existingCorrectionsStart);
                    if (existingCorrectionsEnd > 0)
                    {
                        existingCorrectionsLength = ((existingCorrectionsEnd + config.CorrectionsCloser.Length) - existingCorrectionsStart);
                    }
                    else
                    {
                        existingCorrectionsLength = (description.Length - existingCorrectionsStart);
                    }
                    // Remove the existing corrections section
                    description = description.Remove(existingCorrectionsStart, Math.Max(0, existingCorrectionsLength));
                }
                if ((corrections.Length + description.Length) > YouTubeConstants.MaxDescriptionLength && config.TruncateDescriptionOverflow)
                {
                    // Old description is too long to add corrections text, truncate it
                    description = description.Substring(0, Math.Max(0, description.Length - corrections.Length));
                }
            }
            return description + Environment.NewLine + corrections;
        }
    }
}
