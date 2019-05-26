using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Csa.Build
{
    public class Tool
    {
        private readonly string fileName;

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

        public Task Run(params string[] args)
        {
            return Task.Factory.StartNew(() =>
            {
                var p = Process.Start(new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = CreateArgumentsString(args)
                });
                Console.WriteLine($"{p.StartInfo.FileName} {p.StartInfo.Arguments}");

                p.WaitForExit();

                if (p.ExitCode != 0)
                {
                    throw new Exception($"exit code {p.ExitCode}: {p.StartInfo.FileName} {p.StartInfo.Arguments}");
                }
            }, TaskCreationOptions.LongRunning);
        }

        private string CreateArgumentsString(string[] args)
        {
            return String.Join(" ", args.Select(QuoteIfRequired));
        }
    }
}