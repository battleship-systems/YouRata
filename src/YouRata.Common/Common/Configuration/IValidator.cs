// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;

namespace YouRata.Common.Configuration;

/// <summary>
/// Represents a custom validator
/// </summary>
public interface IValidator
{
    Task ValidateAsync();
}
