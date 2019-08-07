using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;

namespace Amg.Build
{
    public class MyTargetsAop
    {
        [Description("Release or Debug")]
        public string Configuration { get; set; }

        public virtual string result { get; set; } = String.Empty;

        [Once]
        protected virtual Builtin.Git Git => Runner.Once<Amg.Build.Builtin.Git>(_ => _.RootDirectory = Runner.RootDirectory());

        [Once]
        protected virtual Builtin.Dotnet Dotnet => Runner.Once<Amg.Build.Builtin.Dotnet>();

        [Once]
        MyTargetsAop Nested => Runner.Once<MyTargetsAop>();

        [Once]
        [Description("Print the dotnet version")]
        public virtual async Task Version()
        {
            await Task.WhenAll(Git.GetVersion(), Dotnet.Version());
            var v = await Dotnet.Version();
            Console.WriteLine(v);
        }

        [Once]        
        [Description("Compile source code")]
        public virtual async Task Compile()
        {
            await Task.CompletedTask;
            Console.WriteLine("compiling...");
            result += "Compile";
        }

        [Once][Description("Link object files")]
        public virtual async Task Link()
        {
            await Compile();
            result += "Link";
        }

        [Once] [Description("Say hello")]
        public virtual async Task<string> SayHello(string name)
        {
            await Task.CompletedTask;
            return $"Hello, {name}";
        }

        [Once] [Description("Pack nuget package")]
        public virtual async Task Pack()
        {
            await Compile();
            await Link();
            result += "Pack";
        }

        [Once] [Description("Compile, link, and pack")] [Default]
        public virtual async Task All()
        {
            await Task.WhenAll(
                Compile(),
                Link(),
                Pack(),
                Version()
                );
        }

        [Once]
        public virtual async Task<int> Times2(int a)
        {
            await Task.CompletedTask;
            args.Add(a);
            return a * 2;
        }

        [Once]
        public virtual async Task<int> Div2(int a)
        {
            await Task.Delay(100);
            return await Times2(a) / 4;
        }

        public IList<int> args = new List<int>();

        [Once]
        public virtual async Task WhatCouldGoWrong()
        {
            await Task.CompletedTask;
            throw new Exception("epic fail");
        }

        [Once]
        public virtual async Task AlwaysFails()
        {
            await WhatCouldGoWrong();
        }
    }
}
