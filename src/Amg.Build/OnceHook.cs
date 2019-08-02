using System;
using System.Reflection;
using Castle.DynamicProxy;

namespace Amg.Build
{
    internal class OnceHook : IProxyGenerationHook
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public void MethodsInspected()
        {
        }

        public static bool HasOnce(MemberInfo memberInfo)
        {
            return memberInfo.GetCustomAttribute<OnceAttribute>() != null;
        }

        public void NonProxyableMemberNotification(Type type, MemberInfo memberInfo)
        {
            if (HasOnce(memberInfo))
            {
                throw new Exception($"{memberInfo} must be virtual because it has the [Once] attribute.");
            }
        }

        public bool ShouldInterceptMethod(Type type, MethodInfo methodInfo)
        {
            return HasOnce(methodInfo);
        }
    }
}
