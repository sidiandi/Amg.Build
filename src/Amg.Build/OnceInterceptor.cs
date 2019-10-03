using System.Collections.Generic;
using System.Threading.Tasks;
using Castle.DynamicProxy;

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

        public void Intercept(IInvocation invocation)
        {
            var cacheKey = GenerateCacheKey(invocation.Method.Name, invocation.Arguments);
            invocation.ReturnValue = _cache.GetOrAdd(cacheKey, () => new InvocationInfo(this, cacheKey, invocation))
                .ReturnValue;
        }

        readonly string? prefix;

        string GenerateCacheKey(string name, object[] arguments)
        {
            string Key()
            {
                if (arguments == null || arguments.Length == 0)
                    return name;
                return $"{name}({arguments.Join(",")})";
            }
            return prefix == null
                ? Key()
                : prefix + "." + Key();
        }
    }
}
