using System.Threading.Tasks;
using System;
using System.IO;
using Amg.Build;
using System.Diagnostics;
using System.Linq;

partial class BuildTargets : Targets
{
    private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	
	static int Main(string[] args) => Targets.Run<BuildTargets>(args);

	Target<string, Nothing> Greet => DefineTarget(async (string name) =>
	{
		await Task.Delay(TimeSpan.FromSeconds(1));
		Console.WriteLine($"Hello, {name}");
		return Nothing.Instance;
	});
	
	public Target GreetAll => DefineTarget(async () =>
	{
		await Task.WhenAll(Enumerable.Range(0,5).Select(_ => Greet($"Alice {_}")));
	});
	
	Target Default => DefineTarget(async () =>
	{
		Console.WriteLine(GetRootDirectory());
		await GreetAll();
	});
}

