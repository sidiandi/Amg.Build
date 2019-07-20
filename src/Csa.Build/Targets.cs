using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;
using System.ComponentModel;
using Csa.CommandLine;
using System.IO;
using Serilog;

[assembly: InternalsVisibleTo("Csa.Build.Tests")]

namespace Csa.Build
{
    public delegate Task Target();
    public delegate Task<Result> Target<Result>();
    public delegate Task<Result> Target<Arg, Result>(Arg a);

    public partial class Targets
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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

            [Short('v'), Description("Increase verbosity")]
            public bool Verbose { get; set; }
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
                .WriteTo.Console(Serilog.Events.LogEventLevel.Debug)
                .CreateLogger();

            try
            {
                options.targets.RunTargets(options.Targets).Wait();
                return 0;
            }
            catch (Exception ex)
            {
                return -1;
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
            var publicTargets = targets.GetTargetProperties();
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
                PrintSummary(Console.Out, targets.Values);
            }
        }

        static DateTime? Max(DateTime? a, DateTime? b)
        {
            return (a == null)
                ? b
                : b == null
                    ? null
                    : a.Value > b.Value
                        ? a
                        : b;
        }

        static DateTime? Min(DateTime? a, DateTime? b)
        {
            return (a == null)
                ? b
                : b == null
                    ? null
                    : a.Value < b.Value
                        ? a
                        : b;
        }

        static void PrintSummary(TextWriter @out, IEnumerable<TargetStateBase> targets)
        {
            var end = targets.Aggregate((DateTime?)null, (m, _) => Max(m, _.End));
            var begin = targets.Aggregate((DateTime?)null, (m, _) => Min(m, _.Begin));

            if (end == null || begin == null)
            {
                return;
            }

            new
            {
                Begin = begin,
                End = end,
                Duration = (end.Value - begin.Value).HumanReadable(),
            }.ToPropertiesTable().Write(@out);

            @out.WriteLine();

            targets.OrderBy(_ => _.End)
                .Select(_ => new
                {
                    _.Id,
                    Duration = _.Duration.HumanReadable(),
                    _.State,
                    Timeline = _.Begin.HasValue && _.End.HasValue
                        ? Extensions.TimeBar(80, begin.Value, end.Value, _.Begin.Value, _.End.Value)
                        : String.Empty
                })
                .ToTable().Write(@out);
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

        Target GetTarget(PropertyInfo p)
        {
            return (Target)p.GetValue(this, new object[] { });
        }

        IEnumerable<PropertyInfo> GetTargetProperties()
        {
            return GetType().GetProperties(
                BindingFlags.NonPublic |
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly
                )
                .Where(_ => typeof(Target).IsAssignableFrom(_.PropertyType))
                .ToList();
        }

        Target GetTarget(string name)
        {
            return GetTarget(GetTargetProperties().FindByName(_ => _.Name, name));
        }

        async Task Run(IEnumerable<Target> targets)
        {
            await Task.WhenAll(targets.Select(_ => _()));
        }

        Dictionary<string, TargetStateBase> targets = new Dictionary<string, TargetStateBase>();

        protected Target DefineTarget(Func<Task> f, [CallerMemberName] string name = null)
        {
            lock (targets)
            {
                var id = name;
                var targetState = targets.GetOrAdd(id, () => new TargetState(id, f));
                return ((TargetState)targetState).Run;
            }
        }

        protected Target DefineTarget(Action f, [CallerMemberName] string name = null)
        {
            return DefineTarget(AsyncHelper.ToAsync(f), name);
        }

        protected Target<Result> DefineTarget<Result>(Func<Task<Result>> f, [CallerMemberName] string name = null)
        {
            lock (targets)
            {
                var id = name;
                var targetState = targets.GetOrAdd(id, () => new TargetState<Result>(id, f));
                return ((TargetState<Result>)targetState).Run;
            }
        }

        protected Target<Arg, Result> DefineTarget<Arg, Result>(Func<Arg, Result> f, [CallerMemberName] string name = null)
        {
            lock (targets)
            {
                var id = name;
                var targetState = targets.GetOrAdd(id, () => new TargetState<Arg, Result>(id, f));
                return ((TargetState<Arg, Result>)targetState).Run;
            }
        }

        protected Target<Arg, Result> DefineTarget<Arg, Result>(Func<Arg, Task<Result>> f, [CallerMemberName] string name = null)
        {
            lock (targets)
            {
                var id = name;
                var targetState = targets.GetOrAdd(id, () => new TargetState<Arg, Result>(id, f));
                return ((TargetState<Arg, Result>)targetState).Run;
            }
        }
    }
}
