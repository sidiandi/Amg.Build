using Serilog;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Amg.FileSystem;

namespace Amg.Test;

public class TestBase
{
    static TestBase()
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(standardErrorFromLevel: Serilog.Events.LogEventLevel.Error)
            .CreateLogger();
    }

    protected string CreateEmptyTestDirectory([CallerMemberName] string name = null!)
    {
        var dir = this.GetType().GetProgramDataDirectory().Combine(name);
        return dir.EnsureDirectoryIsEmpty();
    }

    public static TimeSpan MeasureTime(Action a)
    {
        var stopwatch = Stopwatch.StartNew();
        a();
        return stopwatch.Elapsed;
    }

    public static TimeSpan MeasureTime(Func<Task> a)
    {
        var stopwatch = Stopwatch.StartNew();
        a().Wait();
        return stopwatch.Elapsed;
    }
}
