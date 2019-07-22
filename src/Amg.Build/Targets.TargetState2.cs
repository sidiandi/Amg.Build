using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Amg.Build
{
    public partial class Targets
    {
        class TargetState<Arg, Result> : TargetStateBase
        {
            private readonly Func<Arg, Task<Result>> worker;
            readonly IDictionary<Arg, TargetState<Result>> results = new Dictionary<Arg, TargetState<Result>>();

            public override DateTime? Begin
            {
                get
                {
                    return results.Values.Select(_ => _.End).Min();
                }
                set { }
            }

            public override DateTime? End
            {
                get
                {
                    return results.Values.Select(_ => _.End).Max();
                }
                set { }
            }

            public TargetState(string id, Func<Arg, Result> worker)
                :this(id, AsyncHelper.ToAsync(worker))
            {
            }

            public TargetState(string id, Func<Arg, Task<Result>> worker)
            {
                this.Id = id;
                this.worker = worker;
            }

            public Task<Result> Run(Arg a)
            {
                var state = results.GetOrAdd(a, () => new TargetState<Result>($"{Id}({a})", () => this.worker(a)));
                return state.Run();
            }
        }
    }
}
