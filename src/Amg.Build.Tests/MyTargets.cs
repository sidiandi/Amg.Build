using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Amg.Build
{
    class MyTargets : Targets
    {
        Git Git => DefineTargets(() => new Git());
        Dotnet Dotnet => DefineTargets(() => new Dotnet());

        [Description("Release or Debug")]
        public string Configuration { get; set; }

        public string result { get; private set; } = String.Empty;

        public MyTargets()
        {

        }

        [Description("Print the dotnet version")]
        Target DotnetVersion => DefineTarget(async () =>
        {
            var v = await Dotnet.Version();
            Console.WriteLine(v);
        });

        [Description("Compile source code")]
        Target Compile => DefineTarget(() =>
        {
            Console.WriteLine(GetRootDirectory());
            result += "Compile";
        });

        [Description("Link object files")]
        public Target Link => DefineTarget(async () =>
        {
            await Compile();
            result += "Link";
        });

        [Description("Say hello")]
        public Target<string> SayHello => DefineTarget(() =>
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
            await Task.WhenAll(
                Compile(),
                Link(),
                Pack(),
                DotnetVersion(),
                Git.GetVersion()
                );
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

        Target WhatCouldGoWrong => DefineTarget(() =>
        {
            throw new Exception("epic fail");
        });

        public Target AlwaysFails => DefineTarget(async () =>
        {
            await WhatCouldGoWrong();
        });

    }
}
