using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Csa.Build
{
    class MyTargetsNoDefault : Targets
    {
        [Description("Release or Debug")]
        public string Configuration { get; set; }

        public string result { get; private set; } = String.Empty;

        [Description("Compile source code")]
        Target Compile => DefineTarget(() =>
        {
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

    class MyTargets : Targets
    {
        [Description("Release or Debug")]
        public string Configuration { get; set; }

        public string result { get; private set; } = String.Empty;

        [Description("Compile source code")]
        public Target Compile => DefineTarget(() =>
        {
            result += "Compile";
        });

        [Description("Link object files")]
        public Target Link => DefineTarget(async () =>
        {
            await Compile();
            result += "Link";
        });

        [Description("Say hello")]
        public Target<string> SayHello => DefineTarget(async () =>
        {
            return "Hello";
        });

        [Description("Pack nuget package")]
        public Target Pack => DefineTarget(async () =>
        {
            await Compile();
            await Link();
            result += "Pack";
        });

        [Description("Compile, link, and pack")]
        public Target Default => DefineTarget(async () =>
        {
            await Compile();
            await Link();
            await Pack();
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
