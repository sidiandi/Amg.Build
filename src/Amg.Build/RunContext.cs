using Amg.CommandLine;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Amg.Build
{
    /// <summary>
    /// Runs classes with [Once] 
    /// </summary>
    internal class RunContext
    {
        private static Serilog.ILogger Logger => Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

        private readonly Type targetsType;
        private readonly string[] commandLineArguments;

        public enum ExitCode
        {
            Success = 0,
            UnknownError = 1,
            RebuildRequired = 2,
            HelpDisplayed = 3,
            CommandLineError = 4,
            TargetFailed = 5
        }

        public RunContext(
            Type targetsType,
            string[] commandLineArguments
            )
        {
            this.targetsType = targetsType;
            this.commandLineArguments = commandLineArguments;
        }

        static ExitCode RequireRebuild()
        {
            Console.Out.WriteLine("Build script requires rebuild.");
            return ExitCode.RebuildRequired;
        }

        public async Task<ExitCode> Run()
        {
            try
            {
                RecordStartupTime();

                var minimalOptions = new Options();
                GetOptParser.Parse(commandLineArguments, minimalOptions, ignoreUnknownOptions: true);

                bool needConfigureLogger = Log.Logger.GetType().Name.Equals("SilentLogger");
                if (needConfigureLogger)
                {
                    var levelSwitch = new LoggingLevelSwitch(SerilogLogEventLevel(minimalOptions.Verbosity));
                    if (minimalOptions.Verbosity == Verbosity.Quiet)
                    {
                        Tools.Default = Tools.Default.Silent();
                    }

                    Log.Logger = new LoggerConfiguration()
                        .MinimumLevel.ControlledBy(levelSwitch)
                        .WriteTo.Console(LogEventLevel.Verbose,
                        standardErrorFromLevel: LogEventLevel.Error,
                        outputTemplate: "{Timestamp:o}|{Level:u3}|{Message:lj}{NewLine}{Exception}"
                        )
                        .CreateLogger();
                }

                var source = await RebuildMyself.BuildIfSourcesChanged(commandLineArguments);

                var onceProxy = Once.Create(targetsType);
                var combinedOptions = new CombinedOptions(onceProxy);
                if (source != null)
                {
                    combinedOptions.SourceOptions = new SourceOptions();
                }

                GetOptParser.Parse(commandLineArguments, combinedOptions);

                if (combinedOptions.Options.Help)
                {
                    HelpText.Print(Console.Out, combinedOptions);
                    return ExitCode.HelpDisplayed;
                }

                if (combinedOptions.SourceOptions != null && source != null)
                {
                    var sourceOptions = combinedOptions.SourceOptions;
                    if (sourceOptions.Edit)
                    {
                        await Tools.Cmd.Run("start", source.CsprojFile);
                    }

                    if (sourceOptions.Debug)
                    {
                        // tbd
                    }
                }

                var (target, targetArguments) = ParseCommandLineTarget(commandLineArguments, combinedOptions);

                var amgBuildAssembly = Assembly.GetExecutingAssembly();
                Logger.Debug("{name} {version}", amgBuildAssembly.GetName().Name, amgBuildAssembly.NugetVersion());

                IEnumerable<InvocationInfo> invocations = new[] { GetStartupInvocation() };

                object? result = null;
                
                try
                {
                    result = await RunTarget(onceProxy, target, targetArguments);
                }
                catch (InvocationFailedException)
                {
                    // can be ignored here, because failures are recorded in invocations
                }

                invocations = invocations.Concat(((IInvocationSource)onceProxy).Invocations);

                if (combinedOptions.Options.Summary)
                {
                    Logger.Information(Summary.PrintTimeline(invocations));
                }

                if (combinedOptions.Options.AsciiArt)
                {
                    Summary.PrintAsciiArt(invocations);
                }

                if (result != null)
                {
                    result.Dump().Write(Console.Out);
                }

                return invocations.Failed()
                    ? ExitCode.TargetFailed
                    : ExitCode.Success;
            }
            catch (CommandLineArgumentException ex)
            {
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine();
                Console.Error.WriteLine("Run with --help to get help.");
                return ExitCode.CommandLineError;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($@"An unknown error has occured.

This is a bug in Amg.Build.

Submit here: https://github.com/sidiandi/Amg.Build/issues

Details:
{ex}
");
                return ExitCode.UnknownError;
            }
        }

        private async Task<bool> Watch()
        {
            var rootDir = Assembly.GetEntryAssembly().Location
                .Parent()
                .Parent()
                .Parent()
                .Parent();

            if (rootDir != null)
            {
                var watcher = new Watcher(
                    Assembly.GetEntryAssembly(),
                    commandLineArguments,
                    rootDir);
                
                if (watcher.IsWatching())
                {
                    return false;
                }

                await watcher.Watch();
                return true;
            }
            else
            {
                return false;
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

        InvocationInfo GetStartupInvocation()
        {
            var begin = GetStartupTime();
            var end = DateTime.UtcNow;
            var startupDuration = end - begin;
            Logger.Debug("Startup duration: {startupDuration}", startupDuration);
            var startupInvocation = new InvocationInfo("startup", begin, end);
            return startupInvocation;
        }

        string BuildScriptDll => Assembly.GetEntryAssembly().Location;

        /// <summary>
        /// Extract the target to be called and its arguments from command line arguments
        /// </summary>
        /// <param name="arguments">all command line arguments (required for error display)</param>
        /// <param name="options">parsed options</param>
        /// <returns></returns>
        static (MethodInfo target, string[] arguments) ParseCommandLineTarget(string[] arguments, CombinedOptions options)
        {
            var commandAndArguments = options.Options!.TargetAndArguments;
            var targets = options.OnceProxy;
            if (commandAndArguments.Length == 0)
            {
                try
                {
                    var defaultTarget = GetDefaultTarget(targets);
                    if (defaultTarget == null)
                    {
                        HelpText.Print(Console.Out, options);
                        Environment.Exit((int)ExitCode.HelpDisplayed);
                    }
                    else
                    {
                        return (defaultTarget, new string[] { });
                    }
                }
                catch (Exception e)
                {
                    throw new CommandLineArgumentException(arguments, -1, e);
                }
            }

            var candidates = HelpText.Commands(targets.GetType());

            var commandName = commandAndArguments[0];
            var commandArguments = commandAndArguments.Skip(1).ToArray();
            try
            {
                var command = candidates.FindByName(
                    _ => GetOptParser.GetLongOptionNameForMember(_.Name),
                    commandName,
                    "commands"
                    );
                return (command, commandArguments);
            }
            catch (ArgumentOutOfRangeException e)
            {
                throw new CommandLineArgumentException(arguments, Array.IndexOf(arguments, commandName), e);
            }
        }

        private static async Task<object?> RunTarget(object targets, MethodInfo target, string[] arguments)
        {
            Logger.Information("Run {target}({arguments})", target.Name, arguments.Join(", "));
            return await AsTask(GetOptParser.Invoke(targets, target, arguments));
        }

        private static MethodInfo? GetDefaultTarget(object targets)
        {
            var t = HelpText.Targets(targets.GetType());
            var defaultTarget = new[]
            {
                t.FirstOrDefault(_ => _.GetCustomAttribute<DefaultAttribute>() != null),
                t.FindByNameOrDefault(_ => _.Name, "All"),
                t.FindByNameOrDefault(_ => _.Name, "Default"),
            }.FirstOrDefault(_ => _ != null);

            return defaultTarget;
        }

        /// <summary>
        /// Waits if returnValue is Task
        /// </summary>
        /// <param name="returnValue"></param>
        /// <returns></returns>
        static Task<object?> AsTask(object returnValue)
        {
            if (returnValue is Task task)
            {
                var type = returnValue.GetType();
                var resultProperty = type.GetProperty("Result");
                return task.ContinueWith((_) =>
                {
                    try
                    {
                        var result = resultProperty == null
                            ? null
                            : resultProperty.GetValue(task);
                        return result;
                    }
                    catch (TargetInvocationException targetInvocationException)
                    {
                        throw targetInvocationException.InnerException.InnerException;
                    }
                });
            }
            else
            {
                return Task.FromResult<object?>(returnValue);
            }
        }

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
}
