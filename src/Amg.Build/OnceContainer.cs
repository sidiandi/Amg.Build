﻿using Amg.Build.Extensions;
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
    internal class OnceContainer
    {
        /// <summary>
        /// Default instance
        /// </summary>
        public static OnceContainer Instance { get; } = new OnceContainer();

        public OnceContainer()
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
                    .Any(_ => HasOnceAttribute(_));
            }
        }

        /// <summary>
        /// Get the property info for a getter or setter method.
        /// </summary>
        /// <param name="getterOrSetterMethod"></param>
        /// <returns>property info, or null if property info for the passed method does not exist.</returns>
        internal static PropertyInfo? GetPropertyInfo(MethodInfo getterOrSetterMethod)
        {
            if (!getterOrSetterMethod.IsSpecialName) return null;
            return getterOrSetterMethod.DeclaringType.GetProperty(getterOrSetterMethod.Name.Substring(4),
              BindingFlags.Instance | 
              BindingFlags.Static | 
              BindingFlags.NonPublic |
              BindingFlags.Public);
        }

        public static bool HasOnceAttribute(MemberInfo member)
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
                        : HasOnceAttribute(property);
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
        public object Get(Type type, params object?[] ctorArguments)
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
