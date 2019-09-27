using System.Threading.Tasks;
using System;
using Amg.Build;
using System.ComponentModel;

public class Build
{
    private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	
	static int Main(string[] args) => Runner.Run(args);

	[Description("example command line option")]
	public string ExampleOption {get; set;}
	
	[Once, Description("example action")]
	public virtual async Task ExampleAction()
	{
		Console.WriteLine("ExampleAction");
		await Task.CompletedTask;
	}
	
	[Once, Default, Description("Example default action")]
	public virtual async Task Default()
	{
		Console.WriteLine("Default");
		await Task.CompletedTask;
	}
}

