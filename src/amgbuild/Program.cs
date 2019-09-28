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
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static int Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("run"))
            {
                return Runner.Once<Program>().Run(args).Result;
            }
            else
            {
                return Runner.Run(args);
            }
        }

        [Once]
        [Description("Create an Amg.Build script")]
        public virtual async Task Init(string name)
        {
            var path = name.EndsWith(".cmd")
                ? name.Absolute()
                : (name + ".cmd").Absolute();

            var sourceLayout = await Amg.Build.SourceCodeLayout.Create(path);
        }

        [Once] virtual protected Dotnet Dotnet => Runner.Once<Dotnet>();

        [Once]
        [Description("Run a dotnet project")]
        public virtual async Task<int> Run(string[] dotnetArgs)
        {
            var dotnet = await Dotnet.Tool();
            var r = await dotnet.Run(dotnetArgs);
            return r.ExitCode;
        }
    }
}
