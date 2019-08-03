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

    /// <summary>
    /// Command line options
    /// </summary>
    /// <typeparam name="TargetsDerivedClass"></typeparam>
    public class Options<TargetsDerivedClass>
    {
        /// <summary>
        /// Class with the targets methods
        /// </summary>
        public TargetsDerivedClass targets { get; set; }

        /// <summary />
        public Options(TargetsDerivedClass targets)
        {
            this.targets = targets;
        }

        /// <summary />
        [Operands]
        [Description("Target name and arguments")]
        public string[] TargetAndArguments { get; set; } = new string[] { };

        /// <summary />
        [Short('h'), Description("Show help and exit")]
        public bool Help { get; set; }

        /// <summary />
        [Description("Force a rebuild of the build script")]
        public bool Clean { get; set; }

        /// <summary />
        [Description("Ignore --clean (internal use only)")]
        public bool IgnoreClean { get; set; }

        /// <summary />
        [Short('v'), Description("Set the verbosity level.")]
        public Verbosity Verbosity { get; set; } = Verbosity.Normal;
    }

}
