// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using YouRata.Common.Proto;

namespace YouRata.YouTubeSync.YouTubeProcess
{
    /// <summary>
    /// YouTube video workflow action gatekeeper class
    /// </summary>
    public static class YouTubeActionGatekeeper
    {
        public static bool CanStartCorrections(GitHubActionEnvironment actionEnvironment)
        {
            return (actionEnvironment.EnvGitHubEventName.Equals("push"));
        }

        public static bool CanStartVideoUpdate(GitHubActionEnvironment actionEnvironment)
        {
            return (actionEnvironment.EnvGitHubEventName.Equals("schedule") || actionEnvironment.EnvGitHubEventName.Equals("workflow_dispatch"));
        }
    }
}
