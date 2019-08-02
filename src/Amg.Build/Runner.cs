using Amg.CommandLine;
using Castle.DynamicProxy;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

            if (options.Help)
            {
                HelpText.Print(Console.Out, options);
                return ExitCodeHelpDisplayed;
            }

            var amgBuildAssembly = typeof(Target).Assembly;

            var thisDll = Assembly.GetExecutingAssembly().Location;
            var sourceDir = thisDll.Parent().Parent().Parent().Parent();
            var sourceFiles = sourceDir.Glob()
                .Exclude("bin")
                .Exclude("obj")
                .Exclude(".vs");

            if (options.Clean || thisDll.IsOutOfDate(sourceFiles))
            {
                Console.Error.WriteLine("Build script requires rebuild.");
                return ExitCodeRebuildRequired;
            }

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(SerilogLogEventLevel(options.Verbosity))
                .CreateLogger();

            Logger.Information("{assembly} {build}", amgBuildAssembly.Location, amgBuildAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);

            try
            {
                RunTarget(options.TargetAndArguments, options.targets);
                return ExitCodeSuccess;
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex, "Build failed.");
                return ExitCodeUnknownError;
            }
        }

        private static void RunTarget(string[] targetAndArguments, object targets)
        {
            var targetName = targetAndArguments.FirstOrDefault();
            var target = (targetName == null)
                ? HelpText.Targets(targets.GetType()).FindByName(_ => _.Name, "Default")
                : HelpText.PublicTargets(targets.GetType()).FindByName(_ => _.Name, targetName);
            var arguments = targetAndArguments.Skip(1).ToArray();

            var result = Wait(GetOptParser.Invoke(targets, target, arguments));
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
