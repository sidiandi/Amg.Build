using System;
using System.Threading.Tasks;

namespace Amg.Build
{

    public partial class Targets
    {
        internal class TargetState : TargetStateBase
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
                Logger.Information("start: {target}", this);
                Begin = DateTime.UtcNow;
                try
                {
                    await worker();
                    Logger.Information("success: {target}", this);
                }
                catch (Exception exception)
                {
                    this.exception = new TargetFailed(this, exception);
                    Logger.Error(this.exception, "fail: {target}", this);
                    throw this.exception;
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
