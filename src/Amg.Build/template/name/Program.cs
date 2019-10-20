using System;
using System.Threading.Tasks;
using System.ComponentModel;
using Amg.GetOpt;
using Amg.Build;

namespace ReplaceWithName
{
    public class Program
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);
	
	    static int Main(string[] args) => Runner.Run(args);

	    [Once, Description("example command line option")]
	    public virtual string ExampleOption {get; set;}
	
	    [Once, Description("example action")]
	    public virtual async Task ExampleAction()
	    {
		    Console.WriteLine($"ExampleAction. ExampleOption: {ExampleOption}.");
		    await Task.CompletedTask;
	    }
	
	    [Once, Default, Description("Example default action")]
	    public virtual async Task Default()
	    {
		    Console.WriteLine("Hello, World!");
		    await Task.CompletedTask;
	    }
    }
}
