using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Amg.Build
{
    public class MyBuild
    {
        protected MyBuild(string? result)
        {
            this.result = result ?? String.Empty;
        }

        protected MyBuild()
        {
            this.result = String.Empty;
        }

        [Description("Release or Debug")]
        public string Configuration { get; set; } = "Release";

        public virtual string result { get; set; }

        [Once]
        protected virtual Git Git => Once.Create<Git>(Runner.RootDirectory());

        [Once]
        protected virtual Dotnet Dotnet => Once.Create<Dotnet>();

        [Once]
        MyBuild Nested => Runner.Once<MyBuild>();

        [Once]
        [Description("Print the dotnet version")]
        public virtual async Task Version()
        {
            await Task.WhenAll(Git.GetVersion(), Dotnet.Version());
            var vt = Dotnet.Version();
            var v = await vt;
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
        [Description("Calls a tool that always fails.")]
        public virtual async Task ToolFails()
        {
            await Tools.Cmd.Run("asfkjasdfasdf");
        }

        [Once][Description("Always fails.")]
        public virtual async Task AlwaysFails()
        {
            await WhatCouldGoWrong();
        }
    }
}
