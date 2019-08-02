using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace Amg.Build
{
    internal class OnceInterceptor : IInterceptor
    {
        class Invocation
        {
            public Invocation(string id, object returnValue)
            {
                ReturnValue = returnValue;
                Begin = DateTime.UtcNow;
                Id = id;

                if (returnValue is Task task)
                {
                    task.ContinueWith(_ =>
                    {
                        End = DateTime.UtcNow;
                        if (_.IsFaulted)
                        {
                            Exception = _.Exception;
                        }
                    });
                }
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

            public override string ToString() => $"Target {Id}";

            public Exception Exception { get; private set; }
            public object ReturnValue { get; private set; }

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

        static readonly IDictionary<string, Invocation> _cache;

        static OnceInterceptor()
        {
            _cache = new Dictionary<string, Invocation>();
        }

        public void Intercept(IInvocation invocation)
        {
            var cacheKey = GenerateCacheKey(invocation.Method.Name, invocation.Arguments);
            invocation.ReturnValue = _cache.GetOrAdd(cacheKey, () =>
            {
                invocation.Proceed();
                return new Invocation(cacheKey, invocation.ReturnValue);
            }).ReturnValue;

        }

        static string GenerateCacheKey(string name, object[] arguments)
        {
            if (arguments == null || arguments.Length == 0)
                return name;
            return name + "--" + string.Join("--", arguments.Select(a => a.ToString()).ToArray());
        }
    }
}
