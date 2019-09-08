using System.Threading.Tasks;
using System;
using System.IO;
using Amg.Build;
using System.Diagnostics;
using System.Linq;

public class BuildTargets
{
    private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	
	static int Main(string[] args) => Runner.Run<BuildTargets>(args);

	[Once, Default]
	public virtual async Task Default()
	{
		await Task.CompletedTask;
	}
}

