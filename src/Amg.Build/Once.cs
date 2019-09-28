using Castle.DynamicProxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Amg.Build
{
    /// <summary>
    /// Creates proxies for classes that execute methods marked with [Once] only once.
    /// </summary>
    public class Once : IServiceProvider
    {
        /// <summary>
        /// Default instance
        /// </summary>
        public static Once Instance { get; } = new Once();

        internal static bool HasOnceMethods(object x)
        {
            if (x == null) return false;

            var type = x.GetType();
            return type.GetMethods(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic)
                .Any(_ => Has(_));
        }

        internal static PropertyInfo GetPropertyInfo(MethodInfo method)
        {
            if (!method.IsSpecialName) return null;
            return method.DeclaringType.GetProperty(method.Name.Substring(4),
              BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
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

        /// <summary>
        /// Get an instance of T that executes methods marked with [Once] only once and caches the result.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Get<T>() where T: class
        {
            return Add<T>();
        }

        static ProxyGenerator generator = new ProxyGenerator(new DefaultProxyBuilder());

        /// <summary>
        /// Get an instance of T that executes methods marked with [Once] only once and caches the result.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Add<T>(params object[] ctorArguments) where T : class
        {
            return (T)onceInstanceCache.GetOrAdd(typeof(T), () => Create<T>(ctorArguments));
        }

        /// <summary>
        /// Get an instance of T that executes methods marked with [Once] only once and caches the result.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Create<T>(params object[] ctorArguments) where T : class
        {
            var onceInterceptor = new OnceInterceptor();
            var onceProxy = generator.CreateClassProxy(
                typeof(T),
                new ProxyGenerationOptions
                {
                    Hook = new OnceHook()
                },
                ctorArguments,
                onceInterceptor);
            var proxy = (T)onceProxy;
            return proxy;
        }

        /// <summary />
        public object GetService(Type serviceType)
        {
            return GetType().GetMethod(nameof(Get))
                .MakeGenericMethod(serviceType)
                .Invoke(this, new object[] { });
        }

        Dictionary<Type, object> onceInstanceCache = new Dictionary<Type, object>();
    }
}
