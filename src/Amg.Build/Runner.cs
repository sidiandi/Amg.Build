using Amg.CommandLine;
using Castle.DynamicProxy;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
    public class Runner
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        const int ExitCodeHelpDisplayed = 1;
        const int ExitCodeUnknownError = -1;
        const int ExitCodeSuccess = 0;
        const int ExitCodeRebuildRequired = 2;

        /// <summary>
        /// Creates an TargetsDerivedClass instance and runs the contained targets according to the passed commandLineArguments.
        /// </summary>
        /// Call this method directly from your Main()
        /// <typeparam name="TargetsDerivedClass"></typeparam>
        /// <param name="commandLineArguments"></param>
        /// <returns>Exit code: 0 if success, unequal to 0 otherwise.</returns>
        public static int Run<TargetsDerivedClass>(string[] commandLineArguments) where TargetsDerivedClass : class
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(SerilogLogEventLevel(Verbosity.Detailed))
                .CreateLogger();

            if (IsOutOfDate())
            {
                Console.Error.WriteLine("Build script requires rebuild.");
                return ExitCodeRebuildRequired;
            }

            var builder = new DefaultProxyBuilder();
            var generator = new ProxyGenerator(builder);
            var onceInterceptor = new OnceInterceptor();
            var onceProxy = generator.CreateClassProxy<TargetsDerivedClass>(new ProxyGenerationOptions
            {
                Hook = new OnceHook()
            },
            onceInterceptor, new LogInvocationInterceptor());

            var options = new Options<TargetsDerivedClass>(onceProxy);
            GetOptParser.Parse(commandLineArguments, options);

            if ((options.Clean && !options.IgnoreClean))
            {
                Console.Error.WriteLine("Build script requires rebuild.");
                return ExitCodeRebuildRequired;
            }

            if (options.Help)
            {
                HelpText.Print(Console.Out, options);
                return ExitCodeHelpDisplayed;
            }

            var amgBuildAssembly = typeof(Target).Assembly;

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(SerilogLogEventLevel(options.Verbosity))
                .CreateLogger();

            Logger.Information("{assembly} {build}", amgBuildAssembly.Location, amgBuildAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);

            var invocations = GetStartupInvocations();

            try
            {
                RunTarget(options.TargetAndArguments, options.targets);
                invocations = invocations.Concat(onceInterceptor.Invocations);
                Console.WriteLine(Summary.Print(invocations));
                return ExitCodeSuccess;
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex, "Build failed.");
                return ExitCodeUnknownError;
            }
        }

        private static IEnumerable<OnceInterceptor.Invocation> GetStartupInvocations()
        {
            Enumerable.Empty<OnceInterceptor.Invocation>();
            var startupFile = BuildScriptDll + ".startup";
            var begin = startupFile.IsFile()
                ? startupFile.LastWriteTimeUtc()
                : Process.GetCurrentProcess().StartTime.ToUniversalTime();

            var end = DateTime.UtcNow;
            var startupDuration = end - begin;
            Logger.Information("Startup duration: {startupDuration}", startupDuration);
            var startupInvocation = new OnceInterceptor.Invocation("startup", Task.CompletedTask)
            {
                Begin = begin,
                End = end
            };
            return new[] { startupInvocation };
        }

        private static string GetThisSourceFile([CallerFilePath] string filePath = null)
        {
            return filePath;
        }

        static string BuildScriptDll => Assembly.GetEntryAssembly().Location;

        static string BuildScriptSourceDir => BuildScriptDll.Parent().Parent().Parent().Parent();

        private static bool IsOutOfDate()
        {
            var buildDll = BuildScriptDll;
            if (Regex.IsMatch(buildDll.FileName(), "test", RegexOptions.IgnoreCase))
            {
                Logger.Warning("test mode detected. Out of date check skipped.");
                return false;
            }

            Logger.Information("{buildDll}", buildDll);
            var sourceFiles = BuildScriptSourceDir.Glob("**")
                .Exclude("bin")
                .Exclude("obj")
                .Exclude(".vs");
            return buildDll.IsOutOfDate(sourceFiles);
        }

        private static void RunTarget(string[] targetAndArguments, object targets)
        {
            var targetName = targetAndArguments.FirstOrDefault();
            var target = (targetName == null)
                ? GetDefaultTarget(targets)
                : HelpText.PublicTargets(targets.GetType()).FindByName(_ => _.Name, targetName);
            var arguments = targetAndArguments.Skip(1).ToArray();

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
                throw new Exception(@"No default target found.

Add a method with signature

[Once] [Default]
public virtual async Task Default()
{
    ...
}

in your {targets.GetType()} class.

");
            }
            return defaultTarget;
        }

        static object Wait(object returnValue)
        {
            if (returnValue is Task task)
            {
                task.Wait();
                return null;
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
