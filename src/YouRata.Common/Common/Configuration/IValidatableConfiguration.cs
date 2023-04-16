// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;

namespace YouRata.Common.Configuration;

/// <summary>
/// Represents a custom validatable configuration
/// </summary>
public interface IValidatableConfiguration
{
    void Validate();
}
