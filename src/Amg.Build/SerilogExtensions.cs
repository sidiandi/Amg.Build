using Serilog;
using System.Runtime.CompilerServices;

namespace Amg.Build
{
    /// <summary>
    /// Utilities for Serilog
    /// </summary>
    public static class SerilogExtensions
    {
        public static void Debug(
            this ILogger logger,
            object x,
            [CallerFilePath] string? sourceFile = null)
        {
            if (logger.IsEnabled(Serilog.Events.LogEventLevel.Debug))
            {
                logger.Debug("{@ToString} {sourceFile}", x.ToString(), sourceFile);
            }
        }
    }
}