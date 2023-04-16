// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace YouRata.Common.Configuration;

/// <summary>
/// Base class for YouRata configuration
/// </summary>
public abstract class BaseValidatableConfiguration : IValidatableConfiguration
{
    /// <summary>
    /// Throw an exception if this instance is not valid
    /// </summary>
    /// <exception cref="AggregateException"></exception>
    public void Validate()
    {
        List<ValidationResult> errors = new List<ValidationResult>();
        bool isValid = Validator.TryValidateObject(this, new ValidationContext(this), errors, true);

        if (!isValid)
        {
            // An exception will be thrown here if any invalid configuration exists
            throw new AggregateException(errors.Select(e => new ValidationException(e.ErrorMessage)));
        }
    }
}
