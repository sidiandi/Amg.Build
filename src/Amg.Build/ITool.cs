using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        /// File name of the executable.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        ITool WithFileName(string fileName);

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

        /// <summary>
        /// Start the process under a certain user account
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        ITool RunAs(string user, string password);

        /// <summary>
        /// Call for lineHandler every line of error
        /// </summary>
        /// <param name="lineHandler"></param>
        /// <returns></returns>
        ITool OnError(Action<IRunning, string> lineHandler);

        /// <summary>
        /// Call lineHandler for every line out output
        /// </summary>
        /// <param name="lineHandler"></param>
        /// <returns></returns>
        ITool OnOutput(Action<IRunning, string> lineHandler);
    }

    /// <summary>
    /// Information about a running tool
    /// </summary>
    public interface IRunning
    {
        /// <summary>
        /// Underlying process
        /// </summary>
        Process Process { get; }
    }

    /// <summary>
    /// Convenience extensions for ITool
    /// </summary>
    public static class IToolExtensions
    {
        /// <summary>
        /// Add a single environment variable.
        /// </summary>
        /// <param name="tool"></param>
        /// <param name="name">Name of the environment variable</param>
        /// <param name="value">Value of the envrionment variable</param>
        /// <returns>tool</returns>
        public static ITool WithEnvironment(this ITool tool, string name, string value)
        {
            return tool.WithEnvironment(new Dictionary<string, string> { { name, value } });
        }
    }
}