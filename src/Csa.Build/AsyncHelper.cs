using System;
using System.Threading.Tasks;

namespace Csa.Build
{
    static class AsyncHelper
    {
        public static Func<Arg, Task<Result>> ToAsync<Arg, Result>(this Func<Arg, Result> f)
        {
            return a => Task.Factory.StartNew(() => f(a), TaskCreationOptions.LongRunning);
        }

        public static Func<Task<Result>> ToAsync<Result>(this Func<Result> f)
        {
            return () => Task.Factory.StartNew(f, TaskCreationOptions.LongRunning);
        }

        public static Func<Task> ToAsync(this Action f)
        {
            return () => Task.Factory.StartNew(f, TaskCreationOptions.LongRunning);
        }
    }
}
