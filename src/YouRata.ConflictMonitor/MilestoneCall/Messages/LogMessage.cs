// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using YouRata.Common.Proto;

namespace YouRata.ConflictMonitor.MilestoneCall.Messages;

/// <summary>
/// LogMessage service impl
/// </summary>
internal class LogMessage : LogService.LogServiceBase
{
    private readonly CallManager _callManager;
    private readonly ILogger<LogMessage> _logger;

    public LogMessage(ILoggerFactory loggerFactory, CallManager callManager)
    {
        _logger = loggerFactory.CreateLogger<LogMessage>();
        _callManager = callManager;
    }

    public override Task<LogMessages> GetLogMessages(Empty request, ServerCallContext context)
    {
        TaskCompletionSource<LogMessages> logMessagesResult = new TaskCompletionSource<LogMessages>();
        _callManager.ActionCallbacks.Enqueue((callHandler) =>
        {
            // Create a blank LogMessages
            LogMessages logMessages = new LogMessages();
            try
            {
                // Fill the LogMessages with CallHandler logs
                logMessages.Messages.AddRange(callHandler.GetLogs());
            }
            catch (Exception e)
            {
                _logger.LogError($"Error on GetLogMessages: {e.Message}");
            }

            logMessagesResult.SetResult(logMessages);
        });
        // Let CallManager know it needs to run an action
        _callManager.ActionReady.Set();
        return logMessagesResult.Task;
    }

    public override Task<Empty> WriteLogMessage(MilestoneLog request, ServerCallContext context)
    {
        TaskCompletionSource<Empty> emptyResult = new TaskCompletionSource<Empty>();
        _callManager.ActionCallbacks.Enqueue((callHandler) =>
        {
            try
            {
                // Log message to in-memory log
                callHandler.LogMessage($"({request.Milestone}) {request.Message}");
            }
            catch (Exception e)
            {
                _logger.LogError($"Error on WriteLogMessage: {e.Message}");
            }

            emptyResult.SetResult(new Empty());
        });
        // Let CallManager know it needs to run an action
        _callManager.ActionReady.Set();
        return emptyResult.Task;
    }
}
