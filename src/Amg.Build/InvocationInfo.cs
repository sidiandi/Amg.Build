using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace Amg.Build
{
    class InvocationInfo
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

        private readonly IInvocation? invocation;
        private readonly OnceInterceptor? interceptor;
        public Exception? Exception { get; private set; }

        public InvocationInfo(string id, DateTime begin, DateTime end)
        {
            this.Id = id;
            Begin = begin;
            End = end;
            invocation = null;
            interceptor = null;
            Exception = null;
        }

        interface IReturnValueSource
        {
            object ReturnValue { get; }
        }

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
                try
                {
                    var r = await task;
                    r = (Result?) invocationInfo.InterceptReturnValue(r);
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
                    invocationInfo.Complete();
                }
                catch (Exception ex)
                {
                    throw invocationInfo.Fail(ex);
                }
            }

            public object ReturnValue => GetReturnValue();
        }

        internal static bool TryGetResultType(Task task, out Type resultType)
        {
            var taskType = task.GetType();
            if (taskType.IsGenericType)
            {
                if (!taskType.GenericTypeArguments[0].Name.Equals("VoidTaskResult"))
                {
                    resultType = taskType.GenericTypeArguments[0];
                    return true;
                }
            }

            resultType = null!;
            return false;
        }

        public InvocationInfo(OnceInterceptor interceptor, string id, IInvocation invocation)
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
                Logger.Information("{task} suceeded.", this);
                Complete();
            }
        }

        void Complete()
        {
            End = DateTime.UtcNow;
            Logger.Information("{target} suceeded", this);
        }

        private InvocationFailed Fail(Exception exception)
        {
            End = DateTime.UtcNow;
            this.Exception = exception;
            Logger.Fatal(@"{target} failed.
{exception}", this, Summary.ErrorDetails(this));
            Console.Error.WriteLine($"{this} failed.");
            var invocationFailed = new InvocationFailed(this);
            return invocationFailed;
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

        internal object? InterceptReturnValue(object? x)
        {
            return x;
        }

        public override string ToString() => $"Target {Id.OneLine().Truncate(32)}";

        public object? ReturnValue => invocation.Map(_ => _.ReturnValue);

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

    static class InvocationInfoExtensions
    {
        public static bool Failed(this IEnumerable<InvocationInfo> invocations)
        {
            return invocations.Any(_ => _.Failed);
        }
    }

}
