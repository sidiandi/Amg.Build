using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Amg.Build
{
    class MyTargetsNoDefault : Targets
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        Git Git = new Git();
        Dotnet Dotnet = new Dotnet();

        [Description("Release or Debug")]
        public string Configuration { get; set; }

        public string result { get; private set; } = String.Empty;

        [Description("Compile source code")]
        Target Compile => DefineTarget(async () =>
        {
            var v = await Git.GetVersion();
            Logger.Information("Using version {version}", v);
            result += "Compile";
        });

        [Description("Link object files")]
        public Target Link => DefineTarget(async () =>
        {
            await Compile();
            result += "Link";
        });

        [Description("Pack nuget package")]
        public Target Pack => DefineTarget(async () =>
        {
            await Compile();
            await Link();
            result += "Pack";
        });

        Target<int, int> Times2 => DefineTarget((int a) =>
        {
            args.Add(a);
            return a * 2;
        });

        public Target<int, int> Div2 => DefineTarget(async (int a) =>
        {
            await Task.Delay(100);
            return await Times2(a) / 4;
        });

        public IList<int> args = new List<int>();

    }
}
