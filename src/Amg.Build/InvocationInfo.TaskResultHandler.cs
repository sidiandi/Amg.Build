namespace Amg.Build;

partial class InvocationInfo
{
    class TaskResultHandler<Result> : IReturnValueSource where Result : class
    {
        private readonly InvocationInfo invocationInfo;
        private readonly Task<Result?> task;

        public TaskResultHandler(InvocationInfo invocationInfo, Task<Result?> task)
        {
            this.invocationInfo = invocationInfo;
            this.task = task;
        }

        async Task<Result?> GetReturnValue()
        {
            var result = GetReturnValue2();
            await Task.WhenAny(result, this.invocationInfo.interceptor!._waitUntilCancelled);
            return await result;
        }

        async Task<Result?> GetReturnValue2()
        {
            try
            {
                var r = await task;
                r = (Result?)invocationInfo.InterceptReturnValue(r);
                invocationInfo.Complete();
                return r;
            }
            catch (Exception ex)
            {
                throw invocationInfo.Fail(ex);
            }
        }

        public object ReturnValue => GetReturnValue();
    }
}
