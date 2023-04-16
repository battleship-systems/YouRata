// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YouRata.Common;
using YouRata.Common.Configuration;
using YouRata.Common.Configuration.MilestoneLifetime;
using YouRata.ConflictMonitor.MilestoneData;
using static YouRata.Common.Proto.MilestoneActionIntelligence.Types;

namespace YouRata.ConflictMonitor.MilestoneProcess;

/// <summary>
/// Periodically checks the MilestoneIntelligenceRegistry for running milestones that don't have recent activity
/// </summary>
internal class MilestoneLifetimeManager : IDisposable
{
    private readonly object _lock = new();
    private readonly MilestoneIntelligenceRegistry _milestoneIntelligence;
    private readonly CancellationTokenSource _stopTokenSource;
    private readonly WebApplication _webApp;
    private bool _disposed;

    internal MilestoneLifetimeManager(WebApplication webApp, MilestoneIntelligenceRegistry milestoneIntelligence)
    {
        _webApp = webApp;
        _milestoneIntelligence = milestoneIntelligence;
        _stopTokenSource = new CancellationTokenSource();
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (!_disposed)
            {
                _stopTokenSource.Cancel();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Start the loop thread
    /// </summary>
    internal void StartLoop()
    {
        lock (_lock)
        {
            if (!_disposed)
            {
                Task.Run(() =>
                {
                    while (!_disposed && !_stopTokenSource.Token.IsCancellationRequested)
                    {
                        Task.Delay(YouRataConstants.MilestoneLifetimeCheckInterval, _stopTokenSource.Token);
                        ProcessLifetimeManager();
                    }

                    _stopTokenSource.Dispose();
                });
            }
        }
    }

    /// <summary>
    /// Check for running milestones that don't have recent activity and kill them
    /// </summary>
    private void ProcessLifetimeManager()
    {
        lock (_lock)
        {
            if (!_disposed)
            {
                IOptions<YouRataConfiguration>? options = _webApp.Services.GetService<IOptions<YouRataConfiguration>>();
                if (options == null) return;
                MilestoneLifetimeConfiguration config = options.Value.MilestoneLifetime;
                ILogger<MilestoneLifetimeManager>? logger = _webApp.Services.GetService<ILogger<MilestoneLifetimeManager>>();
                if (logger == null) return;
                foreach (BaseMilestoneIntelligence milestoneIntelligence in _milestoneIntelligence.Milestones)
                {
                    // Only find milestones with condition MilestoneRunning
                    if (milestoneIntelligence.Condition == MilestoneCondition.MilestoneRunning &&
                        milestoneIntelligence.LastUpdate != 0 &&
                        milestoneIntelligence.StartTime != 0 &&
                        milestoneIntelligence.ProcessId != 0)
                    {
                        // Time since last update
                        long dwellTime = DateTimeOffset.Now.ToUnixTimeSeconds() - milestoneIntelligence.LastUpdate;
                        // Time since started
                        long runTime = DateTimeOffset.Now.ToUnixTimeSeconds() - milestoneIntelligence.StartTime;
                        if (dwellTime > config.MaxUpdateDwellTime ||
                            runTime > config.MaxRunTime)
                        {
                            // Try to find the process by the process ID that was registered at activation
                            Process milestoneProcess = Process.GetProcessById(milestoneIntelligence.ProcessId);
                            if (!milestoneProcess.HasExited)
                            {
                                // Kill the process
                                milestoneProcess.Kill();
                                logger.LogWarning($"Milestone {milestoneIntelligence.GetType().Name} was forcefully killed");
                                milestoneIntelligence.Condition = MilestoneCondition.MilestoneFailed;
                            }
                        }
                    }
                }
            }
        }
    }
}
