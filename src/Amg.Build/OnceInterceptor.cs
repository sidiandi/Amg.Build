using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Amg.Extensions;

namespace Amg.Build
{
    internal class OnceInterceptor : IInterceptor
    {
        public OnceInterceptor(Task waitUntilCancelled)
        {
            _waitUntilCancelled = waitUntilCancelled;
            prefix = null;
            _cache = new Dictionary<string, InvocationInfo>();
        }

        public OnceInterceptor(Task waitUntilCancelled, OnceInterceptor parent, string prefix)
        : this(waitUntilCancelled)
        {
            this.prefix = prefix;
            _cache = parent._cache;
        }

        public Task _waitUntilCancelled { get; }

        readonly IDictionary<string, InvocationInfo> _cache;

        public IEnumerable<InvocationInfo> Invocations => _cache.Values;

        static bool IsSetter(MethodInfo method)
        {
            return method.Name.StartsWith("set_");
        }

        public void Intercept(IInvocation invocation)
        {
            var cacheKey = GenerateCacheKey(invocation.Method, invocation.Arguments);

            if (IsSetter(invocation.Method) && (_cache.ContainsKey(cacheKey)))
            {
                throw new OncePropertyCanOnlyBeCalledOnceException(invocation.Method);
            }

            invocation.ReturnValue = _cache.GetOrAdd(cacheKey, () => new InvocationInfo(this, cacheKey, invocation))
                .ReturnValue;
        }

        readonly string? prefix;

        string GenerateCacheKey(MethodInfo method, object[] arguments)
        {
            string Key()
            {
                var id = $"{method.DeclaringType.Name}.{method.Name}";
                if (!IsSetter(method) && (arguments.Length > 0))
                {
                    id = $"{id}({arguments.Join(",")})";
                }
                return id;
            }

            return prefix == null
                ? Key()
                : prefix + "." + Key();
        }
    }
}
