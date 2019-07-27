using System;
using System.Threading.Tasks;

namespace Amg.Build
{
    public partial class Targets
    {
        class TargetState<Result> : TargetStateBase
        {
            private readonly Func<Task<Result>> worker;
            public Task<Result> result;
            bool done = false;

            public TargetState(string id, Func<Task<Result>> worker)
            {
                this.Id = id;
                this.worker = worker;
            }

            async Task<Result> RunOnce()
            {
                try
                {
                    Logger.Information("begin: {target}", this);
                    Begin = DateTime.UtcNow;
                    var result = await worker();
                    Logger.Information("success: {target} returns {result}", this, result);
                    return result;
                }
                catch (Exception exception)
                {
                    this.exception = exception;
                    Logger.Error("fail: {target}\r\n{exception}", this, exception);
                    throw new Exception($"fail {Id}", exception);
                }
                finally
                {
                    End = DateTime.UtcNow;
                }
            }

            public Task<Result> Run()
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
