using System;
using System.Threading.Tasks;

namespace Amg.Build
{
    partial class InvocationInfo
    {
        class TaskHandler : IReturnValueSource
        {
            private readonly InvocationInfo invocationInfo;
            private readonly Task task;

            public TaskHandler(InvocationInfo invocationInfo, Task task)
            {
                this.invocationInfo = invocationInfo;
                this.task = task;
            }

            async Task GetReturnValue()
            {
                var result = GetReturnValue2();
                await Task.WhenAny(result, this.invocationInfo.interceptor!._waitUntilCancelled);
                await result;
            }

            async Task GetReturnValue2()
            {
                try
                {
                    await task;
                    invocationInfo.Complete();
                }
                catch (Exception ex)
                {
                    throw invocationInfo.Fail(ex);
                }
            }

            public object ReturnValue => GetReturnValue();
        }
    }

}
