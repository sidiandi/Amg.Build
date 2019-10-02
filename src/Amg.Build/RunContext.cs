using Amg.CommandLine;
using Castle.DynamicProxy;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Amg.Build
{
    /// <summary>
    /// Runs classes with [Once] 
    /// </summary>
    internal class RunContext
    {
        private static Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

        private Type targetsType;
        private readonly string sourceFile;
        private string[] commandLineArguments;

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
            string sourceFile,
            string[] commandLineArguments
            )
        {
            this.targetsType = targetsType;
            this.sourceFile = sourceFile;
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

                var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Debug);

                bool needConfigureLogger = Log.Logger.GetType().Name.Equals("SilentLogger");

                if (needConfigureLogger)
                {
                    Logger = Log.Logger = new LoggerConfiguration()
                        .MinimumLevel.ControlledBy(levelSwitch)
                        .WriteTo.Console(LogEventLevel.Verbose,
                        standardErrorFromLevel: LogEventLevel.Error,
                        outputTemplate: "{Timestamp:o}|{Level:u3}|{Message:lj}{NewLine}{Exception}"
                        )
                        .CreateLogger();
                }

                RebuildMyself.BuildIfSourcesChanged(commandLineArguments).Wait();

                var onceProxy = Once.Create(targetsType);

                var options = new Options(onceProxy);
                GetOptParser.Parse(commandLineArguments, options);

                levelSwitch.MinimumLevel = SerilogLogEventLevel(options.Verbosity);
                if (options.Verbosity == Verbosity.Quiet)
                {
                    Tools.Default = Tools.Default.Silent();
                }

                if (options.Help)
                {
                    HelpText.Print(Console.Out, options);
                    return ExitCode.HelpDisplayed;
                }

                var (target, targetArguments) = ParseCommandLineTarget(commandLineArguments, options);

                var amgBuildAssembly = Assembly.GetExecutingAssembly();
                Logger.Information("Amg.Build: {assembly} {build}", amgBuildAssembly.Location, amgBuildAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);

                IEnumerable<InvocationInfo> invocations = new[] { GetStartupInvocation() };

                try
                {
                    await RunTarget(options.Targets, target, targetArguments);
                }
                catch (InvocationFailedException)
                {
                }

                invocations = invocations.Concat(((IInvocationSource)onceProxy).Invocations);

                if (options.Summary)
                {
                    Logger.Information(Summary.PrintTimeline(invocations));
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
            Logger.Information("Startup duration: {startupDuration}", startupDuration);
            var startupInvocation = new InvocationInfo("startup", begin, end);
            return startupInvocation;
        }

        string BuildScriptDll => Assembly.GetEntryAssembly().Location;

        /// <summary>
        /// Detects if the buildDll itself needs to be re-built.
        /// </summary>
        /// <returns></returns>
        static bool IsOutOfDate(SourceCodeLayout layout)
        {
            if (Regex.IsMatch(layout.DllFile.FileName(), "test", RegexOptions.IgnoreCase))
            {
                Logger.Warning("test mode detected. Out of date check skipped.");
                return false;
            }

            var sourceFiles = layout.SourceDir.Glob("**")
                .Exclude("bin")
                .Exclude("obj")
                .Exclude(".vs");

            var sourceChanged = sourceFiles.LastWriteTimeUtc();
            var buildDllChanged = layout.DllFile.LastWriteTimeUtc();

            var maximalAge = TimeSpan.FromMinutes(60);

            return (sourceChanged > buildDllChanged)
                || (DateTime.UtcNow - buildDllChanged) > maximalAge;
        }

        /// <summary>
        /// Extract the target to be called and its arguments from command line arguments
        /// </summary>
        /// <param name="arguments">all command line arguments (required for error display)</param>
        /// <param name="options">parsed options</param>
        /// <returns></returns>
        static (MethodInfo target, string[] arguments) ParseCommandLineTarget(string[] arguments, Options options)
        {
            var targetAndArguments = options.TargetAndArguments;
            var targets = options.Targets;
            if (targetAndArguments.Length == 0)
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

            var candidates = HelpText.PublicTargets(targets.GetType());

            var targetName = targetAndArguments[0];
            var targetArguments = targetAndArguments.Skip(1).ToArray();
            try
            {
                var target = candidates.FindByName(
                    _ => GetOptParser.GetLongOptionNameForMember(_.Name),
                    targetName,
                    "targets"
                    );
                return (target, targetArguments);
            }
            catch (ArgumentOutOfRangeException e)
            {
                throw new CommandLineArgumentException(arguments, Array.IndexOf(arguments, targetName), e);
            }
        }

        private static async Task RunTarget(object targets, MethodInfo target, string[] arguments)
        {
            Logger.Information("Run {target}({arguments})", target.Name, arguments.Join(", "));
            await AsTask(GetOptParser.Invoke(targets, target, arguments));
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
        static Task AsTask(object returnValue)
        {
            if (returnValue is Task task)
            {
                return (Task)task;
            }
            else
            {
                return Task.FromResult(returnValue);
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
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
