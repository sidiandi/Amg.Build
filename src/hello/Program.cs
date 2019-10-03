using System.Threading.Tasks;
using System;
using Amg.Build;
using System.Linq;
using System.ComponentModel;

namespace hello
{
    public class Program
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

        static int Main(string[] args) => Runner.Run(args);

        [Once]
        [Description("Greet someone.")]
        public virtual async Task Greet(string name)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            Logger.Information($"Hello, {name}");

            Console.WriteLine($"Hello, {name}");
        }

        [Once]
        [Description("Greet all.")]
        public virtual async Task GreetAll()
        {
            await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => Greet($"Alice {_}")).ToArray());
            await Greet(Enumerable.Range(0, 100).Select(_ => "Very long name ").Join());
        }

        [Once, Default]
        public virtual async Task Default()
        {
            await Greet("Alice");
        }

        [Once]
        [Description("Simulate a failing tool")]
        public virtual async Task FailTool()
        {
            await Tools.Cmd.Run("/c", "fasdfasdfasd");
        }

        [Once]
        [Description("Runs forever")]
        public virtual async Task RunForever()
        {
            await Tools.Default.WithFileName("cmd")
                .Run();
        }

        [Once]
        [Description("Use failing tool")]
        public virtual async Task UseFailingTool()
        {
            await Task.WhenAll(FailTool(), RunForever());
        }
    }

}