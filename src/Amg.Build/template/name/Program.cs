using System;
using System.Threading.Tasks;
using Amg.GetOpt;
using Amg.Build;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace ReplaceWithNamespace
{
    public class Program
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);
	
	    static int Main(string[] args) => Runner.Run(args);

		[Once, Default, Description("Default action")]
		public virtual async Task Default()
		{
			Console.WriteLine("Hello, World!");
			await Task.CompletedTask;
		}
		
		/*

		[Once, Description("example command line option")]
	    public virtual string? ExampleOption {get; set;}
	
	    [Once, Description("example action")]
	    public virtual async Task ExampleAction()
	    {
		    Console.WriteLine($"ExampleAction. ExampleOption: {ExampleOption}.");
		    await Task.CompletedTask;
	    }

		*/
    }
}
