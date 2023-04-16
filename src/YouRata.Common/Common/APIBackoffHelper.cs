// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;

namespace YouRata.Common;

/// <summary>
/// API failure retry backoff helper class
/// </summary>
public static class APIBackoffHelper
{
    public static TimeSpan GetRandomBackoff(TimeSpan minTime, TimeSpan maxTime)
    {
        Random random = new Random();
        return TimeSpan.FromMilliseconds(random.Next((int)minTime.TotalMilliseconds, (int)maxTime.TotalMilliseconds));
    }
}
