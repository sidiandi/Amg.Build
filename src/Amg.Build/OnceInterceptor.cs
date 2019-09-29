using System.Collections.Generic;
using System.Linq;
using Castle.DynamicProxy;

namespace Amg.Build
{
    internal class OnceInterceptor : IInterceptor
    {
        public OnceInterceptor()
        {
            prefix = null;
            _cache = new Dictionary<string, InvocationInfo>();
        }

        public OnceInterceptor(OnceInterceptor parent, string prefix)
        {
            this.prefix = prefix;
            _cache = parent._cache;
        }

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
