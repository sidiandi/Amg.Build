using System;
using System.ComponentModel;
using Serilog.Events;

namespace Amg.CommandLine
{
    class RunnerOptions
    {
        public object? Options { get; set; }

        [Short('h'), Description("Print help and exit.")]
        public bool Help { get; set; } = false;

        [Description("Print version and exit.")]
        public bool Version { get; set; } = false;

        [Description("Show tool web page and exit.")]
        public bool About { get; set; } = false;

        [Short('v'), Description("Increase verbosity. Can be used multiple times as in -vvvv.")]
        public bool Verbose
        {
            set
            {
                var v = Verbosity - 1;
                if (System.Enum.IsDefined(typeof(LogEventLevel), v))
                {
                    Verbosity = v;
                }
            }
        }

        [Description("Logging verbosity. Default: Fatal")]
        public LogEventLevel Verbosity { get; set; } = LogEventLevel.Fatal;
    }
}
