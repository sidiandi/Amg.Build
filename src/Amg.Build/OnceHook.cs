using System;
using System.Reflection;
using Castle.DynamicProxy;

namespace Amg.Build
{
    internal class OnceHook : IProxyGenerationHook
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

        public void MethodsInspected()
        {
            // nothing needs to be don at the end of the inspection
        }

        public void NonProxyableMemberNotification(Type type, MemberInfo memberInfo)
        {
            if (Once.Has(memberInfo))
            {
                throw new InvalidOperationException($"{memberInfo} must be virtual because it has the [Once] attribute.");
            }
        }

        public bool ShouldInterceptMethod(Type type, MethodInfo methodInfo)
        {
            var intercept = Once.Has(methodInfo);
            return intercept;
        }
    }
}
