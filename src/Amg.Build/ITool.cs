using System.Collections.Generic;
using System.Threading.Tasks;

namespace Amg.Build
{
    /// <summary>
    /// Command line tool wrapper
    /// </summary>
    public interface ITool
    {
        /// <summary>
        /// Runs the command line tool
        /// </summary>
        /// <param name="args">Command line argument. One string per argument. Will be quoted automatically (i.e. when a argument contains whitespace)</param>
        /// <returns></returns>
        Task<IToolResult> Run(params string[] args);

        /// <summary>
        /// Disable throwing an exception when the exit code was not as expected.
        /// </summary>
        /// <returns></returns>
        Tool DoNotCheckExitCode();
        /// <summary>
        /// Prepends arguments to every Run call.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        Tool WithArguments(IEnumerable<string> args);
        /// <summary>
        /// Prepends arguments to every Run call.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        Tool WithArguments(params string[] args);
        /// <summary>
        /// Adds environment variables.
        /// </summary>
        /// <param name="environmentVariables"></param>
        /// <returns></returns>
        Tool WithEnvironment(IDictionary<string, string> environmentVariables);
        /// <summary>
        /// Set the expected exit code. Default: 0
        /// </summary>
        /// <param name="expectedExitCode"></param>
        /// <returns></returns>
        Tool WithExitCode(int expectedExitCode);
        /// <summary>
        /// Set the working directory for the tool. Default: current directory
        /// </summary>
        /// <param name="workingDirectory"></param>
        /// <returns></returns>
        Tool WithWorkingDirectory(string workingDirectory);

    }
}