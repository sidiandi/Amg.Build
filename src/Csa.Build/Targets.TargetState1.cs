using System;
using System.Threading.Tasks;

namespace Csa.Build
{
    public partial class Targets
    {
        class TargetState<T> : TargetStateBase
        {
            private readonly Func<Task<T>> worker;
            public Task<T> result;
            bool done = false;

            public TargetState(string id, Func<Task<T>> worker)
            {
                this.id = id;
                this.worker = worker;
            }

            async Task<T> RunOnce()
            {
                try
                {
                    Banner($"begin {id}");
                    begin = DateTime.UtcNow;
                    var r = await worker();
                    Banner($"end {id}\r\n\r\nResult\r\n{r.Dump()}");
                    return r;
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

            public Task<T> Run()
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
