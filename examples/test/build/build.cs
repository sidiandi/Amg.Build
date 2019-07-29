using Amg.Build;

partial class BuildTargets : Targets
{
    private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	
	static int Main(string[] args) => Targets.Run<BuildTargets>(args);

	Target Default => DefineTarget(async () =>
	{
	});
}

