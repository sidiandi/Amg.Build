using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security;
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

        private string fileName;
        private string[] leadingArguments = new string[] { };
        private string workingDirectory = ".";
        private int? expectedExitCode = 0;
        private IDictionary<string, string> environment = new Dictionary<string, string>();
        private string user;
        private string password;
        private Action<IRunning, string> onError = new Action<IRunning, string>((r, _) => Console.Error.WriteLine(_));
        private Action<IRunning, string> onOutput = new Action<IRunning, string>((r, _) => Console.Out.WriteLine(_));

        /// <summary>
        /// Prepends arguments to every Run call.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public ITool WithArguments(params string[] args)
        {
            return WithArguments((IEnumerable<string>)args);
        }

        /// <summary>
        /// Prepends arguments to every Run call.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public ITool WithArguments(IEnumerable<string> args)
        {
            var t = (Tool)this.MemberwiseClone();
            t.leadingArguments = leadingArguments.Concat(args).ToArray();
            return t;
        }

        /// <summary />
        public ITool RunAs(string user, string password)
        {
            var t = (Tool)this.MemberwiseClone();
            t.user = user;
            t.password = password;
            return t;
        }

        /// <summary>
        /// Adds environment variables.
        /// </summary>
        /// <param name="environmentVariables"></param>
        /// <returns></returns>
        public ITool WithEnvironment(IDictionary<string, string> environmentVariables)
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
        public ITool WithWorkingDirectory(string workingDirectory)
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
        public ITool WithExitCode(int expectedExitCode)
        {
            var t = (Tool)this.MemberwiseClone();
            t.expectedExitCode = expectedExitCode;
            return t;
        }

        /// <summary>
        /// Disable throwing an exception when the exit code was not as expected.
        /// </summary>
        /// <returns></returns>
        public ITool DoNotCheckExitCode()
        {
            var t = (Tool)this.MemberwiseClone();
            t.expectedExitCode = null;
            return t;
        }

        /// <summary>
        /// Create a tool. 
        /// </summary>
        /// <param name="executableFileName">.exe or .cmd file. Relative paths are resolved to current directory and PATH environment.</param>
        [Obsolete("Use Tool.Default")]
        public Tool(string executableFileName)
        {
            this.fileName = executableFileName;
        }

        internal Tool()
        {
        }

        class ResultImpl : IToolResult
        {
            public int ExitCode { get; set; }
            public string Output { get; set; }
            public string Error { get; set; }
        }

        static SecureString GetSecureString(string password)
        {
            var s = new SecureString();
            foreach (var i in password)
            {
                s.AppendChar(i);
            }
            return s;
        }

        class Running : IRunning
        {
            private Process process;

            public Running(Process p)
            {
                this.process = p;
            }

            public Process Process => process;

            public override string ToString() => Process.Id.ToString();
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
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                if (user != null)
                {
                    var userParts = user.Split('\\');
                    var userName = userParts.Last();
                    if (userParts.Length >= 2)
                    {
                        var domain = userParts[0];
                        startInfo.Domain = domain;
                    }
                    startInfo.UserName = userName;
                    startInfo.Password = GetSecureString(password);
                }

                startInfo.EnvironmentVariables.Add(this.environment);

                Process Start()
                {
                    try
                    {
                        return Process.Start(startInfo);
                    }
                    catch (System.ComponentModel.Win32Exception e)
                    {
                        if ((uint)e.HResult == 0x80004005)
                        {
                            throw new ToolStartFailed(e.Message, startInfo);
                        }
                        throw;
                    }
                }

                var p = Start();

                var processLog = Serilog.Log.Logger.ForContext("pid", p.Id);

                processLog.Information("process {Id} started: {FileName} {Arguments}", p.Id, p.StartInfo.FileName, p.StartInfo.Arguments);

                var running = new Running(p);
                var output = p.StandardOutput.Tee(_ => onOutput(running, _)).ReadToEndAsync();
                var error = p.StandardError.Tee(_ => onError(running, _)).ReadToEndAsync();

                p.WaitForExit();
                processLog.Information("process {Id} exited with {ExitCode}: {FileName} {Arguments}", p.Id, p.ExitCode, p.StartInfo.FileName, p.StartInfo.Arguments);

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
                        throw new ToolException(
                            $"process exited with {p.ExitCode}, but {expectedExitCode.Value} was expected.", 
                            result, startInfo);
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

        /// <summary />
        public ITool WithOnError(Func<Action<IRunning, string>, Action<IRunning, string>> getLineHandler)
        {
            var t = (Tool)this.MemberwiseClone();
            t.onError = getLineHandler(onError);
            return t;
        }

        /// <summary />
        public ITool WithOnOutput(Func<Action<IRunning, string>, Action<IRunning, string>> getLineHandler)
        {
            var t = (Tool)this.MemberwiseClone();
            t.onOutput = getLineHandler(onOutput);
            return t;
        }

        /// <summary />
        public ITool WithFileName(string fileName)
        {
            var t = (Tool)this.MemberwiseClone();
            t.fileName = fileName;
            return t;
        }
    }
}