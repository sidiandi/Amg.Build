using System;
using System.Threading.Tasks;

namespace Csa.Build
{

    public partial class Targets
    {
        class TargetState : TargetStateBase
        {
            private readonly Func<Task> worker;
            public Task result;
            bool done = false;

            public TargetState(string id, Func<Task> worker)
            {
                this.Id = id;
                this.worker = worker;
            }

            async Task RunOnce()
            {
                Logger.Information("begin {id}", Id);
                Begin = DateTime.UtcNow;
                try
                {
                    await worker();
                    Logger.Information("end {id}", Id);
                }
                catch (Exception exception)
                {
                    this.exception = exception;
                    Logger.Error("fail {id}\r\n{exception}", Id, exception);
                    throw new Exception($"fail {Id}", exception);
                }
                finally
                {
                    End = DateTime.UtcNow;
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
