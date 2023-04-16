// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using YouRata.Common;

namespace YouRata.ConflictMonitor.MilestoneInterface.WebHost;

/// <summary>
/// Extension methods used in configuring a web host to listen on a Unix domain socket
/// </summary>
internal static class WebHostExtensions
{
    public static void AddUnixSocket(this ConfigureWebHostBuilder builder)
    {
        builder.ConfigureKestrel(opt =>
        {
            // A file will persist for all sockets
            if (File.Exists(YouRataConstants.GrpcUnixSocketPath))
            {
                File.Delete(YouRataConstants.GrpcUnixSocketPath);
            }

            opt.ListenUnixSocket(YouRataConstants.GrpcUnixSocketPath);
        });
    }
}
