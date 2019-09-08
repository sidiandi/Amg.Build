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

        private string sourceDir;
        private string buildDll;
        private Type targetsType;
        private string[] commandLineArguments;
        private readonly bool rebuildCheck;

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
            string buildSourceFile,
            string buildDll,
            Type targetsType,
            string[] commandLineArguments,
            bool rebuildCheck
            )
        {
            this.sourceDir = buildSourceFile.Parent();
            this.buildDll = buildDll;
            this.targetsType = targetsType;
            this.commandLineArguments = commandLineArguments;
            this.rebuildCheck = rebuildCheck;
        }

        static ExitCode RequireRebuild()
        {
            Console.Out.WriteLine("Build script requires rebuild.");
            return ExitCode.RebuildRequired;
        }

        public ExitCode Run()
        {
            try
            {
                Logger = Log.Logger = new LoggerConfiguration()
                    .WriteTo.Console(SerilogLogEventLevel(Verbosity.Detailed))
                    .CreateLogger();

                if (rebuildCheck)
                {
                    if (IsOutOfDate())
                    {
                        return RequireRebuild();
                    }
                }

                var builder = new DefaultProxyBuilder();
                var generator = new ProxyGenerator(builder);
                var onceInterceptor = new OnceInterceptor();
                var onceProxy = generator.CreateClassProxy(targetsType, new ProxyGenerationOptions
                {
                    Hook = new OnceHook()
                },
                onceInterceptor, new LogInvocationInterceptor());

                var options = new Options(onceProxy);

                GetOptParser.Parse(commandLineArguments, options);

                Logger = Log.Logger = new LoggerConfiguration()
                    .WriteTo.Console(SerilogLogEventLevel(options.Verbosity),
                        outputTemplate: "{Timestamp:o}|{Level:u3}|{Message:lj}{NewLine}{Exception}")
                    .CreateLogger();

                if (options.Edit)
                {
                    var cmd = new Tool("cmd").WithArguments("/c", "start");
                    var buildCsProj = sourceDir.Glob("*.csproj").First();
                    cmd.Run(buildCsProj).Wait();
                    return ExitCode.HelpDisplayed;
                }

                if ((options.Clean && !options.IgnoreClean))
                {
                    return RequireRebuild();
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
                    invocations = invocations.Concat(onceInterceptor.Invocations);
                    if (options.Verbosity > Verbosity.Quiet)
                    {
                        Console.WriteLine(Summary.Print(invocations));
                    }
                    Console.Error.WriteLine(Summary.Error(invocations));
                    return ExitCode.TargetFailed;
                }

                invocations = invocations.Concat(onceInterceptor.Invocations);
                if (options.Verbosity > Verbosity.Quiet)
                {
                    Console.WriteLine(Summary.Print(invocations));
                }
                return ExitCode.Success;
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
                Console.Error.WriteLine(ex.Message);
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

        private static string GetThisSourceFile([CallerFilePath] string filePath = null)
        {
            return filePath;
        }

        static string BuildScriptDll => Assembly.GetEntryAssembly().Location;

        /// <summary>
        /// Detects if the buildDll itself needs to be re-built.
        /// </summary>
        /// <returns></returns>
        bool IsOutOfDate()
        {
            if (Regex.IsMatch(buildDll.FileName(), "test", RegexOptions.IgnoreCase))
            {
                Logger.Warning("test mode detected. Out of date check skipped.");
                return false;
            }

            var sourceFiles = sourceDir.Glob("**")
                .Exclude("bin")
                .Exclude("obj")
                .Exclude(".vs");

            var sourceChanged = sourceFiles.LastWriteTimeUtc();
            var buildDllChanged = buildDll.LastWriteTimeUtc();

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
                    return (GetDefaultTarget(targets), new string[] { });
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
            Logger.Information("Run {target}({arguments})", target, arguments);
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

            if (defaultTarget == null)
            {
                throw new Exception($@"No default target found.

Specify a target or add a method with signature

    [Once] [Default]
    public virtual async Task Default()
    {{
        ...
    }}

in the {targets.GetType().BaseType} class.");
            }
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

        internal static Targets Once<Targets>() where Targets : class
        {
            var builder = new DefaultProxyBuilder();
            var generator = new ProxyGenerator(builder);
            return generator.CreateClassProxy<Targets>(new ProxyGenerationOptions
            {
                Hook = new OnceHook()
            },
            new OnceInterceptor(), new LogInvocationInterceptor());
        }
    }
}
