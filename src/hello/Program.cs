using System.Threading.Tasks;
using System;
using Amg.Build;
using Amg.Extensions;
using System.Linq;
using System.ComponentModel;
using Cake.Common.IO;
using Amg.FileSystem;
using Amg.GetOpt;

namespace hello
{
    public class Program
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

        static int Main(string[] args) => Runner.Run(args);

        [Once]
        protected virtual Cake.Core.ICakeContext Cake => Amg.Build.Cake.Cake.CreateContext();

        [Once, Description("Demo of the Cake adapter")]
        public virtual void WorkWithCakeAddins()
        {
            Cake.Zip(
                Runner.RootDirectory().Combine("hello"), 
                Runner.RootDirectory().Combine("out", "z.zip").EnsureParentDirectoryExists());
        }

        [Once, Description("Greet someone.")]
        public virtual async Task Greet(string name)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            Logger.Information($"Hello, {name}");

            Console.WriteLine($"Hello, {name}");
        }

        [Once, Description("Greet all.")]
        public virtual async Task GreetAll()
        {
            await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => Greet($"Alice {_}")).ToArray());
            await Greet(Enumerable.Range(0, 100).Select(_ => "Very long name ").Join());
        }

        [Once, Default, Description("Greet Alice.")]
        public virtual async Task Default()
        {
            await Greet("Alice");
        }

        [Once, Description("Simulate a failing tool")]
        public virtual async Task FailTool()
        {
            await Tools.Cmd.Run("/c", "fasdfasdfasd");
        }

        [Once, Description("Runs forever")]
        public virtual async Task RunForever()
        {
            await Tools.Default.WithFileName("cmd")
                .Run();
        }

        [Once, Description("Use failing tool")]
        public virtual async Task UseFailingTool()
        {
            await Task.WhenAll(FailTool(), RunForever());
        }
    }
}