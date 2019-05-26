using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;

namespace Csa.Build
{
    delegate Task Target();
    delegate Task<T> Target<T>();

    partial class Targets
    {
        static void Banner(string message)
        {
            Console.WriteLine();
            Console.WriteLine(new string('=', 80));
            Console.WriteLine(message);
            Console.WriteLine(new string('=', 80));
            Console.WriteLine();
        }

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
                foreach (var i in targets.Values.OrderBy(_ => _.end))
                {
                    Console.WriteLine($"{i.id}: {i.Duration.TotalSeconds:F2} {i.State}");
                }
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
                if (targets.TryGetValue(id, out var targetState))
                {

                }
                else
                {
                    targetState = targets[id] = new TargetState(id, f);
                }
                return ((TargetState)targetState).Run;
            }
        }

        protected Target<T> DefineTarget<T>(Func<Task<T>> f, [CallerMemberName] string name = null)
        {
            lock (targets)
            {
                var id = name;
                if (targets.TryGetValue(id, out var targetState))
                {

                }
                else
                {
                    targetState = targets[id] = new TargetState<T>(id, f);
                }
                return ((TargetState<T>)targetState).Run;
            }
        }
    }
}
