// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using System.Threading;
using YouRata.Common.Milestone;
using YouRata.Common.Proto;

namespace YouRata.Common.GitHub;

/// <summary>
/// GitHub REST API request retry/recovery helper class
/// </summary>
public static class GitHubRetryHelper
{
    public static void RetryCommand(GitHubActionEnvironment environment, Action command, Action<string> logger)
    {
        // Do not trap any exceptions
        RetryCommand(environment, command, logger, null, out _);
    }

    /// <summary>
    /// Used for retrying actions
    /// </summary>
    /// <param name="environment"></param>
    /// <param name="command"></param>
    /// <param name="logger"></param>
    /// <param name="trapInnerException"></param>
    /// <param name="trapped"></param>
    /// <exception cref="MilestoneException"></exception>
    public static void RetryCommand(GitHubActionEnvironment environment, Action command, Action<string> logger, Type? trapInnerException,
        out bool trapped)
    {
        trapped = false;
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
            catch (Exception ex) when (trapInnerException != null)
            {
                if (ex.InnerException != null && ex.InnerException.GetType() == trapInnerException)
                {
                    // Exception type matches specified trap
                    logger.Invoke($"GitHub API: {ex.Message}");
                    trapped = true;
                    break;
                }

                retryCount++;
                if (retryCount > 1)
                {
                    throw new MilestoneException("GitHub API failure", ex);
                }
            }
            catch (Exception)
            {
                retryCount++;
                if (retryCount > 1)
                {
                    throw;
                }
            }

            // Wait for a random amount of time before the next attempt
            TimeSpan backOff = APIBackoffHelper.GetRandomBackoff(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));
            Thread.Sleep(backOff);
        }
    }

    public static T? RetryCommand<T>(GitHubActionEnvironment environment, Func<T> command, Action<string> logger)
    {
        // Do not trap any exceptions
        return RetryCommand(environment, command, logger, null, out _);
    }

    /// <summary>
    /// Used for retrying functions
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="environment"></param>
    /// <param name="command"></param>
    /// <param name="logger"></param>
    /// <param name="trapInnerException"></param>
    /// <param name="trapped"></param>
    /// <returns></returns>
    /// <exception cref="MilestoneException"></exception>
    public static T? RetryCommand<T>(GitHubActionEnvironment environment, Func<T> command, Action<string> logger, Type? trapInnerException,
        out bool trapped)
    {
        trapped = false;
        int retryCount = 0;
        T? returnValue = default(T?);
        while (retryCount < 3)
        {
            try
            {
                {
                    // Decrement rate limit before the call
                    environment.RateLimitCoreRemaining--;
                    returnValue = command.Invoke();
                }
                break;
            }
            catch (Exception ex) when (trapInnerException != null)
            {
                if (ex.InnerException != null && ex.InnerException.GetType() == trapInnerException)
                {
                    // Exception type matches specified trap
                    logger.Invoke($"GitHub API: {ex.Message}");
                    trapped = true;
                    break;
                }

                retryCount++;
                if (retryCount > 1)
                {
                    throw new MilestoneException("GitHub API failure", ex);
                }
            }
            catch (Exception)
            {
                retryCount++;
                if (retryCount > 1)
                {
                    throw;
                }
            }

            // Wait for a random amount of time before the next attempt
            TimeSpan backOff = APIBackoffHelper.GetRandomBackoff(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));
            Thread.Sleep(backOff);
        }

        return returnValue;
    }
}
