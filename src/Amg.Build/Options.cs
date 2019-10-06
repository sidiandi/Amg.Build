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

    class SourceOptions
    {
        /// <summary />
        [Short('e'), Description("Edit the script in Visual Studio.")]
        public bool Edit { get; set; }

        /// <summary />
        [Description("Debug the script in Visual Studio.")]
        public bool Debug { get; set; }

        /// <summary />
        [Description("Force a rebuild of the build script")]
        public bool Clean { get; set; }

        /// <summary />
        [Description("watch parent directory of .cmd file")]
        public bool Watch { get; set; }
    }

    /// <summary>
    /// Command line options
    /// </summary>
    class Options
    {
        /// <summary />
        [Operands]
        [Description("command name and arguments")]
        public string[] TargetAndArguments { get; set; } = new string[] { };

        /// <summary />
        [Short('h'), Description("Show help and exit")]
        public bool Help { get; set; }

        /// <summary />
        [Short('v'), Description("Set the verbosity level.")]
        public Verbosity Verbosity { get; set; } = Verbosity.Normal;

        [Description("show summary")]
        public bool Summary { get; set; }

        [Description("visualize build result")]
        public bool AsciiArt { get; set; }
    }

    class CombinedOptions
    {
        public CombinedOptions(object onceProxy)
        {
            OnceProxy = onceProxy;
        }
        public object OnceProxy { get; }
        public Options Options { get; } = new Options();
        public SourceOptions? SourceOptions { get; set; }
    }
}
