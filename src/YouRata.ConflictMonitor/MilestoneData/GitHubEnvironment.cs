// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using System.Reflection;
using YouRata.Common.Proto;

namespace YouRata.ConflictMonitor.MilestoneData;

/// <summary>
/// ConflictMonitor abstraction of GitHubActionEnvironment
/// </summary>
internal class GitHubEnvironment
{
    public GitHubEnvironment()
    {
        this.Action = string.Empty;
        this.Action_Path = string.Empty;
        this.Action_Repository = string.Empty;
        this.Actor = string.Empty;
        this.Actor_Id = string.Empty;
        this.Api_Url = string.Empty;
        this.Base_Ref = string.Empty;
        this.Env = string.Empty;
        this.Event_Name = string.Empty;
        this.Event_Path = string.Empty;
        this.Graphql_Url = string.Empty;
        this.Head_Ref = string.Empty;
        this.Job = string.Empty;
        this.Path = string.Empty;
        this.Ref = string.Empty;
        this.Ref_Name = string.Empty;
        this.Ref_Protected = string.Empty;
        this.Ref_Type = string.Empty;
        this.Repository = string.Empty;
        this.Repository_Id = 0;
        this.Repository_Owner = string.Empty;
        this.Repository_Owner_Id = 0;
        this.Retention_Days = 0;
        this.Run_Attempt = 0;
        this.Run_Id = 0;
        this.Run_Number = 0;
        this.Server_Url = string.Empty;
        this.Sha = string.Empty;
        this.Step_Summary = string.Empty;
        this.Workflow = string.Empty;
        this.Workflow_Ref = string.Empty;
        this.Workflow_Sha = string.Empty;
        this.Workspace = string.Empty;
    }

    /// <summary>
    /// Populate a new GitHubActionEnvironment with this instances properties
    /// </summary>
    /// <returns>GitHubActionEnvironment</returns>
    public GitHubActionEnvironment GetActionEnvironment()
    {
        GitHubActionEnvironment actionEnvironment = new GitHubActionEnvironment();
        Type actionEnvironmentType = actionEnvironment.GetType();
        Type environmentType = this.GetType();
        PropertyInfo[] environmentProps = environmentType.GetProperties();
        foreach (PropertyInfo environmentProp in environmentProps)
        {
            if (!environmentProp.CanRead)
            {
                continue;
            }

            if (string.IsNullOrEmpty(environmentProp.Name))
            {
                continue;
            }

            // Protobuf removes underscores from field names
            string propertyName = $"EnvGitHub{environmentProp.Name.Replace("_", "")}";
            PropertyInfo? targetProperty = actionEnvironmentType.GetProperty(propertyName);
            if (targetProperty == null)
            {
                continue;
            }

            if (!targetProperty.PropertyType.IsAssignableFrom(environmentProp.PropertyType))
            {
                continue;
            }

            // Set the GitHubActionEnvironment value
            targetProperty.SetValue(actionEnvironment, environmentProp.GetValue(this));
        }

        return actionEnvironment;
    }

    public string Action { get; set; }

    public string Action_Path { get; set; }

    public string Action_Repository { get; set; }

    public string Actor { get; set; }

    public string Actor_Id { get; set; }

    public string Api_Url { get; set; }

    public string Base_Ref { get; set; }

    public string Env { get; set; }

    public string Event_Name { get; set; }

    public string Event_Path { get; set; }

    public string Graphql_Url { get; set; }

    public string Head_Ref { get; set; }

    public string Job { get; set; }

    public string Path { get; set; }

    public string Ref { get; set; }

    public string Ref_Name { get; set; }

    public string Ref_Protected { get; set; }

    public string Ref_Type { get; set; }

    public string Repository { get; set; }

    public long Repository_Id { get; set; }

    public string Repository_Owner { get; set; }

    public long Repository_Owner_Id { get; set; }

    public int Retention_Days { get; set; }

    public int Run_Attempt { get; set; }

    public long Run_Id { get; set; }

    public int Run_Number { get; set; }

    public string Server_Url { get; set; }

    public string Sha { get; set; }

    public string Step_Summary { get; set; }

    public string Workflow { get; set; }

    public string Workflow_Ref { get; set; }

    public string Workflow_Sha { get; set; }

    public string Workspace { get; set; }
}
