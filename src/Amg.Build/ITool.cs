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
        /// Call a function for every line of error
        /// </summary>
        /// <param name="getLineHandler">Create the new line handler given the old one.</param>
        /// <returns></returns>
        ITool WithOnError(Func<Action<IRunning, string>, Action<IRunning, string>> getLineHandler);

        /// <summary>
        /// Call a function for every line of output
        /// </summary>
        /// <param name="getLineHandler">Create the new line handler given the old one.</param>
        /// <returns></returns>
        ITool WithOnOutput(Func<Action<IRunning, string>, Action<IRunning, string>> getLineHandler);
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

        /// <summary>
        /// Disable OnError and OnOutput handlers
        /// </summary>
        /// <param name="tool"></param>
        /// <returns></returns>
        public static ITool Silent(this ITool tool)
        {
            return tool
                .WithOnOutput(old => (r, l) => { })
                .WithOnError(old => (r, l) => { });
        }

        /// <summary>
        /// Write output and error to output
        /// </summary>
        /// <param name="tool"></param>
        /// <returns></returns>
        public static ITool IgnoreError(this ITool tool)
        {
            return tool
                .WithOnOutput(old => (r, l) => { })
                .WithOnError(old => (r, l) => { });
        }

        /// <summary>
        /// write output and error to the console
        /// </summary>
        /// <param name="tool"></param>
        /// <returns></returns>
        public static ITool Passthrough(this ITool tool)
        {
            return tool
                .WithOnOutput(old => (r, _) => Console.WriteLine(_))
                .WithOnError(old => (r, _) => Console.Error.WriteLine(_));
        }

        /// <summary>
        /// Like Passthrough, but prepend the process ID to each line
        /// </summary>
        /// <param name="tool"></param>
        /// <returns></returns>
        public static ITool PrependProcessId(this ITool tool)
        {
            return tool
                .WithOnOutput(old => (r, _) => Console.WriteLine($"{r.Process.Id}:{_}"))
                .WithOnError(old => (r, _) => Console.Error.WriteLine($"{r.Process.Id}:{_}"));
        }
    }
}