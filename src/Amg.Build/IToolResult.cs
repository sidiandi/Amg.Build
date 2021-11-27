namespace Amg.Build
{
    /// <summary>
    /// Result of running a command line tool
    /// </summary>
    public interface IToolResult
    {
        /// <summary>
        /// Process exit code of the tool
        /// </summary>
        int ExitCode { get; }

        /// <summary>
        /// stdout output
        /// </summary>
        string Output { get; }

        /// <summary>
        /// stderr output
        /// </summary>
        string Error { get; }
    }
}