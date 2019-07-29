using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;
using System.ComponentModel;
using Amg.CommandLine;
using System.IO;
using Serilog;
using Serilog.Events;

[assembly: InternalsVisibleTo("Amg.Build.Tests")]

namespace Amg.Build
{
    /// <summary>
    /// Build target with no input and no output
    /// </summary>
    /// <returns></returns>
    public delegate Task Target();

    /// <summary>
    /// Build target with no input and output Output
    /// </summary>
    /// <typeparam name="Output"></typeparam>
    /// <returns></returns>
    public delegate Task<Output> Target<Output>();

    /// <summary>
    /// Build target with input and output
    /// </summary>
    /// <typeparam name="Input"></typeparam>
    /// <typeparam name="Output"></typeparam>
    /// <param name="a"></param>
    /// <returns></returns>
    public delegate Task<Output> Target<Input, Output>(Input a);

    /// <summary>
    /// Derive from this class to implement your own container of build targets.
    /// </summary>
    /// This is the central class in Amg.Build.
    public partial class Targets
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        TargetProgressLog targetLog = new TargetProgressLog();

        /// <summary>
        /// Set this with your own implementation to customize the recording of execution of build targets.
        /// </summary>
        public TargetProgress Progress { get; set; }

        enum Verbosity
        {
            Quiet,
            Minimal,
            Normal,
            Detailed
        };

        const int ExitCodeHelpDisplayed = 1;
        const int ExitCodeUnknownError = -1;
        const int ExitCodeSuccess = 0;
        const int ExitCodeRebuildRequired = 2;

        /// <summary />
        public Targets()
        {
            Progress = targetLog;
        }

        class Options<TargetsDerivedClass>
        {
            public TargetsDerivedClass targets { get; set; }

            public Options(TargetsDerivedClass targets)
            {
                this.targets = targets;
            }

            [Operands]
            [Description("Build targets")]
            public string[] Targets { get; set; }  = new string[] { };

            [Short('h'), Description("Show help and exit")]
            public bool Help { get; set; }

            [Description("Force a rebuild of the build script")]
            public bool Clean { get; set; }

            [Short('v'), Description("Set the verbosity level.")]
            public Verbosity Verbosity { get; set; } = Verbosity.Normal;
        }

        /// <summary>
        /// Creates an TargetsDerivedClass instance and runs the contained targets according to the passed commandLineArguments.
        /// </summary>
        /// Call this method directly from your Main()
        /// <typeparam name="TargetsDerivedClass"></typeparam>
        /// <param name="commandLineArguments"></param>
        /// <returns>Exit code: 0 if success, unequal to 0 otherwise.</returns>
        public static int Run<TargetsDerivedClass>(string[] commandLineArguments) where TargetsDerivedClass : Targets, new()
        {
            var options = new Options<TargetsDerivedClass>(new TargetsDerivedClass());
            GetOptParser.Parse(commandLineArguments, options);

            if (options.Help)
            {
                PrintHelp(Console.Out, options);
                return ExitCodeHelpDisplayed;
            }

            var thisDll = Assembly.GetExecutingAssembly().Location;
            var sourceDir = thisDll.Parent().Parent().Parent().Parent();
            var sourceFiles = sourceDir.Glob()
                .Exclude("bin")
                .Exclude("obj")
                .Exclude(".vs");

            if (options.Clean || thisDll.IsOutOfDate(sourceFiles))
            {
                return ExitCodeRebuildRequired;
            }

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(SerilogLogEventLevel(options.Verbosity))
                .CreateLogger();

            try
            {
                options.targets.RunTargets(options.Targets).Wait();
                return ExitCodeSuccess;
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex, "Build failed.");
                return ExitCodeUnknownError;
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

        private static void PrintHelp<TargetsDerivedClass>(TextWriter @out, Options<TargetsDerivedClass> options) where TargetsDerivedClass : Targets, new()
        {
            @out.WriteLine(@"Usage: build <targets> [options]

Targets:");
            PrintTargetsList(@out, options.targets);
            @out.WriteLine(@"
Options:");
            PrintOptionsList(@out, options);
        }

        const string indent = " ";

        private static void PrintOptionsList<TargetsDerivedClass>(TextWriter @out, Options<TargetsDerivedClass> options) where TargetsDerivedClass : Targets, new()
        {
            GetOptParser.GetOptions(options)
                .Where(_ => !_.IsOperands)
                .Select(_ => new { indent, _.Syntax, _.Description })
                .ToTable(header: false)
                .Write(@out);
        }

        private static void PrintTargetsList<TargetsDerivedClass>(TextWriter @out, TargetsDerivedClass targets) where TargetsDerivedClass : Targets, new()
        {
            var publicTargets = targets.GetPublicTargetProperties();
            publicTargets
                .Select(_ => new { indent, _.Name, Description = GetDescription(_) })
                .ToTable(header: false)
                .Write(@out);
        }

        private static string GetDescription(PropertyInfo p)
        {
            var a = p.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
            return a == null
                ? String.Empty
                : a.Description;
        }

        const string DefaultTargetName = "Default";

        IEnumerable<Target> GetTargets(IEnumerable<string> targetNames)
        {
            targetNames = targetNames.Any()
                ? targetNames
                : new[] { DefaultTargetName };

            return targetNames.Select(GetTarget);
        }

        internal async Task RunTargets(IEnumerable<string> targetNames)
        {
            try
            {
                var targets = GetTargets(targetNames);
                await Run(targets);
            }
            finally
            {
                targetLog.PrintSummary(Console.Out);
                targetLog.PrintErrorSummary(Console.Error);
            }
        }
        IEnumerable<Target> GetTargets()
        {
            return GetType().GetProperties(
                BindingFlags.NonPublic |
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly
                )
                .Where(_ => typeof(Target).IsAssignableFrom(_.PropertyType))
                .Select(GetTarget)
                .ToList();
        }

        internal Target GetTarget(PropertyInfo p)
        {
            var targetDelegate = p.GetValue(this, new object[] { });
            if (targetDelegate is Target)
            {
                return (Target)targetDelegate;
            }

            if (targetDelegate is MulticastDelegate)
            {
                var mcd = (MulticastDelegate)targetDelegate;
                return new Target(() =>
                {
                    var r = (Task) mcd.DynamicInvoke(new object[] { });
                    return r;
                });
            }

            throw new NotImplementedException();
        }

        IEnumerable<PropertyInfo> GetTargetProperties()
        {
            return GetType().GetProperties(
                BindingFlags.NonPublic |
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly
                )
                .Where(IsTargetProperty)
                .ToList();
        }

        IEnumerable<PropertyInfo> GetPublicTargetProperties()
        {
            return GetType().GetProperties(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly
                )
                .Where(IsPublicTargetProperty)
                .ToList();
        }

        internal static bool IsTargetProperty(PropertyInfo p)
        {
            var isMulticast = typeof(MulticastDelegate).IsAssignableFrom(p.PropertyType);
            return isMulticast;
        }

        internal static bool IsPublicTargetProperty(PropertyInfo p)
        {
            return IsTargetProperty(p) &&
                p.GetCustomAttribute<DescriptionAttribute>() != null;
        }

        Target GetTarget(string name)
        {
            return GetTarget(GetTargetProperties().FindByName(_ => _.Name, name));
        }

        async Task Run(IEnumerable<Target> targets)
        {
            var tasks = targets.Select(_ => _()).ToList();
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Define a target.
        /// </summary>
        /// Usage pattern:
        /// <![CDATA[
        /// [Description("Compile source code")]
        /// Target Compile => DefineTarget(async () =>
        /// {
        ///     ...
        /// });
        /// ]]>
        /// Will always return the same instance for the same name.
        /// <param name="f">Work to be done when the target executes.</param>
        /// <param name="name">Name of the target</param>
        /// <returns></returns>
        protected Target DefineTarget(Func<Task> f, [CallerMemberName] string name = null)
        {
            var t = DefineTarget<Nothing, Nothing>(async (nothing) =>
            {
                await f();
                return Nothing.Instance;
            }, name);

            return () => t(Nothing.Instance);
        }

        /// <summary>
        /// Define a target.
        /// </summary>
        protected Target DefineTarget(Action f, [CallerMemberName] string name = null)
        {
            var t = DefineTarget<Nothing, Nothing>(async (nothing) =>
            {
                await Task.Factory.StartNew(f, TaskCreationOptions.LongRunning);
                return Nothing.Instance;
            }, name);
            return () => t(Nothing.Instance);
        }

        /// <summary>
        /// Define a target.
        /// </summary>
        protected Target<Output> DefineTarget<Output>(Func<Output> f, [CallerMemberName] string name = null)
        {
            return DefineTarget(AsyncHelper.ToAsync(f), name);
        }

        /// <summary>
        /// Define a target.
        /// </summary>
        protected Target<Output> DefineTarget<Output>(Func<Task<Output>> f, [CallerMemberName] string name = null)
        {
            var t = DefineTarget<Nothing, Output>((nothing) => f(), name);
            return () => t(Nothing.Instance);
        }

        /// <summary>
        /// Define a target.
        /// </summary>
        protected Target<Input, Output> DefineTarget<Input, Output>(Func<Input, Output> f, [CallerMemberName] string name = null)
        {
            return DefineTarget(AsyncHelper.ToAsync(f), name);
        }

        /// <summary>
        /// Define a target.
        /// </summary>
        Dictionary<string, TargetDefinitionBase> targets = new Dictionary<string, TargetDefinitionBase>();

        /// <summary>
        /// Define a target with input and output
        /// </summary>
        protected Target<Input, Output> DefineTarget<Input, Output>(Func<Input, Task<Output>> f, [CallerMemberName] string name = null)
        {
            var definition = (TargetDefinition<Input, Output>) targets.GetOrAdd(name, () => new TargetDefinition<Input, Output>(name, f, Progress));
            return definition.Run;
        }

        /// <summary>
        /// Root directory of the build. That is the directory which contains the `build.cmd` script.
        /// </summary>
        /// <param name="thisSource"></param>
        /// <returns></returns>
        public string GetRootDirectory([CallerFilePath] string thisSource = null)
        {
            return thisSource.Parent().Parent();
        }

        /// <summary>
        /// Define a subtargets class instance.
        /// </summary>
        /// Usage pattern in your Targets derived class:
        /// <![CDATA[
        /// Git Git => DefineTargets(() => new Git());
        /// ]]>
        /// <typeparam name="MyTargets"></typeparam>
        /// <param name="factory"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public MyTargets DefineTargets<MyTargets>(Func<MyTargets> factory, [CallerMemberName] string name = null) where MyTargets : Targets
        {
            return (MyTargets) subTargets.GetOrAdd(name, () =>
            {
                var targets = (Targets) factory();
                targets.Progress = new PrefixProgress(this.Progress, name + ".");
                return targets;
            });
        }

        IDictionary<string, Targets> subTargets = new Dictionary<string, Targets>();
    }
}
