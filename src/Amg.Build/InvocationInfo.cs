using System;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace Amg.Build
{
    class InvocationInfo
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IInvocation invocation;
        private readonly OnceInterceptor interceptor;

        static AsyncLocal<InvocationInfo> currentInvocationInfo = new AsyncLocal<InvocationInfo>();

        public static InvocationInfo Current
        {
            get
            {
                return currentInvocationInfo.Value;
            }

            private set
            {
                currentInvocationInfo.Value = value;
            }
        }

        public InvocationInfo(string id, DateTime begin, DateTime end)
        {
            this.Id = id;
            Begin = begin;
            End = end;
        }

        interface IReturnValueSource
        {
            object ReturnValue { get; }
        }

        class TaskResultHandler<Result> : IReturnValueSource
        {
            private readonly InvocationInfo invocationInfo;
            private readonly Task<Result> task;

            public TaskResultHandler(InvocationInfo invocationInfo, Task<Result> task)
            {
                this.invocationInfo = invocationInfo;
                this.task = task;
            }

            async Task<Result> GetReturnValue()
            {
                try
                {
                    var r = await task;
                    r = (Result)invocationInfo.InterceptReturnValue(r);
                    return r;
                }
                catch (Exception ex)
                {
                    invocationInfo.Exception = ex;
                    Logger.Fatal("{target} failed: {exception}", invocationInfo, InvocationFailed.ShortMessage(ex));
                    throw new InvocationFailed(ex, invocationInfo);
                }
                finally
                {
                    invocationInfo.Complete();
                }
            }

            public object ReturnValue => GetReturnValue();
        }

        class TaskHandler : IReturnValueSource
        {
            private InvocationInfo invocationInfo;
            private Task task;

            public TaskHandler(InvocationInfo invocationInfo, Task task)
            {
                this.invocationInfo = invocationInfo;
                this.task = task;
            }

            async Task GetReturnValue()
            {
                try
                {
                    await task;
                }
                catch (Exception ex)
                {
                    invocationInfo.Exception = ex;
                    Logger.Fatal("{target} failed: {exception}", invocationInfo, InvocationFailed.ShortMessage(ex));
                    throw new InvocationFailed(ex, invocationInfo);
                }
                finally
                {
                    invocationInfo.Complete();
                }
            }

            public object ReturnValue => GetReturnValue();
        }

        public InvocationInfo(OnceInterceptor interceptor, string id, IInvocation invocation)
        {
            this.interceptor = interceptor;
            this.invocation = invocation;
            this.Id = id;

            InvocationInfo.Current = this;
            Logger.Information("Begin {id}", Id);
            Begin = DateTime.UtcNow;
            invocation.Proceed();
            if (ReturnValue is Task task)
            {
                var returnType = task.GetType();
                if (returnType.IsGenericType)
                {
                    var resultHandlerType = typeof(TaskResultHandler<>)
                      .MakeGenericType(returnType.GetGenericArguments()[0]);
                    var resultHandler = (IReturnValueSource)Activator.CreateInstance(resultHandlerType, this, task);
                    invocation.ReturnValue = resultHandler.ReturnValue;
                }
                else
                {
                    var handler = new TaskHandler(this, task);
                    invocation.ReturnValue = handler.ReturnValue;
                }
            }
            else
            {
                invocation.ReturnValue = InterceptReturnValue(invocation.ReturnValue);
                Complete();
            }
        }

        void Complete()
        {
            End = DateTime.UtcNow;
            Logger.Information("End {id}", Id);
            InvocationInfo.Current = null;
        }

        public virtual DateTime? Begin { get; set; }
        public virtual DateTime? End { get; set; }
        public string Id { get; set; }
        public virtual TimeSpan Duration
        {
            get
            {
                return Begin.HasValue && End.HasValue
                    ? (End.Value - Begin.Value)
                    : TimeSpan.Zero;
            }
        }

        internal object InterceptReturnValue(object x)
        {
            if (Amg.Build.Once.HasOnceMethods(x))
            {
                /*
                var builder = new DefaultProxyBuilder();
                var generator = new ProxyGenerator(builder);
                var onceProxy = generator.CreateClassProxyWithTarget(x.GetType(), x,
                    new ProxyGenerationOptions
                    {
                        Hook = new OnceHook()
                    },
                    new OnceInterceptor(interceptor, invocation.Method.Name),
                    new LogInvocationInterceptor());
                return onceProxy;
                */
                return x;
            }
            else
            {
                return x;
            }
        }

        public override string ToString() => Id.OneLine().Truncate(32);

        public Exception Exception { get; set; }
        public object ReturnValue => invocation.ReturnValue;

        public enum States
        {
            Pending,
            InProgress,
            Done,
            Failed
        }

        public States State
        {
            get
            {
                if (Begin.HasValue)
                {
                    if (End.HasValue)
                    {
                        if (Exception == null)
                        {
                            return States.Done;
                        }
                        else
                        {
                            return States.Failed;
                        }
                    }
                    else
                    {
                        return States.InProgress;
                    }
                }
                else
                {
                    return States.Pending;
                }
            }
        }

        public bool Failed => Exception != null;
    }

}
