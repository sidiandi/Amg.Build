using System;
using System.Threading.Tasks;
using Amg.Extensions;
using Castle.DynamicProxy;

namespace Amg.Build
{
    partial class InvocationInfo : IInvocation
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

        private readonly Castle.DynamicProxy.IInvocation? invocation;
        private readonly OnceInterceptor? interceptor;
        public Exception? Exception { get; private set; }

        public InvocationInfo(string id, DateTime begin, DateTime end)
        {
            this.Id = new InvocationId
            (
                instanceId: String.Empty,
                method: id,
                arguments: new object[] { }
            );

            Begin = begin;
            End = end;
            invocation = null;
            interceptor = null;
            Exception = null;
        }

        public InvocationInfo(OnceInterceptor interceptor, InvocationId id, Castle.DynamicProxy.IInvocation invocation)
        {
            this.interceptor = interceptor;
            this.invocation = invocation;
            this.Id = id;

            Logger.Information("{task} started", this);
            Begin = DateTime.UtcNow;
            invocation.Proceed();
            if (ReturnValue is Task task)
            {
                if (TryGetResultType(task, out var resultType))
                {
                    var resultHandlerType = typeof(TaskResultHandler<>).MakeGenericType(resultType);
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

        interface IReturnValueSource
        {
            object ReturnValue { get; }
        }

        internal static bool TryGetResultType(Task task, out Type resultType)
        {
            var taskType = task.GetType();
            if (taskType.IsGenericType && (!taskType.GenericTypeArguments[0].Name.Equals("VoidTaskResult")))
            {
                resultType = taskType.GenericTypeArguments[0];
                return true;
            }

            resultType = null!;
            return false;
        }

        internal static Type? TryGetTaskResultType(Type taskType)
        {
            if (taskType.IsGenericType && (!taskType.GenericTypeArguments[0].Name.Equals("VoidTaskResult")))
            {
                return taskType.GenericTypeArguments[0];
            }

            return null;
        }

        void Complete()
        {
            End = DateTime.UtcNow;
            Logger.Information("{target} succeeded", this);
        }

        private InvocationFailedException Fail(Exception exception)
        {
            End = DateTime.UtcNow;
            this.Exception = exception;
            Logger.Fatal(@"{target} failed. Reason: {exception}", this,
                Logger.IsEnabled(Serilog.Events.LogEventLevel.Information)
                    ? Summary.ErrorDetails(this)
                    : Summary.ShortErrorDetails(this)
                    );
            var invocationFailed = new InvocationFailedException(this);
            return invocationFailed;
        }

        public virtual DateTime? Begin { get; set; }
        public virtual DateTime? End { get; set; }
        public InvocationId Id { get; }
        public virtual TimeSpan Duration
        {
            get
            {
                return Begin.HasValue && End.HasValue
                    ? (End.Value - Begin.Value)
                    : TimeSpan.Zero;
            }
        }

        internal object? InterceptReturnValue(object? x)
        {
            return x;
        }

        public override string ToString() => Id.ToString();

        public object? ReturnValue => invocation.Map(_ => _.ReturnValue);

        public InvocationState State
        {
            get
            {
                if (Begin.HasValue)
                {
                    if (End.HasValue)
                    {
                        if (Exception == null)
                        {
                            return InvocationState.Done;
                        }
                        else
                        {
                            return InvocationState.Failed;
                        }
                    }
                    else
                    {
                        return InvocationState.InProgress;
                    }
                }
                else
                {
                    return InvocationState.Pending;
                }
            }
        }

    }
}
