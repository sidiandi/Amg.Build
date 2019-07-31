using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Amg.Build
{
    /// <summary>
    /// Start a command line tool.
    /// </summary>
    /// Immutable. To customize, use the With... methods.
    public class Tool : ITool
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly string fileName;
        private string[] leadingArguments = new string[] { };
        private string workingDirectory = ".";
        private int? expectedExitCode = 0;
        private IDictionary<string, string> environment = new Dictionary<string, string>();

        /// <summary>
        /// Prepends arguments to every Run call.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public Tool WithArguments(params string[] args)
        {
            return WithArguments((IEnumerable<string>)args);
        }

        /// <summary>
        /// Prepends arguments to every Run call.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public Tool WithArguments(IEnumerable<string> args)
        {
            var t = (Tool)this.MemberwiseClone();
            t.leadingArguments = leadingArguments.Concat(args).ToArray();
            return t;
        }

        /// <summary>
        /// Adds environment variables.
        /// </summary>
        /// <param name="environmentVariables"></param>
        /// <returns></returns>
        public Tool WithEnvironment(IDictionary<string, string> environmentVariables)
        {
            var t = (Tool)this.MemberwiseClone();
            t.environment = environment.Merge(environmentVariables);
            return t;
        }

        /// <summary>
        /// Set the working directory for the tool. Default: current directory
        /// </summary>
        /// <param name="workingDirectory"></param>
        /// <returns></returns>
        public Tool WithWorkingDirectory(string workingDirectory)
        {
            var t = (Tool)this.MemberwiseClone();
            t.workingDirectory = workingDirectory;
            return t;
        }

        /// <summary>
        /// Set the expected exit code. Default: 0
        /// </summary>
        /// <param name="expectedExitCode"></param>
        /// <returns></returns>
        public Tool WithExitCode(int expectedExitCode)
        {
            var t = (Tool)this.MemberwiseClone();
            t.expectedExitCode = expectedExitCode;
            return t;
        }

        /// <summary>
        /// Disable throwing an exception when the exit code was not as expected.
        /// </summary>
        /// <returns></returns>
        public Tool DoNotCheckExitCode()
        {
            var t = (Tool)this.MemberwiseClone();
            t.expectedExitCode = null;
            return t;
        }

        /// <summary>
        /// Create a tool. 
        /// </summary>
        /// <param name="executableFileName">.exe or .cmd file. Relative paths are resolved to current directory and PATH environment.</param>
        public Tool(string executableFileName)
        {
            this.fileName = executableFileName;
        }

        class ResultImpl : IToolResult
        {
            public int ExitCode { get; set; }
            public string Output { get; set; }
            public string Error { get; set; }
        }

        /// <summary>
        /// Runs the tool
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public Task<IToolResult> Run(params string[] args)
        {
            return Task.Factory.StartNew(() =>
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = CreateArgumentsString(leadingArguments.Concat(args)),
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    WorkingDirectory = workingDirectory,
                };

                startInfo.EnvironmentVariables.Add(this.environment);

                var p = Process.Start(startInfo);

                var processLog = Serilog.Log.Logger.ForContext("pid", p.Id);

                processLog.Information("process started: {FileName} {Arguments}", p.StartInfo.FileName, p.StartInfo.Arguments);

                var output = p.StandardOutput.Tee(_ => processLog.Information(_)).ReadToEndAsync();
                var error = p.StandardError.Tee(_ => processLog.Error(_)).ReadToEndAsync();

                p.WaitForExit();
                processLog.Information("process exited with {ExitCode}: {FileName} {Arguments}", p.ExitCode, p.StartInfo.FileName, p.StartInfo.Arguments);

                var result = (IToolResult)new ResultImpl
                {
                    ExitCode = p.ExitCode,
                    Error = error.Result,
                    Output = output.Result
                };

                if (expectedExitCode != null)
                {
                    if (p.ExitCode != expectedExitCode.Value)
                    {
                        throw new ToolException($"exit code {p.ExitCode}, was expecting {expectedExitCode.Value}: {p.StartInfo.FileName} {p.StartInfo.Arguments}", result);
                    }
                }

                return result;

            }, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// Creates an argument string for Process.StartInfo.Arguments
        /// </summary>
        /// Provided as a convenience for use outside of this class.
        /// Quotes arguments with whitespace
        /// <param name="args"></param>
        /// <returns></returns>
        public static string CreateArgumentsString(IEnumerable<string> args)
        {
            return String.Join(" ", args.Select(_ => _.QuoteIfRequired()));
        }
    }
}