using System.Threading.Tasks;
using System;
using System.IO;
using Amg.Build;
using System.Diagnostics;
using System.Linq;
using System.ComponentModel;

public class BuildTargets
{
    private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	
	static int Main(string[] args) => Runner.Run(args);

	[Once]
	public virtual async Task Greet(string name)
	{
		await Task.Delay(TimeSpan.FromSeconds(1));
		Console.WriteLine($"Hello, {name}");
	}
	
	[Once]
	public virtual async Task GreetAll()
	{
		await Task.WhenAll(Enumerable.Range(0,5).Select(_ => Greet($"Alice {_}")));
	}
	
	[Once]
	public virtual async Task Default()
	{
		await GreetAll();
	}

    [Once] [Description("Simulate a failing tool")]
    public virtual async Task FailTool()
    {
        await new Tool("cmd.exe").Run("/c", "fasdfasdfasd");
    }

    [Once][Description("Use failing tool")]
    public virtual async Task UseFailingTool()
    {
        await FailTool();
    }
}

