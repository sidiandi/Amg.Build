using Amg.Build;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace amgbuild
{
    internal class Program
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

        static int Main(string[] args)
        {
            return Runner.Run(args);
        }

        static string PathFromName(string name)
        {
            if (name.Contains("."))
            {
                // treat as path
                var path = name.Absolute();
                if (!path.HasExtension(SourceCodeLayout.CmdExtension))
                {
                    throw new ArgumentOutOfRangeException(nameof(name), name, $"Must have {SourceCodeLayout.CmdExtension} extension.");
                }
                return path;
            }
            else
            {
                return (name + SourceCodeLayout.CmdExtension).Absolute();
            }
        }

        [Once]
        [Description("Create an Amg.Build script")]
        public virtual async Task New(string name)
        {
            var path = PathFromName(name);

            var sourceLayout = await Amg.Build.SourceCodeLayout.Create(path);
        }

        [Once]
        [Description("Fix an Amg.Build script")]
        public virtual async Task Fix(string cmdFile)
        {
            if (!cmdFile.HasExtension(SourceCodeLayout.CmdExtension))
            {
                throw new ArgumentOutOfRangeException(nameof(cmdFile), cmdFile, "Must have .cmd extension.");
            }

            if (!cmdFile.IsFile())
            {
                throw new ArgumentOutOfRangeException(nameof(cmdFile), cmdFile, "File not found.");
            }

            var sourceLayout = new SourceCodeLayout(cmdFile);
            await sourceLayout.Fix();
        }
    }
}
