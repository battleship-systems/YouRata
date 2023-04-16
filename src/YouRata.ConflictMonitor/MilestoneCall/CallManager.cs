// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Threading;

namespace YouRata.ConflictMonitor.MilestoneCall;

/// <summary>
/// Provides a way to manage a queue of ConflictMonitorCall objects and coordinate their processing
/// </summary>
/// <remarks>
/// This class is used in WebAppServer
/// </remarks>
internal class CallManager
{
    // Action callbacks from gRPC services
    private readonly ConcurrentQueue<ConflictMonitorCall> _actionCallbacks;

    // Signals when there is an item in the queue for processing
    private readonly AutoResetEvent _actionReady;

    // Signals when the WebAppServer should stop running
    private readonly ManualResetEvent _actionStop;

    public CallManager()
    {
        _actionReady = new AutoResetEvent(false);
        _actionStop = new ManualResetEvent(false);
        _actionCallbacks = new ConcurrentQueue<ConflictMonitorCall>();
    }

    public ConcurrentQueue<ConflictMonitorCall> ActionCallbacks => _actionCallbacks;
    public AutoResetEvent ActionReady => _actionReady;
    public ManualResetEvent ActionStop => _actionStop;

    // Callback function that is executed when an item in the queue is processed
    internal delegate void ConflictMonitorCall(CallHandler handler);
}
