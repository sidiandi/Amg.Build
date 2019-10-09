using Castle.DynamicProxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace Amg.Build
{
    /// <summary>
    /// Creates proxies for classes that execute methods marked with [Once] only once.
    /// </summary>

    internal interface IInvocationSource
    {
        IEnumerable<InvocationInfo> Invocations { get; }
    }

    public class Once
    {
        /// <summary>
        /// Default instance
        /// </summary>
        public static Once Instance { get; } = new Once();

        public Once()
        {
            waitUntilCancelled = Task.Delay(-1, cancelAll.Token);
        }

        internal static bool HasOnceMethods(object? x)
        {
            if (x == null)
            {
                return false;
            }
            else
            {
                var type = x.GetType();
                return type.GetMethods(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic)
                    .Any(_ => Has(_));
            }
        }

        internal static PropertyInfo? GetPropertyInfo(MethodInfo method)
        {
            if (!method.IsSpecialName) return null;
            return method.DeclaringType.GetProperty(method.Name.Substring(4),
              BindingFlags.Instance | 
              BindingFlags.Static | 
              BindingFlags.NonPublic |
              BindingFlags.Public);
        }

        internal static bool Has(MemberInfo member)
        {
            var r = member.GetCustomAttribute<OnceAttribute>() != null;
            if (r)
            {
                return true;
            }
            else
            {
                if (member is MethodInfo method)
                {
                    var property = GetPropertyInfo(method);
                    return property == null
                        ? r
                        : Has(property);
                }
                else
                {
                    return r;
                }
            }
        }

        static ProxyGenerator generator = new ProxyGenerator(new DefaultProxyBuilder());

        class InvocationSource : IInvocationSource
        {
            public InvocationSource(IEnumerable<InvocationInfo> invocations)
            {
                Invocations = invocations;
            }

            public IEnumerable<InvocationInfo> Invocations { get; private set; }
        }

        /// <summary>
        /// Get an instance of type that executes methods marked with [Once] only once and caches the result.
        /// </summary>
        /// <returns></returns>
        public static object Create(Type type, params object?[] ctorArguments)
        {
            return Instance.Get(type, ctorArguments);
        }

        /// <summary>
        /// Get an instance of T that executes methods marked with [Once] only once and caches the result.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Create<T>(params object?[] ctorArguments) where T : class
        {
            return (T)Create(typeof(T), ctorArguments);
        }

        readonly IDictionary<string, object> _cache = new Dictionary<string, object>();

        static string GenerateCacheKey(Type type, object?[] arguments)
        {
            return $"{type.FullName}({arguments.Join(", ")})";
        }

        /// <summary>
        /// Get an instance of type that executes methods marked with [Once] only once and caches the result.
        /// </summary>
        /// <returns></returns>
        public T Get<T>(params object?[] ctorArguments) => (T) Get(typeof(T), ctorArguments);

        /// <summary>
        /// Get an instance of type that executes methods marked with [Once] only once and caches the result.
        /// </summary>
        /// <returns></returns>
        object Get(Type type, params object?[] ctorArguments)
        {
            var interceptor = new OnceInterceptor(waitUntilCancelled);

            var options = new ProxyGenerationOptions
            {
                Hook = new OnceHook(),
            };
            options.AddMixinInstance(new InvocationSource(interceptor.Invocations));

            return generator.CreateClassProxy(
                type,
                options,
                ctorArguments,
                interceptor);
        }

        public void CancelAll()
        {
            cancelAll.Cancel();
        }

        readonly CancellationTokenSource cancelAll = new CancellationTokenSource();
        readonly Task waitUntilCancelled;
    }
}
