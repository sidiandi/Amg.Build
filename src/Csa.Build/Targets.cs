using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;

namespace Csa.Build
{
    public delegate Task Target();
    public delegate Task<Result> Target<Result>();
    public delegate Task<Result> Target<Arg, Result>(Arg a);

    public partial class Targets
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public async Task<int> Run(string[] args)
        {
            try
            {
                if (args.Any())
                {
                    await Run(args.Select(GetTarget));
                }
                else
                {
                    await Run(GetTargets());
                }
                return 0;
            }
            catch
            {
                return 1;
            }
            finally
            {
                Console.WriteLine(
                    targets.Values.OrderBy(_ => _.End)
                    .Select(_ => new { _.Id, _.Duration, _.State })
                    .ToTable()
                    );
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
                .Select(_ => (Target)_.GetValue(this, new object[] { }))
                .ToList();
        }

        Target GetTarget(string name)
        {
            return (new[] { GetType().GetProperty(name,
                BindingFlags.NonPublic |
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly |
                BindingFlags.IgnoreCase
                ) })
                .Where(_ => typeof(Target).IsAssignableFrom(_.PropertyType))
                .Select(_ => (Target)_.GetValue(this, new object[] { }))
                .Single();
        }

        async Task Run(IEnumerable<Target> targets)
        {
            foreach (var target in targets)
            {
                await target();
            }
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
