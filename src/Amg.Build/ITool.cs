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
        ITool DoNotCheckExitCode();
        /// <summary>
        /// Prepends arguments to every Run call.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        ITool WithArguments(IEnumerable<string> args);
        /// <summary>
        /// Prepends arguments to every Run call.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        ITool WithArguments(params string[] args);
        /// <summary>
        /// Adds environment variables.
        /// </summary>
        /// <param name="environmentVariables"></param>
        /// <returns></returns>
        ITool WithEnvironment(IDictionary<string, string> environmentVariables);
        /// <summary>
        /// Set the expected exit code. Default: 0
        /// </summary>
        /// <param name="expectedExitCode"></param>
        /// <returns></returns>
        ITool WithExitCode(int expectedExitCode);
        /// <summary>
        /// Set the working directory for the tool. Default: current directory
        /// </summary>
        /// <param name="workingDirectory"></param>
        /// <returns></returns>
        ITool WithWorkingDirectory(string workingDirectory);
    }
}