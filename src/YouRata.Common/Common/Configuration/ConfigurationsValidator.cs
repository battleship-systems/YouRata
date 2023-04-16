// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YouRata.Common.Configuration;

/// <summary>
/// Runs validation on any IValidatableConfiguration instance in the service
/// </summary>
public class ConfigurationsValidator : IValidator
{
    private readonly IEnumerable<IValidatableConfiguration> _validatableObjects;

    public ConfigurationsValidator(IEnumerable<IValidatableConfiguration> validatableObjects)
    {
        _validatableObjects = validatableObjects;
    }

    public async Task ValidateAsync()
    {
        foreach (IValidatableConfiguration validatableObject in _validatableObjects)
        {
            await Task.Run(() => validatableObject.Validate()).ConfigureAwait(false);
            if (validatableObject is YouRataConfiguration)
            {
                // Call ValidateConfigurationMembers on the root YouRataConfiguration
                await Task.Run(() => { ((YouRataConfiguration)validatableObject).ValidateConfigurationMembers(); }).ConfigureAwait(false);
            }
        }
    }
}
