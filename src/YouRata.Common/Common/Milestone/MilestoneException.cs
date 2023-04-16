// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;

namespace YouRata.Common.Milestone;

/// <summary>
/// Generic exception for any milestone
/// </summary>
public class MilestoneException : Exception
{
    public MilestoneException()
    {
    }

    public MilestoneException(string message) : base(message)
    {
    }

    public MilestoneException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
