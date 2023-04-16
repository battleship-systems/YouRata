// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using System.Net;
using System.Threading;
using Google;
using YouRata.Common.Milestone;
using static YouRata.Common.Proto.MilestoneActionIntelligence.Types;

namespace YouRata.Common.YouTube;

/// <summary>
/// YouTube Data API request retry/recovery helper class
/// </summary>
public static class YouTubeRetryHelper
{
    public static void RetryCommand(YouTubeSyncActionIntelligence? intelligence, int quotaCost, Action command, TimeSpan minRetry,
        TimeSpan maxRetry, Action<string> logger)
    {
        // Do not trap any exceptions
        RetryCommand(intelligence, quotaCost, command, minRetry, maxRetry, logger, null, out _);
    }

    /// <summary>
    /// Used for retrying actions
    /// </summary>
    /// <param name="intelligence"></param>
    /// <param name="quotaCost"></param>
    /// <param name="command"></param>
    /// <param name="minRetry"></param>
    /// <param name="maxRetry"></param>
    /// <param name="logger"></param>
    /// <param name="trapStatus"></param>
    /// <param name="trapped"></param>
    /// <exception cref="MilestoneException"></exception>
    public static void RetryCommand(YouTubeSyncActionIntelligence? intelligence, int quotaCost, Action command, TimeSpan minRetry,
        TimeSpan maxRetry, Action<string> logger, HttpStatusCode? trapStatus, out bool trapped)
    {
        trapped = false;
        int retryCount = 0;
        while (retryCount < 3)
        {
            try
            {
                try
                {
                    if (intelligence != null)
                    {
                        // Decrement rate limit before the call
                        intelligence.CalculatedQueriesPerDayRemaining -= quotaCost;
                    }

                    command.Invoke();
                }
                catch (GoogleApiException unavailableEx) when (unavailableEx.HttpStatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    // 503 transient error
                    retryCount++;
                    if (intelligence != null)
                    {
                        intelligence.CalculatedQueriesPerDayRemaining += quotaCost;
                    }

                    if (retryCount > 2)
                    {
                        throw;
                    }

                    Thread.Sleep(maxRetry);
                    continue;
                }

                break;
            }
            catch (GoogleApiException ex) when (trapStatus != null)
            {
                if (ex.HttpStatusCode == trapStatus)
                {
                    // Exception type matches specified trap
                    logger.Invoke($"YouTube API: {ex.Message}");
                    trapped = true;
                    break;
                }

                retryCount++;
                if (retryCount > 1)
                {
                    throw new MilestoneException("YouTube API failure", ex);
                }
            }
            catch (Exception)
            {
                retryCount++;
                if (retryCount > 2)
                {
                    throw;
                }
            }

            // Wait for a random amount of time before the next attempt
            TimeSpan backOff = APIBackoffHelper.GetRandomBackoff(minRetry, maxRetry);
            Thread.Sleep(backOff);
        }
    }

    public static T? RetryCommand<T>(YouTubeSyncActionIntelligence? intelligence, int quotaCost, Func<T> command, TimeSpan minRetry,
        TimeSpan maxRetry, Action<string> logger)
    {
        // Do not trap any exceptions
        return RetryCommand(intelligence, quotaCost, command, minRetry, maxRetry, logger, null, out _);
    }

    /// <summary>
    /// Used for retrying functions
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="intelligence"></param>
    /// <param name="quotaCost"></param>
    /// <param name="command"></param>
    /// <param name="minRetry"></param>
    /// <param name="maxRetry"></param>
    /// <param name="logger"></param>
    /// <param name="trapStatus"></param>
    /// <param name="trapped"></param>
    /// <returns></returns>
    /// <exception cref="MilestoneException"></exception>
    public static T? RetryCommand<T>(YouTubeSyncActionIntelligence? intelligence, int quotaCost, Func<T> command, TimeSpan minRetry,
        TimeSpan maxRetry, Action<string> logger, HttpStatusCode? trapStatus, out bool trapped)
    {
        trapped = false;
        int retryCount = 0;
        T? returnValue = default(T?);
        while (retryCount < 3)
        {
            try
            {
                try
                {
                    if (intelligence != null)
                    {
                        // Decrement rate limit before the call
                        intelligence.CalculatedQueriesPerDayRemaining -= quotaCost;
                    }

                    returnValue = command.Invoke();
                }
                catch (GoogleApiException unavailableEx) when (unavailableEx.HttpStatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    // 503 transient error
                    retryCount++;
                    if (intelligence != null)
                    {
                        intelligence.CalculatedQueriesPerDayRemaining += quotaCost;
                    }

                    if (retryCount > 2)
                    {
                        throw;
                    }

                    Thread.Sleep(maxRetry);
                    continue;
                }

                break;
            }
            catch (GoogleApiException ex) when (trapStatus != null)
            {
                if (ex.HttpStatusCode == trapStatus)
                {
                    // Exception type matches specified trap
                    logger.Invoke($"YouTube API: {ex.Message}");
                    trapped = true;
                    break;
                }

                retryCount++;
                if (retryCount > 1)
                {
                    throw new MilestoneException("YouTube API failure", ex);
                }
            }
            catch (Exception)
            {
                retryCount++;
                if (retryCount > 2)
                {
                    throw;
                }
            }

            // Wait for a random amount of time before the next attempt
            TimeSpan backOff = APIBackoffHelper.GetRandomBackoff(minRetry, maxRetry);
            Thread.Sleep(backOff);
        }

        return returnValue;
    }
}
