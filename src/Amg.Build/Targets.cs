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
    public delegate Task Target();
    public delegate Task<Result> Target<Result>();
    public delegate Task<Result> Target<Arg, Result>(Arg a);

    public partial class Targets
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public TargetProgress Progress => targetLog;
        TargetProgressLog targetLog = new TargetProgressLog();

        enum Verbosity
        {
            Quiet,
            Minimal,
            Normal,
            Detailed
        };

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

            [Short('v'), Description("Set the verbosity level.")]
            public Verbosity Verbosity { get; set; } = Verbosity.Normal;
        }

        public static int Run<TargetsDerivedClass>(string[] args) where TargetsDerivedClass : Targets, new()
        {
            var options = new Options<TargetsDerivedClass>(new TargetsDerivedClass());
            GetOptParser.Parse(args, options);
            if (options.Help)
            {
                PrintHelp(Console.Out, options);
                return 1;
            }

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(SerilogLogEventLevel(options.Verbosity))
                .CreateLogger();

            try
            {
                options.targets.RunTargets(options.Targets).Wait();
                return 0;
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex, "Build failed.");
                return -1;
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

        protected Target DefineTarget(Func<Task> f, [CallerMemberName] string name = null)
        {
            var t = DefineTarget<Nothing, Nothing>(async (nothing) =>
            {
                await f();
                return Nothing.Instance;
            }, name);

            return () => t(Nothing.Instance);
        }

        protected Target DefineTarget(Action f, [CallerMemberName] string name = null)
        {
            var t = DefineTarget<Nothing, Nothing>(async (nothing) =>
            {
                await Task.Factory.StartNew(f, TaskCreationOptions.LongRunning);
                return Nothing.Instance;
            }, name);
            return () => t(Nothing.Instance);
        }

        protected Target<Output> DefineTarget<Output>(Func<Output> f, [CallerMemberName] string name = null)
        {
            return DefineTarget(AsyncHelper.ToAsync(f), name);
        }

        protected Target<Output> DefineTarget<Output>(Func<Task<Output>> f, [CallerMemberName] string name = null)
        {
            var t = DefineTarget<Nothing, Output>((nothing) => f(), name);
            return () => t(Nothing.Instance);
        }

        protected Target<Input, Output> DefineTarget<Input, Output>(Func<Input, Output> f, [CallerMemberName] string name = null)
        {
            return DefineTarget(AsyncHelper.ToAsync(f), name);
        }

        Dictionary<string, TargetDefinitionBase> targets = new Dictionary<string, TargetDefinitionBase>();

        protected Target<Input, Output> DefineTarget<Input, Output>(Func<Input, Task<Output>> f, [CallerMemberName] string name = null)
        {
            Logger.Information("Define target {name}", name);
            var definition = (TargetDefinition<Input, Output>) targets.GetOrAdd(name, () => new TargetDefinition<Input, Output>(name, f, Progress));
            return definition.Run;
        }
    }
}
