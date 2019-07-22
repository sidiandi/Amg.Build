using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Amg.Build
{
    public class Tool
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly string fileName;
        private string[] leadingArguments = new string[] { };
        private string workingDirectory = ".";

        public Tool WithArguments(params string[] args)
        {
            return WithArguments((IEnumerable<string>)args);
        }

        public Tool WithArguments(IEnumerable<string> args)
        {
            var t = (Tool) this.MemberwiseClone();
            t.leadingArguments = leadingArguments.Concat(args).ToArray();
            return t;
        }

        public Tool WithWorkingDirectory(string workingDirectory)
        {
            var t = (Tool)this.MemberwiseClone();
            t.workingDirectory = workingDirectory;
            return t;
        }

        public Tool(string fileName)
        {
            this.fileName = fileName;
        }

        static string QuoteIfRequired(string x)
        {
            return x.Any(Char.IsWhiteSpace)
                ? x.Quote()
                : x;
        }

        class ResultImpl : IToolResult
        {
            public int ExitCode { get; set; }
            public string Output { get; set; }
            public string Error { get; set; }
        }

        public Task<IToolResult> Run(params string[] args)
        {
            return Task.Factory.StartNew(() =>
            {
                var p = Process.Start(new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = CreateArgumentsString(leadingArguments.Concat(args)),
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    WorkingDirectory = workingDirectory
                });

                var processLog = Serilog.Log.Logger.ForContext("pid", p.Id);

                processLog.Information("process started: {FileName} {Arguments}", p.StartInfo.FileName, p.StartInfo.Arguments);

                var output = p.StandardOutput.Tee(_ => processLog.Information(_)).ReadToEndAsync();
                var error = p.StandardError.Tee(_ => processLog.Error(_)).ReadToEndAsync();

                p.WaitForExit();
                processLog.Information("process exited with {ExitCode}: {FileName} {Arguments}", p.ExitCode, p.StartInfo.FileName, p.StartInfo.Arguments);

                var result = (IToolResult) new ResultImpl
                {
                    ExitCode = p.ExitCode,
                    Error = error.Result,
                    Output = output.Result
                };

                if (p.ExitCode != 0)
                {
                    throw new ToolException($"exit code {p.ExitCode}: {p.StartInfo.FileName} {p.StartInfo.Arguments}", result);
                }

                return result;

            }, TaskCreationOptions.LongRunning);
        }

        private string CreateArgumentsString(IEnumerable<string> args)
        {
            return String.Join(" ", args.Select(QuoteIfRequired));
        }
    }
}