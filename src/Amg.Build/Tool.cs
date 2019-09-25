using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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

        readonly string fileName;
        readonly string[] leadingArguments = new string[] { };
        readonly string workingDirectory = ".";
        readonly int? expectedExitCode = 0;
        readonly IDictionary<string, string> environment = new Dictionary<string, string>();
        readonly string user = null;
        readonly string password = null;
        readonly Action<IRunning, string> onError = new Action<IRunning, string>((r, _) => Console.Error.WriteLine(_));
        readonly Action<IRunning, string> onOutput = new Action<IRunning, string>((r, _) => Console.Out.WriteLine(_));

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
            return With(_ => leadingArguments, leadingArguments.Concat(args).ToArray());
        }

        /// <summary />
        public ITool RunAs(string user, string password)
        {
            return With(_ => _.user, user).With(_ => _.password, password);
        }

        /// <summary>
        /// Adds environment variables.
        /// </summary>
        /// <param name="environmentVariables"></param>
        /// <returns></returns>
        public ITool WithEnvironment(IDictionary<string, string> environmentVariables)
        {
            return With(_ => _.environment, environment.Merge(environmentVariables));
        }

        /// <summary>
        /// Set the working directory for the tool. Default: current directory
        /// </summary>
        /// <param name="workingDirectory"></param>
        /// <returns></returns>
        public ITool WithWorkingDirectory(string workingDirectory)
        {
            return With(_ => _.workingDirectory, workingDirectory);
        }

        /// <summary>
        /// Set the expected exit code. Default: 0
        /// </summary>
        /// <param name="expectedExitCode"></param>
        /// <returns></returns>
        public ITool WithExitCode(int expectedExitCode)
        {
            return With(_ => _.expectedExitCode, expectedExitCode);
        }

        /// <summary>
        /// Disable throwing an exception when the exit code was not as expected.
        /// </summary>
        /// <returns></returns>
        public ITool DoNotCheckExitCode()
        {
            return With(_ => _.expectedExitCode, null);
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
            return With(_ => _.onError, getLineHandler(onError));
        }

        /// <summary />
        public ITool WithOnOutput(Func<Action<IRunning, string>, Action<IRunning, string>> getLineHandler)
        {
            return With(_ => _.onOutput, getLineHandler(onOutput));
        }

        Tool With<T>(Expression<Func<Tool, T>> field, T newValue)
        {
            var t = (Tool)this.MemberwiseClone();
            var fieldInfo = (FieldInfo)((MemberExpression)field.Body).Member;
            fieldInfo.SetValue(t, newValue);
            return t;
        }

        /// <summary />
        public ITool WithFileName(string fileName)
        {
            return With(_ => _.fileName, fileName);
        }
    }
}