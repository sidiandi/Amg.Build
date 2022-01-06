using Amg.Extensions;
using Amg.FileSystem;
using Amg.GetOpt;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Diagnostics;
using System.Reflection;

namespace Amg.Build;

/// <summary>
/// Runs classes with [Once] 
/// </summary>
internal class RunContext
{
    private static Serilog.ILogger Logger => Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

    private readonly Func<object> commandObjectFactory;
    private readonly string[] commandLineArguments;

    public RunContext(
        Func<object> commandObjectFactory,
        string[] commandLineArguments
        )
    {
        this.commandObjectFactory = commandObjectFactory;
        this.commandLineArguments = commandLineArguments;
    }

    async Task<int?> Watch()
    {
        if (!String.IsNullOrEmpty(System.Environment.GetEnvironmentVariable(nowatchEnvironmentVariable)))
        {
            return null;
        }

        var instance = commandObjectFactory();

        var sourceCodeLayout = SourceCodeLayout.Get(instance);
        if (sourceCodeLayout != null)
        {
            await WatchInternal(sourceCodeLayout, this.commandLineArguments);
            return ExitCode.Success;
        }
        else
        {
            return ExitCode.CommandFailed;
        }
    }

    const string nowatchEnvironmentVariable = "Amg.Build_nowatch";

    async Task WatchInternal(SourceCodeLayout source, string[] commandLineArgs)
    {

        var watchedDir = source.CmdFile.Parent();
        using var fsw = new FileSystemWatcher
        {
            Path = watchedDir,
            IncludeSubdirectories = true,
        };
        var tool = Tools.Cmd.WithArguments(source.CmdFile)
            .WithEnvironment(nowatchEnvironmentVariable, true.ToString())
            .Passthrough();

        Task run = Task.CompletedTask;

        void Changed(object sender, FileSystemEventArgs e)
        {
            if (run.IsCompleted)
            {
                run = tool.Run(commandLineArgs);
            }
        }

        fsw.Changed += Changed;
        fsw.Created += Changed;
        fsw.Deleted += Changed;
        fsw.Renamed += Changed;

        fsw.EnableRaisingEvents = true;

        Console.Write($"Watching {watchedDir}...");
        await Task.Delay(-1);

        fsw.EnableRaisingEvents = false;

        fsw.Changed -= Changed;
        fsw.Created -= Changed;
        fsw.Deleted -= Changed;
        fsw.Renamed -= Changed;
    }

    public async Task<int> Run()
    {
        try
        {
            RecordStartupTime();

            var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);
            bool needConfigureLogger = Log.Logger.GetType().Name.Equals("SilentLogger");
            if (needConfigureLogger)
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.ControlledBy(levelSwitch)
                    .WriteTo.Console(LogEventLevel.Verbose,
                    standardErrorFromLevel: LogEventLevel.Error,
                    outputTemplate: "{Timestamp:o}|{Level:u3}|{Message:lj}{NewLine}{Exception}"
                    )
                    .CreateLogger();
            }

            await RebuildMyself.BuildIfSourcesChanged(commandLineArguments);

            var commandObject = commandObjectFactory();
            var source = SourceCodeLayout.Get(commandObject);

            var combinedOptions = new CombinedOptions(commandObject);
            if (source != null)
            {
                combinedOptions.SourceOptions = new SourceOptions();
            }

            var commandProvider = CommandProviderFactory.FromObject(combinedOptions);
            var parser = new Parser(commandProvider);
            parser.Parse(commandLineArguments);

            if (combinedOptions.Options.Help)
            {
                Help.PrintHelpMessage(Console.Out, commandProvider);
                return Amg.GetOpt.ExitCode.HelpDisplayed;
            }

            levelSwitch.MinimumLevel = SerilogLogEventLevel(combinedOptions.Options.Verbosity);

            if (combinedOptions.SourceOptions != null && source != null)
            {
                var sourceOptions = combinedOptions.SourceOptions;
                if (sourceOptions.Edit)
                {
                    await Tools.Cmd.Run("start", source.CsprojFile);
                    return Amg.GetOpt.ExitCode.Success;
                }

                if (sourceOptions.Debug)
                {
                    System.Diagnostics.Debugger.Launch();
                }

                if (sourceOptions.Watch)
                {
                    await Watch();
                }
            }

            var amgBuildAssembly = Assembly.GetExecutingAssembly();
            Logger.Debug("{name} {version}", amgBuildAssembly.GetName().Name, amgBuildAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);

            try
            {
                var results = await parser.Run();
                results.Destructure().Write(Console.Out);
            }
            catch (NoDefaultCommandException)
            {
                Help.PrintHelpMessage(Console.Out, commandProvider);
                return GetOpt.ExitCode.HelpDisplayed;
            }
            catch (AggregateException aex)
            {
                int exitCode = ExitCode.Success;

                aex.Handle(exception =>
                {
                    if (exception is NoDefaultCommandException)
                    {
                        Help.PrintHelpMessage(Console.Out, commandProvider);
                        exitCode = ExitCode.HelpDisplayed;
                        return true;
                    }
                    else
                    {
                        Console.Error.WriteLine(exception);
                        exitCode = ExitCode.UnknownError;
                        return true;
                    }
                });

                return exitCode;
            }

            var invocations = new[] { GetStartupInvocation() }
                .Concat(((IInvocationSource)commandObject).Invocations);

            if (combinedOptions.Options.Summary)
            {
                Summary.PrintTimeline(invocations).Write(Console.Out);
            }

            return invocations.Failed()
                ? ExitCode.CommandFailed
                : ExitCode.Success;
        }
        catch (OnceException ex)
        {
            Console.Error.WriteLine(ex);
            Console.Error.WriteLine();
            Console.Error.WriteLine("See https://github.com/sidiandi/Amg.Build/ for instructions.");
            return Amg.GetOpt.ExitCode.CommandFailed;
        }
        catch (Amg.GetOpt.CommandLineException ex)
        {
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine();
            Console.Error.WriteLine("Run with --help to get help.");
            return Amg.GetOpt.ExitCode.CommandLineError;
        }
        catch (InvocationFailedException)
        {
            return Amg.GetOpt.ExitCode.CommandFailed;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($@"An unknown error has occured.

This is a bug in Amg.Build.

Submit here: https://github.com/sidiandi/Amg.Build/issues

Details:
{ex}
");
            return Amg.GetOpt.ExitCode.UnknownError;
        }
    }

    string StartupFile => BuildScriptDll + ".startup";

    void RecordStartupTime()
    {
        if (!StartupFile.IsFile())
        {
            Json.Write(StartupFile, DateTime.UtcNow);
        }
    }

    DateTime GetStartupTime()
    {
        if (StartupFile.IsFile())
        {
            try
            {
                return Json.Read<DateTime>(StartupFile).Result;
            }
            catch
            {
                // ignore read errors
            }
            finally
            {
                StartupFile.EnsureFileNotExists();
            }
        }
        return Process.GetCurrentProcess().StartTime.ToUniversalTime();
    }

    IInvocation GetStartupInvocation()
    {
        var begin = GetStartupTime();
        var end = DateTime.UtcNow;
        var startupDuration = end - begin;
        Logger.Debug("Startup duration: {startupDuration}", startupDuration);
        var startupInvocation = new InvocationInfo("startup", begin, end);
        return startupInvocation;
    }

    string BuildScriptDll => Assembly.GetEntryAssembly().Location;

    private static LogEventLevel SerilogLogEventLevel(Verbosity verbosity)
    {
        switch (verbosity)
        {
            case Verbosity.Detailed:
                return LogEventLevel.Debug;
            case Verbosity.Normal:
                return LogEventLevel.Information;
            case Verbosity.Minimal:
                return LogEventLevel.Error;
            case Verbosity.Quiet:
                return LogEventLevel.Fatal;
            default:
                throw new ArgumentOutOfRangeException(nameof(verbosity), verbosity, "no enum value");
        }
    }
}
