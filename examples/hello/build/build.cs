using System.Threading.Tasks;
using System;
using System.IO;
using Amg.Build;
using System.Diagnostics;

partial class BuildTargets : Targets
{
    private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	
	static int Main(string[] args) => Targets.Run<BuildTargets>(args);

	Target<string, Nothing> Greet => DefineTarget((string name) =>
	{
		Console.WriteLine($"Hello, {name}");
		return Nothing.Instance;
	});
	
	public Target GreetAll => DefineTarget(async () =>
	{
		await Task.WhenAll(Greet("Alice"), Greet("Bob"));
	});
	
	Target Default => DefineTarget(async () =>
	{
		await GreetAll();
	});
}

