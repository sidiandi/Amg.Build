using Amg.CommandLine;
using System.ComponentModel;

namespace Amg.Build
{
    /// <summary>
    /// Logging verbosity level
    /// </summary>
    public enum Verbosity
    {
        /// <summary />
        Quiet,
        /// <summary />
        Minimal,
        /// <summary />
        Normal,
        /// <summary />
        Detailed
    };

    class OptionsWithSource : Options
    {
        public OptionsWithSource(object targets)
            : base(targets)
        {
        }

        /// <summary />
        [Short('e'), Description("Edit the build script in Visual Studio.")]
        public bool Edit { get; set; }

        /// <summary />
        [Description("Force a rebuild of the build script")]
        public bool Clean { get; set; }

        /// <summary />
        [Description("Ignore --clean (internal use only)")]
        public bool IgnoreClean { get; set; }

        /// <summary />
        [Description("Fix .cmd and .csproj files.")]
        public bool Fix { get; set; }
    }

    /// <summary>
    /// Command line options
    /// </summary>
    class Options
    {
        /// <summary>
        /// Class with the targets methods
        /// </summary>
        public object Targets { get; private set; }

        /// <summary />
        public Options(object targets)
        {
            this.Targets = targets;
        }

        /// <summary />
        [Operands]
        [Description("Target name and arguments")]
        public string[] TargetAndArguments { get; set; } = new string[] { };

        /// <summary />
        [Short('h'), Description("Show help and exit")]
        public bool Help { get; set; }

        /// <summary />
        [Short('v'), Description("Set the verbosity level.")]
        public Verbosity Verbosity { get; set; } = Verbosity.Normal;

        [Description("show summary")]
        public bool Summary { get; set; }
    }
}
