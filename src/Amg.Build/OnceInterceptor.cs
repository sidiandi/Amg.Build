using System.Reflection;
using System.Text.RegularExpressions;
using Amg.Extensions;
using Castle.DynamicProxy;

namespace Amg.Build
{
    internal class OnceInterceptor : IInterceptor
    {
        public OnceInterceptor(Task waitUntilCancelled)
        {
            _waitUntilCancelled = waitUntilCancelled;
            instanceId = string.Empty;
            _cache = new Dictionary<InvocationId, IInvocation>();
        }

        public OnceInterceptor(Task waitUntilCancelled, OnceInterceptor parent, string instanceId)
        : this(waitUntilCancelled)
        {
            this.instanceId = instanceId;
            _cache = parent._cache;
        }

        public Task _waitUntilCancelled { get; }

        readonly IDictionary<InvocationId, IInvocation> _cache;

        public IEnumerable<IInvocation> Invocations => _cache.Values;

        static bool IsSetter(MethodInfo method)
        {
            return method.Name.StartsWith("set_");
        }

        static MethodInfo GetterFromSetter(MethodInfo method)
        {
            var getterName = System.Text.RegularExpressions.Regex.Replace(method.Name, "^set_", "get_");
            return method.DeclaringType.GetMethod(getterName);
        }

        public void Intercept(Castle.DynamicProxy.IInvocation invocation)
        {
            var cacheKey = GenerateCacheKey(invocation.Method, invocation.Arguments);

            if (IsSetter(invocation.Method))
            {
                var getterId = new InvocationId(
                    cacheKey.InstanceId,
                    Regex.Replace(cacheKey.Method, @"\.set_", ".get_"),
                    new object[] { });

                if (_cache.ContainsKey(getterId))
                {
                    throw new OncePropertyCanOnlyBeSetBeforeFirstGetException(invocation.Method);
                }

                invocation.ReturnValue = CreateInvocation(this, cacheKey, invocation).ReturnValue;
            }
            else
            {
                invocation.ReturnValue = _cache.GetOrAdd(cacheKey, () => CreateInvocation(this, cacheKey, invocation))
                    .ReturnValue;
            }
        }

        static IInvocation CreateInvocation(
            OnceInterceptor interceptor,
            InvocationId id,
            Castle.DynamicProxy.IInvocation invocation)
        {
            if (invocation.Method.GetCustomAttribute<CachedAttribute>() is { })
            {
                return new CachedInvocationInfo(interceptor, id, invocation);
            }
            else
            {
                return new InvocationInfo(interceptor, id, invocation);
            }
        }

        readonly string instanceId;

        InvocationId GenerateCacheKey(MethodInfo method, object[] arguments)
        {
            var id = new InvocationId
            (
                instanceId: instanceId,
                method: $"{method.DeclaringType.Name}.{method.Name}",
                arguments: arguments
            );

            return id;
        }
    }
}