using Amg.CommandLine;
using Castle.DynamicProxy;
using Serilog;
using Serilog.Events;
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
        private static Serilog.ILogger Logger;

        private Type targetsType;
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
                Logger = Log.Logger = new LoggerConfiguration()
                    .WriteTo.Console(SerilogLogEventLevel(Verbosity.Detailed))
                    .CreateLogger();

                var builder = new DefaultProxyBuilder();
                var generator = new ProxyGenerator(builder);
                var onceInterceptor = new OnceInterceptor();
                var onceProxy = generator.CreateClassProxy(targetsType, new ProxyGenerationOptions
                {
                    Hook = new OnceHook()
                },
                onceInterceptor);

                var source = SourceCodeLayout.Get(targetsType);
                Logger.Information("{@source}", new { source.cmdFile });

                Options options = null;

                if (source != null)
                {
                    var sourceOptions = new OptionsWithSource(onceProxy);
                    options = sourceOptions;
                    GetOptParser.Parse(commandLineArguments, options);
                    if (sourceOptions.Fix)
                    {
                        await source.Fix();
                        return ExitCode.Success;
                    }
                    else
                    {
                        await source.Check();
                    }
                    if (IsOutOfDate(source) && !sourceOptions.IgnoreClean)
                    {
                        return RequireRebuild();
                    }

                    Logger = Log.Logger = new LoggerConfiguration()
                        .WriteTo.Console(SerilogLogEventLevel(options.Verbosity),
                            outputTemplate: "{Timestamp:o}|{Level:u3}|{Message:lj}{NewLine}{Exception}")
                        .CreateLogger();

                    if (sourceOptions.Edit)
                    {
                        var cmd = Tools.Cmd.WithArguments("start");
                        cmd.Run(source.csprojFile).Wait();
                        return ExitCode.HelpDisplayed;
                    }

                    if ((sourceOptions.Clean && !sourceOptions.IgnoreClean))
                    {
                        return RequireRebuild();
                    }
                }
                else
                {
                    options = new Options(onceProxy);
                    GetOptParser.Parse(commandLineArguments, options);

                    Logger = Log.Logger = new LoggerConfiguration()
                        .WriteTo.Console(SerilogLogEventLevel(options.Verbosity),
                            outputTemplate: "{Timestamp:o}|{Level:u3}|{Message:lj}{NewLine}{Exception}")
                        .CreateLogger();
                }

                if (options.Help)
                {
                    HelpText.Print(Console.Out, options);
                    return ExitCode.HelpDisplayed;
                }

                var (target, targetArguments) = ParseCommandLineTarget(commandLineArguments, options);

                var amgBuildAssembly = Assembly.GetExecutingAssembly();
                Logger.Information("Amg.Build: {assembly} {build}", amgBuildAssembly.Location, amgBuildAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);

                var startup = GetStartupInvocation();
                IEnumerable<InvocationInfo> invocations = new[] { startup };

                try
                {
                    RunTarget(options.Targets, target, targetArguments);
                }
                catch (InvocationFailed)
                {
                    return ExitCode.TargetFailed;
                }

                invocations = invocations.Concat(onceInterceptor.Invocations);

                if (options.Summary)
                {
                    Console.WriteLine(Summary.PrintTimeline(invocations));
                }

                Summary.PrintSummary(invocations);
                return invocations.Failed()
                    ? ExitCode.TargetFailed
                    : ExitCode.Success;
            }
            catch (ParseException ex)
            {
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine();
                Console.Error.WriteLine("Run with --help to get help.");
                return ExitCode.CommandLineError;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($@"An unknown error has occured.

This is most likely a bug in Amg.Build

Submit here: https://github.com/sidiandi/Amg.Build/issues

Details:
{ex}
");
                return ExitCode.UnknownError;
            }
        }

        private static InvocationInfo GetStartupInvocation()
        {
            var startupFile = BuildScriptDll + ".startup";
            var begin = startupFile.IsFile()
                ? startupFile.LastWriteTimeUtc()
                : Process.GetCurrentProcess().StartTime.ToUniversalTime();

            var end = DateTime.UtcNow;
            var startupDuration = end - begin;
            Logger.Information("Startup duration: {startupDuration}", startupDuration);
            var startupInvocation = new InvocationInfo("startup", begin, end);
            return startupInvocation;
        }

        static string BuildScriptDll => Assembly.GetEntryAssembly().Location;

        /// <summary>
        /// Detects if the buildDll itself needs to be re-built.
        /// </summary>
        /// <returns></returns>
        static bool IsOutOfDate(SourceCodeLayout layout)
        {
            if (Regex.IsMatch(layout.dllFile.FileName(), "test", RegexOptions.IgnoreCase))
            {
                Logger.Warning("test mode detected. Out of date check skipped.");
                return false;
            }

            var sourceFiles = layout.sourceDir.Glob("**")
                .Exclude("bin")
                .Exclude("obj")
                .Exclude(".vs");

            var sourceChanged = sourceFiles.LastWriteTimeUtc();
            var buildDllChanged = layout.dllFile.LastWriteTimeUtc();

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
                    return (defaultTarget, new string[] { });
                }
                catch (Exception e)
                {
                    throw new ParseException(arguments, -1, e);
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
                throw new ParseException(arguments, Array.IndexOf(arguments, targetName), e);
            }
        }

        private static void RunTarget(object targets, MethodInfo target, string[] arguments)
        {
            Logger.Information("Run {target}({arguments})", target.Name, arguments.Join(", "));
            var result = Wait(GetOptParser.Invoke(targets, target, arguments));
        }

        private static MethodInfo GetDefaultTarget(object targets)
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
        static object Wait(object returnValue)
        {
            if (returnValue is Task task)
            {
                try
                {
                    task.Wait();
                    return null;
                }
                catch (System.AggregateException aggregateException)
                {
                    aggregateException.Handle(e => throw e);
                    throw;
                }
            }
            else
            {
                return returnValue;
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
                    return LogEventLevel.Fatal + 1;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
