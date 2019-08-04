using System;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace Amg.Build
{
    class InvocationInfo
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IInvocation invocation;
        private readonly OnceInterceptor interceptor;

        public InvocationInfo(string id, DateTime begin, DateTime end)
        {
            this.Id = id;
            Begin = begin;
            End = end;
        }

        public InvocationInfo(OnceInterceptor interceptor, string id, IInvocation invocation)
        {
            this.interceptor = interceptor;
            this.invocation = invocation;
            this.Id = id;

            Logger.Information("Begin {id}", Id);
            Begin = DateTime.UtcNow;
            invocation.Proceed();
            invocation.ReturnValue = InterceptReturnValue(invocation.ReturnValue);
            if (ReturnValue is Task task)
            {
                task.ContinueWith(_ =>
                {
                    Complete();
                    if (_.IsFaulted)
                    {
                        Exception = _.Exception;
                    }
                });
            }
            else
            {
                Complete();
            }
        }

        void Complete()
        {
            End = DateTime.UtcNow;
            Logger.Information("End {id}", Id);
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
            }
            else
            {
                return x;
            }
        }

        public override string ToString() => $"Target {Id}";

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
