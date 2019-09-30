using Serilog;

namespace Amg.Build
{
    /// <summary>
    /// Utilities for Serilog
    /// </summary>
    public static class SerilogExtensions
    { 
        /// <summary>
        /// Log an object
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="x"></param>
        public static void Dump(this ILogger logger, object x)
        {
            logger.Information("{@x}", x);
        }
    }
}
