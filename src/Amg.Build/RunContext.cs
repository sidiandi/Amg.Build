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
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private string sourceDir;
        private string buildDll;
        private Type targetsType;
        private string[] commandLineArguments;

        const int ExitCodeHelpDisplayed = 1;
        const int ExitCodeUnknownError = -1;
        const int ExitCodeSuccess = 0;
        const int ExitCodeRebuildRequired = 2;

        public RunContext(
            string buildSourceFile,
            string buildDll,
            Type targetsType,
            string[] commandLineArguments)
        {
            this.sourceDir = buildSourceFile.Parent();
            this.buildDll = buildDll;
            this.targetsType = targetsType;
            this.commandLineArguments = commandLineArguments;
        }

        public int Run()
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
            var onceProxy = generator.CreateClassProxy(targetsType, new ProxyGenerationOptions
            {
                Hook = new OnceHook()
            },
            onceInterceptor, new LogInvocationInterceptor());

            var options = new Options(onceProxy);
            GetOptParser.Parse(commandLineArguments, options);

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(SerilogLogEventLevel(options.Verbosity),
                    outputTemplate: "{Timestamp:o}|{Level:u3}|{Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            if (options.Edit)
            {
                var cmd = new Tool("cmd").WithArguments("/c", "start");
                var buildCsProj = sourceDir.Glob("*.csproj").First();
                cmd.Run(buildCsProj).Wait();
                return ExitCodeHelpDisplayed;
            }

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

            var amgBuildAssembly = Assembly.GetExecutingAssembly();

            Logger.Information("{assembly} {build}", amgBuildAssembly.Location, amgBuildAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);

            var startup = GetStartupInvocation();
            IEnumerable<InvocationInfo> invocations = new[] { startup };

            try
            {
                RunTarget(options.TargetAndArguments, options.Targets);
                invocations = invocations.Concat(onceInterceptor.Invocations);
                Console.WriteLine(Summary.Print(invocations));
                return ExitCodeSuccess;
            }
            catch (InvocationFailed)
            {
                invocations = invocations.Concat(onceInterceptor.Invocations);
                Console.WriteLine(Summary.Print(invocations));
                Console.Error.WriteLine(Summary.Error(invocations));
                return ExitCodeUnknownError;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return ExitCodeUnknownError;
            }
            finally
            {
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

            Logger.Information("{buildDll}", buildDll);
            var sourceFiles = sourceDir.Glob("**")
                .Exclude("bin")
                .Exclude("obj")
                .Exclude(".vs");
            var outputFiles = buildDll.Glob();

            return buildDll.IsOutOfDate(sourceFiles);
        }

        private static void RunTarget(string[] targetAndArguments, object targets)
        {
            var targetName = targetAndArguments.FirstOrDefault();
            var target = (targetName == null)
                ? GetDefaultTarget(targets)
                : HelpText.PublicTargets(targets.GetType()).FindByName(
                    _ => GetOptParser.GetLongOptionNameForMember(_.Name),
                    targetName);
            var arguments = targetAndArguments.Skip(1).ToArray();
            Logger.Debug("{targetName} selects target {target}", targetName, target.Name);

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
