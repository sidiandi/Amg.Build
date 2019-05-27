using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Csa.Build
{
    public class Tool
    {
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

        public interface IResult
        {
            int ExitCode { get; }
            string Output { get; }
            string Error { get; }
        }

        class ResultImpl : IResult
        {
            public int ExitCode { get; set; }
            public string Output { get; set; }
            public string Error { get; set; }
        }

        public Task<IResult> Run(params string[] args)
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
                Console.WriteLine($"{p.StartInfo.FileName} {p.StartInfo.Arguments}");

                var output = p.StandardOutput.ReadToEndAsync();
                var error = p.StandardError.ReadToEndAsync();

                p.WaitForExit();

                if (p.ExitCode != 0)
                {
                    throw new Exception($"exit code {p.ExitCode}: {p.StartInfo.FileName} {p.StartInfo.Arguments}");
                }

                return (IResult) new ResultImpl
                {
                    ExitCode = p.ExitCode,
                    Error = error.Result,
                    Output = output.Result
                };

            }, TaskCreationOptions.LongRunning);
        }

        private string CreateArgumentsString(IEnumerable<string> args)
        {
            return String.Join(" ", args.Select(QuoteIfRequired));
        }
    }
}