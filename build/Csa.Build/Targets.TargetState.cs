using System;
using System.Threading.Tasks;

namespace Csa.Build
{

    partial class Targets
    {
        class TargetState : TargetStateBase
        {
            private readonly Func<Task> worker;
            public Task result;
            bool done = false;

            public TargetState(string id, Func<Task> worker)
            {
                this.id = id;
                this.worker = worker;
            }

            async Task RunOnce()
            {
                Banner($"begin {id}");
                begin = DateTime.UtcNow;
                try
                {
                    await worker();
                    Banner($"end {id}");
                }
                catch (Exception exception)
                {
                    this.exception = exception;
                    Banner($"fail {id}\r\n{exception}");
                    throw new Exception($"fail {id}", exception);
                }
                finally
                {
                    end = DateTime.UtcNow;
                }
            }

            public Task Run()
            {
                if (!done)
                {
                    result = RunOnce();
                    done = true;
                }
                return result;
            }
        }
    }
}
