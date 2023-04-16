// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YouRata.Common;
using YouRata.Common.Configuration;
using YouRata.Common.GitHub;
using YouRata.ConflictMonitor.ActionReport;
using YouRata.ConflictMonitor.MilestoneCall;
using YouRata.ConflictMonitor.MilestoneCall.Messages;
using YouRata.ConflictMonitor.MilestoneData;
using YouRata.ConflictMonitor.MilestoneInterface.ConfigurationValidation;
using YouRata.ConflictMonitor.MilestoneInterface.Services;
using YouRata.ConflictMonitor.MilestoneInterface.WebHost;
using YouRata.ConflictMonitor.MilestoneProcess;
using YouRata.ConflictMonitor.Workflow;
using static YouRata.ConflictMonitor.MilestoneCall.CallManager;

namespace YouRata.ConflictMonitor.MilestoneInterface;

/// <summary>
/// A server for for use with milestone program communication
/// </summary>
internal class WebAppServer
{
    private readonly PreviousActionReportProvider _actionReportProvider;
    private readonly CallHandler _callHandler;
    private readonly CallManager _callManager;
    private readonly ConfigurationHelper _configurationHelper;
    private readonly ConflictMonitorWorkflow _conflictMonitorWorkflow;
    private readonly GitHubEnvironmentBuilder _environmentHelper;
    private readonly InServiceLoggerProvider _loggerProvider;
    private readonly MilestoneIntelligenceRegistry _milestoneIntelligence;

    public WebAppServer(CallHandler callHandler, InServiceLoggerProvider loggerProvider, ConfigurationHelper configurationHelper,
        GitHubEnvironmentBuilder environmentHelper, ConflictMonitorWorkflow conflictMonitorWorkflow,
        MilestoneIntelligenceRegistry milestoneIntelligence, PreviousActionReportProvider actionReportProvider)
    {
        _callHandler = callHandler;
        _callManager = new CallManager();
        _loggerProvider = loggerProvider;
        _configurationHelper = configurationHelper;
        _environmentHelper = environmentHelper;
        _conflictMonitorWorkflow = conflictMonitorWorkflow;
        _milestoneIntelligence = milestoneIntelligence;
        _actionReportProvider = actionReportProvider;
    }

    /// <summary>
    /// Start the server
    /// </summary>
    /// <returns></returns>
    internal async Task RunAsync()
    {
        WebApplication webApp = BuildApp();
        webApp.MapGrpcService<ActionIntelligenceMessage>();
        webApp.MapGrpcService<MilestoneActionIntelligenceMessage>();
        webApp.MapGrpcService<LogMessage>();

        // Validate all configurations
        await webApp.ValidateConfigurationAsync().ConfigureAwait(false);

        // Start the web app asynchronously
        await webApp.StartAsync().ConfigureAwait(false);

        // The main loop of ConflictMonitor
        using (MilestoneLifetimeManager lifetimeManager = new MilestoneLifetimeManager(webApp, _milestoneIntelligence))
        {
            // Start checking for inert milestones
            lifetimeManager.StartLoop();
            while (true)
            {
                // Wait until signaled to process an action
                _callManager.ActionReady.WaitOne();
                while (!_callManager.ActionCallbacks.IsEmpty)
                {
                    // Get the ConflictMonitorCall callback function from the queue
                    _callManager.ActionCallbacks.TryDequeue(out ConflictMonitorCall? call);
                    if (call != null)
                    {
                        // Execute the function
                        call(_callHandler);
                    }
                }
                // Check if ConflictMonitor was signaled to stop
                if (_callManager.ActionStop.WaitOne(0))
                {
                    // Stop the server
                    await webApp.StopAsync().ConfigureAwait(false);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Builds a WebApplication with gRPC services
    /// </summary>
    /// <returns></returns>
    private WebApplication BuildApp()
    {
        IConfiguration appConfig = _configurationHelper.Build();
        IConfiguration environmentConfig = _environmentHelper.Build();
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddGrpc(opt =>
        {
            opt.EnableDetailedErrors = true;
            opt.ResponseCompressionLevel = YouRataConstants.GrpcResponseCompressionLevel;
        });
        builder.WebHost.AddUnixSocket();
        builder.Logging.Services.AddSingleton<ILoggerProvider, InServiceLoggerProvider>(service => _loggerProvider);
        builder.Services.AddAppConfiguration(appConfig);
        builder.Services.AddGitHubEnvironment(environmentConfig);
        builder.Services.AddSingleton<ConflictMonitorWorkflow>(service => _conflictMonitorWorkflow);
        builder.Services.AddSingleton<MilestoneIntelligenceRegistry>(service => _milestoneIntelligence);
        builder.Services.AddSingleton<PreviousActionReportProvider>(service => _actionReportProvider);
        builder.Services.AddSingleton<CallManager>(service => _callManager);
        WebApplication app = builder.Build();
        return app;
    }
}
