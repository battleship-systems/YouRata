// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using YouRata.Common.ActionReport;
using YouRata.Common.Configuration;
using YouRata.Common.Proto;
using static YouRata.Common.Proto.ActionIntelligenceService;
using static YouRata.Common.Proto.LogService;
using static YouRata.Common.Proto.MilestoneActionIntelligence.Types;
using static YouRata.Common.Proto.MilestoneActionIntelligenceService;

namespace YouRata.Common.Milestone;

/// <summary>
/// Represents a ConflictMonitor communication client for a generic milestone
/// </summary>
public abstract class MilestoneCommunicationClient : IDisposable
{
    private readonly GrpcChannel _conflictMonitorChannel;
    private bool _disposed;

    protected MilestoneCommunicationClient()
    {
        // Expect ConflictMonitor to already be running
        _conflictMonitorChannel = GrpcChannel.ForAddress($"http://{IPAddress.Loopback}",
            new GrpcChannelOptions { HttpHandler = CreateHttpHandler(YouRataConstants.GrpcUnixSocketPath) });
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _conflictMonitorChannel.Dispose();
            _disposed = true;
        }
    }

    /// <summary>
    /// Signal ConflictMonitor that the milestone has started
    /// </summary>
    /// <typeparam name="T">Milestone type</typeparam>
    /// <param name="milestoneIntelligenceType"></param>
    /// <param name="milestoneIntelligenceName"></param>
    /// <returns>Success</returns>
    /// <exception cref="MilestoneException"></exception>
    public virtual T Activate<T>(System.Type milestoneIntelligenceType, string milestoneIntelligenceName)
    {
        T milestoneActionIntelligence = (T?)Activator.CreateInstance(milestoneIntelligenceType) ??
                                        throw new MilestoneException("Invalid milestone type to activate");
        milestoneActionIntelligence.GetType().GetProperty("ProcessId")
            ?.SetValue(milestoneActionIntelligence, Process.GetCurrentProcess().Id);
        milestoneActionIntelligence.GetType().GetProperty("Condition")
            ?.SetValue(milestoneActionIntelligence, MilestoneCondition.MilestoneRunning);
        SetMilestoneActionIntelligence(milestoneActionIntelligence, milestoneIntelligenceType, milestoneIntelligenceName);
        Console.WriteLine($"Entering {milestoneIntelligenceName}");
        return milestoneActionIntelligence;
    }

    /// <summary>
    /// Signal that all subsequent milestones should not start
    /// </summary>
    public void BlockAllMilestones()
    {
        ActionIntelligenceServiceClient actionIntelligenceServiceClient = new ActionIntelligenceServiceClient(_conflictMonitorChannel);
        ActionIntelligence actionIntelligence = actionIntelligenceServiceClient.GetActionIntelligence(new Empty());
        // Find all milestone action intelligence members
        List<PropertyInfo> milestoneIntelligenceProperties = actionIntelligence.MilestoneIntelligence
            .GetType()
            .GetProperties()
            .Where(prop => prop.PropertyType.Name.Contains("ActionIntelligence"))
            .ToList();
        if (milestoneIntelligenceProperties?.Count > 0)
        {
            foreach (PropertyInfo milestoneIntelligenceProperty in milestoneIntelligenceProperties)
            {
                object? milestoneIntelligenceObject = milestoneIntelligenceProperty.GetValue(actionIntelligence.MilestoneIntelligence);
                if (milestoneIntelligenceObject == null) continue;
                string milestoneIntelligenceName = milestoneIntelligenceProperty.Name;
                System.Type milestoneIntelligenceType = milestoneIntelligenceObject.GetType();
                // Condition is common to all milestone intelligence
                PropertyInfo? conditionProperty = milestoneIntelligenceType.GetProperty("Condition");
                if (conditionProperty == null)
                {
                    continue;
                }

                object? conditionCurrentValue = conditionProperty.GetValue(milestoneIntelligenceObject);
                if (conditionCurrentValue?.GetType() == typeof(MilestoneCondition))
                {
                    MilestoneCondition currentCondition = (MilestoneCondition)conditionCurrentValue;
                    if (currentCondition is MilestoneCondition.MilestoneCompleted or MilestoneCondition.MilestoneFailed)
                    {
                        // Do not block a milestone that has already completed or failed
                        continue;
                    }
                }

                // Set the condition to MilestoneBlocked
                conditionProperty.SetValue(milestoneIntelligenceObject, MilestoneCondition.MilestoneBlocked);

                SetMilestoneActionIntelligence(milestoneIntelligenceObject, milestoneIntelligenceType, milestoneIntelligenceName);
            }
        }
    }

    /// <summary>
    /// Get the global action intelligence
    /// </summary>
    /// <returns></returns>
    public ActionIntelligence GetActionIntelligence()
    {
        ActionIntelligenceServiceClient actionIntelligenceServiceClient = new ActionIntelligenceServiceClient(_conflictMonitorChannel);
        return actionIntelligenceServiceClient.GetActionIntelligence(new Empty());
    }

    /// <summary>
    /// Retreive the in-memory log messages
    /// </summary>
    /// <returns></returns>
    public string GetLogMessages()
    {
        LogServiceClient logServiceClient = new LogServiceClient(_conflictMonitorChannel);
        return logServiceClient.GetLogMessages(new Empty()).Messages.ToString();
    }

    /// <summary>
    /// Get the milestone type specific intelligence
    /// </summary>
    /// <param name="milestoneIntelligenceType"></param>
    /// <returns></returns>
    public virtual object? GetMilestoneActionIntelligence(System.Type milestoneIntelligenceType)
    {
        object? milestoneActionIntelligence = null;
        if (!IsValidMilestoneIntelligenceType(milestoneIntelligenceType)) return milestoneActionIntelligence;
        ActionIntelligenceServiceClient actionIntelligenceServiceClient = new ActionIntelligenceServiceClient(_conflictMonitorChannel);
        ActionIntelligence? actionIntelligence = actionIntelligenceServiceClient.GetActionIntelligence(new Empty());
        if (actionIntelligence == null) return milestoneActionIntelligence;
        // Find the milestone type in MilestoneActionIntelligence
        List<PropertyInfo> milestoneIntelligenceProperties = actionIntelligence.MilestoneIntelligence
            .GetType()
            .GetProperties()
            .Where(prop => prop.PropertyType == milestoneIntelligenceType)
            .ToList();
        if (milestoneIntelligenceProperties?.Count > 0)
        {
            PropertyInfo milestoneIntelligenceProperty = milestoneIntelligenceProperties.First();
            object? milestoneIntelligenceObject = milestoneIntelligenceProperty.GetValue(actionIntelligence.MilestoneIntelligence);
            if (milestoneIntelligenceObject != null)
            {
                milestoneActionIntelligence = milestoneIntelligenceObject;
            }
        }

        return milestoneActionIntelligence;
    }

    /// <summary>
    /// Get the previously committed action report
    /// </summary>
    /// <returns></returns>
    public ActionReportLayout GetPreviousActionReport()
    {
        ActionIntelligenceServiceClient actionIntelligenceServiceClient = new ActionIntelligenceServiceClient(_conflictMonitorChannel);
        string actionReportText = actionIntelligenceServiceClient.GetActionIntelligence(new Empty()).PreviousActionReport;
        ActionReportLayout actionReport = new ActionReportLayout();
        if (!string.IsNullOrEmpty(actionReportText))
        {
            ActionReportLayout? deserializeActionReport = JsonConvert.DeserializeObject<ActionReportLayout>(actionReportText);
            if (deserializeActionReport != null)
            {
                actionReport = deserializeActionReport;
            }
        }

        return actionReport;
    }

    /// <summary>
    /// Get the milestone type specific MilestoneCondition
    /// </summary>
    /// <param name="milestoneIntelligenceType"></param>
    /// <returns></returns>
    public virtual MilestoneCondition GetStatus(System.Type milestoneIntelligenceType)
    {
        MilestoneCondition milestoneCondition = new MilestoneCondition();
        object? milestoneActionIntelligence = GetMilestoneActionIntelligence(milestoneIntelligenceType);
        if (milestoneActionIntelligence == null) return milestoneCondition;
        PropertyInfo? conditionProperty = milestoneActionIntelligence.GetType().GetProperty("Condition");
        object? conditionValue = conditionProperty?.GetValue(milestoneActionIntelligence, null);
        if (conditionValue != null)
        {
            milestoneCondition = (MilestoneCondition)conditionValue;
        }

        return milestoneCondition;
    }

    /// <summary>
    /// Get the YouRata configuration root
    /// </summary>
    /// <returns></returns>
    public YouRataConfiguration GetYouRataConfiguration()
    {
        YouRataConfiguration appConfig = new YouRataConfiguration();
        ActionIntelligenceServiceClient actionIntelligenceServiceClient = new ActionIntelligenceServiceClient(_conflictMonitorChannel);
        string configJson = actionIntelligenceServiceClient.GetActionIntelligence(new Empty()).ConfigJson;
        using (MemoryStream jsonMemoryStream = new MemoryStream(Encoding.ASCII.GetBytes(configJson)))
        {
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder()
                .AddJsonStream(jsonMemoryStream);
            configurationBuilder.Build().Bind(appConfig);
        }

        return appConfig;
    }

    /// <summary>
    /// Determines if a milestone type is blocked
    /// </summary>
    /// <param name="milestoneIntelligenceType"></param>
    /// <returns></returns>
    /// <exception cref="MilestoneException"></exception>
    /// <remarks>This the first call to ConflictMonitor so it needs retry logic</remarks>
    public virtual bool IsBlocked(System.Type milestoneIntelligenceType)
    {
        MilestoneCondition milestoneCondition = new MilestoneCondition();
        int retryCount = 0;
        while (retryCount < 3)
        {
            try
            {
                milestoneCondition = GetStatus(milestoneIntelligenceType);
                break;
            }
            catch (Grpc.Core.RpcException ex)
            {
                retryCount++;
                if (retryCount > 1)
                {
                    throw new MilestoneException("Failed to connect to ConflictMonitor", ex);
                }
            }

            // Wait for ConflictMonitor to finish starting
            TimeSpan backOff = APIBackoffHelper.GetRandomBackoff(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10));
            Thread.Sleep(backOff);
        }

        return (milestoneCondition == MilestoneCondition.MilestoneBlocked);
    }

    /// <summary>
    /// Signal ConflictMonitor that the milestone is alive
    /// </summary>
    /// <param name="milestoneIntelligenceType"></param>
    /// <param name="milestoneIntelligenceName"></param>
    public virtual void Keepalive(System.Type milestoneIntelligenceType, string milestoneIntelligenceName)
    {
        if (!IsValidMilestoneIntelligenceType(milestoneIntelligenceType)) return;
        MilestoneActionIntelligenceServiceClient milestoneActionIntelligenceServiceClient =
            new MilestoneActionIntelligenceServiceClient(_conflictMonitorChannel);
        List<MethodInfo> clientMethods = milestoneActionIntelligenceServiceClient
            .GetType()
            .GetMethods()
            .Where(method => method
                .GetParameters().Length == 4 && method
                .Name == $"Keepalive{milestoneIntelligenceName}")
            .ToList();
        if (clientMethods?.Count > 0)
        {
            MethodInfo clientMethod = clientMethods.First();
            clientMethod.Invoke(milestoneActionIntelligenceServiceClient,
                new object?[] { new Empty(), null, null, default(CancellationToken) });
        }
    }

    /// <summary>
    /// Log a message to the in-memory log
    /// </summary>
    /// <param name="message"></param>
    /// <param name="milestone"></param>
    public virtual void LogMessage(string message, string milestone)
    {
        LogServiceClient logServiceClient = new LogServiceClient(_conflictMonitorChannel);
        MilestoneLog milestoneLog = new MilestoneLog { Message = message, Milestone = milestone };
        milestoneLog.Message = message;
        milestoneLog.Milestone = milestone;
        logServiceClient.WriteLogMessage(milestoneLog);
    }

    /// <summary>
    /// Set the milestone type specific intelligence
    /// </summary>
    /// <param name="milestoneActionIntelligence"></param>
    /// <param name="milestoneIntelligenceType"></param>
    /// <param name="milestoneIntelligenceName"></param>
    public virtual void SetMilestoneActionIntelligence(object milestoneActionIntelligence, System.Type milestoneIntelligenceType,
        string milestoneIntelligenceName)
    {
        if (!IsValidMilestoneIntelligenceType(milestoneIntelligenceType)) return;
        MilestoneActionIntelligenceServiceClient milestoneActionIntelligenceServiceClient =
            new MilestoneActionIntelligenceServiceClient(_conflictMonitorChannel);
        // Find the update method for the milestone type in MMilestoneActionIntelligenceServiceClient
        List<MethodInfo> clientMethods = milestoneActionIntelligenceServiceClient
            .GetType()
            .GetMethods()
            .Where(method => method
                .GetParameters().Length == 4 && method
                .Name == $"Update{milestoneIntelligenceName}ActionIntelligence")
            .ToList();
        if (clientMethods?.Count > 0)
        {
            MethodInfo clientMethod = clientMethods.First();
            clientMethod.Invoke(milestoneActionIntelligenceServiceClient,
                new object?[] { milestoneActionIntelligence, null, null, default(CancellationToken) });
        }
    }

    /// <summary>
    /// Set the milestone type specific MilestoneCondition
    /// </summary>
    /// <param name="milestoneCondition"></param>
    /// <param name="milestoneIntelligenceType"></param>
    /// <param name="milestoneIntelligenceName"></param>
    public virtual void SetStatus(MilestoneCondition milestoneCondition, System.Type milestoneIntelligenceType,
        string milestoneIntelligenceName)
    {
        if (!IsValidMilestoneIntelligenceType(milestoneIntelligenceType)) return;
        object? milestoneActionIntelligence = GetMilestoneActionIntelligence(milestoneIntelligenceType);
        if (milestoneActionIntelligence == null) return;
        milestoneActionIntelligence.GetType().GetProperty("Condition")?.SetValue(milestoneActionIntelligence, milestoneCondition);

        SetMilestoneActionIntelligence(milestoneActionIntelligence, milestoneIntelligenceType, milestoneIntelligenceName);
    }

    /// <summary>
    /// Configures an http client to use a unix socket path
    /// </summary>
    /// <param name="socketPath"></param>
    /// <returns></returns>
    private static SocketsHttpHandler CreateHttpHandler(string socketPath)
    {
        UnixDomainSocketEndPoint endPoint = new UnixDomainSocketEndPoint(socketPath);
        UnixDomainSocketConnectionFactory connectionFactory = new UnixDomainSocketConnectionFactory(endPoint);
        SocketsHttpHandler socketsHttpHandler = new SocketsHttpHandler
        {
            ConnectCallback = connectionFactory.ConnectAsync, PlaintextStreamFilter = connectionFactory.PlaintextFilter
        };

        return socketsHttpHandler;
    }

    /// <summary>
    /// Check for a valid milestone type
    /// </summary>
    /// <param name="milestoneIntelligenceType"></param>
    /// <returns></returns>
    private bool IsValidMilestoneIntelligenceType(System.Type milestoneIntelligenceType)
    {
        System.Type[] milestoneTypes = typeof(MilestoneActionIntelligence.Types).GetNestedTypes();
        if (!milestoneTypes.Contains(milestoneIntelligenceType)) return false;
        return true;
    }

    /// <summary>
    /// Handles creating the socket connection to the gRPC server
    /// </summary>
    private class UnixDomainSocketConnectionFactory
    {
        private readonly EndPoint _endPoint;

        internal UnixDomainSocketConnectionFactory(EndPoint endPoint)
        {
            _endPoint = endPoint;
        }

        internal async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext _, CancellationToken cancellationToken = default)
        {
            Socket unixSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            try
            {
                await unixSocket.ConnectAsync(_endPoint, cancellationToken).ConfigureAwait(false);
                return new NetworkStream(unixSocket, true);
            }
            catch (Exception ex)
            {
                unixSocket.Dispose();
                throw new HttpRequestException($"Error connecting to '{_endPoint}'.", ex);
            }
        }

        internal ValueTask<Stream> PlaintextFilter(SocketsHttpPlaintextStreamFilterContext filterContext,
            CancellationToken cancellationToken = default)
        {
            return new ValueTask<Stream>(Task.Run(() => filterContext.PlaintextStream, cancellationToken));
        }
    }
}
