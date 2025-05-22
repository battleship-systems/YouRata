// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using System.IO;

namespace YouRata.Common
{
    /// <summary>
    /// Checked-out content file helper class
    /// </summary>
    public sealed class ContentHelper
    {
        private readonly string _contentRoot;

        public ContentHelper()
        {
            // Determine the working directory for the checkout
            string? workspace = Environment.GetEnvironmentVariable(YouRataConstants.GitHubWorkspaceVariable);
            if (workspace == null)
                throw new FileNotFoundException("Workspace could not be found");
            _contentRoot = (workspace + Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Read the textual content of a checked-out file
        /// </summary>
        /// <param name="relativePath"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public string? GetTextContent(string relativePath, Action<string> logger)
        {
            // Try to get the content of the file
            string? content = null;
            try
            {
                content = File.ReadAllText(Path.Combine(_contentRoot, relativePath));
            }
            catch (Exception ex)
            {
                logger.Invoke($"Error on GetTextContent: {ex.Message}");
            }
            return content;
        }
    }
}
