// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using YouRata.Common;
using YouRata.ConflictMonitor.MilestoneCall;

namespace YouRata.ConflictMonitor.MilestoneInterface;

/// <summary>
/// Provides a custom logging instance that writes logs to a CallHandler instance for processing
/// </summary>
/// <remarks>
/// This provides the default logger for the web app
/// </remarks>
internal class InServiceLoggerProvider : ILoggerProvider
{
    private readonly CallHandler _callHandler;
    private bool _disposed;

    public InServiceLoggerProvider(CallHandler callHandler)
    {
        _callHandler = callHandler;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new InServiceLogger(_callHandler, categoryName);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            GC.SuppressFinalize(this);
        }

        _disposed = true;
    }

    /// <summary>
    /// This is the default logger for the web app
    /// </summary>
    internal class InServiceLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly CallHandler _handler;

        public InServiceLogger(CallHandler callHandler, string categoryName)
        {
            _handler = callHandler;
            _categoryName = categoryName;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            // Create a formatter for the exception
            Func<TState, Exception?, string> defaultFormatter = (fnState, fnException) =>
            {
                var lineBuilder = new StringBuilder();
                lineBuilder.Append("[");
                lineBuilder.Append(DateTime.Now.ToString(YouRataConstants.ZuluTimeFormat,
                    CultureInfo.InvariantCulture));
                lineBuilder.Append("] ");
                lineBuilder.Append("[");
                lineBuilder.Append(logLevel.ToString());
                lineBuilder.Append("] ");
                if (exception != null)
                {
                    lineBuilder.AppendLine(exception.ToString());
                }

                if (!string.IsNullOrEmpty(_categoryName))
                {
                    lineBuilder.Append(string.Format(CultureInfo.InvariantCulture, "({0}) ", _categoryName));
                }

                lineBuilder.AppendLine(formatter(state, exception));
                return lineBuilder.ToString();
            };
            // Format the message and log to CallHandler
            _handler.AppendLog(defaultFormatter(state, exception));
        }
    }
}
