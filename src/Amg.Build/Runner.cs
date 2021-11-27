using Amg.FileSystem;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Amg.Build;

/// <summary>
/// Runs classes with [Once] 
/// </summary>
public static class Runner
{
    private static Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

    /// <summary>
    /// Treats the caller's type as container of [Once] methods and runs it.
    /// </summary>
    /// Convenience method to embed the Main method directly in your Build class:
    /// ````
    /// 	static int Main(string[] args) => Runner.Run(args);
    /// ````
    /// See $/examples/hello/build/build.cs for an example.
    /// <param name="commandLineArguments"></param>
    /// <param name="sourceFile"></param>
    /// <returns>Exit code: 0 if success, unequal to 0 otherwise.</returns>
    public static int Run(string[] commandLineArguments)
    {
        StackFrame frame = new StackFrame(1);
        var method = frame.GetMethod();
        var type = method.DeclaringType;

        return RunType(type, commandLineArguments);
    }

    /// <summary>
    /// Creates an CommandsClass instance and runs the contained commands according to the passed commandLineArguments.
    /// </summary>
    /// Call this method directly from your Main()
    /// <typeparam name="CommandsClass"></typeparam>
    /// <param name="commandLineArguments"></param>
    /// <param name="callerFilePath"></param>
    /// <returns>Exit code: 0 if success, unequal to 0 otherwise.</returns>
    public static int Run<CommandsClass>(
        string[] commandLineArguments)
        where CommandsClass : class
    {
        return RunType(typeof(CommandsClass), commandLineArguments);
    }

    internal static int Run(object commandObject, string[] commandLineArguments)
    {
        var runner = new RunContext(
            () => commandObject,
            commandLineArguments);
        return runner.Run().Result;
    }

    static int RunType(Type commandObjectType, string[] commandLineArguments)
    {
        var runner = new RunContext(
            () => Once.Create(commandObjectType),
            commandLineArguments);
        return runner.Run().Result;
    }

    /// <summary>
    /// Returns the directory where build.cmd resides.
    /// </summary>
    /// <param name="callerFilePath"></param>
    /// <returns></returns>
    public static string RootDirectory([CallerFilePath] string callerFilePath = null!)
    {
        return callerFilePath.Parent().Parent();
    }
}
