using System;
using System.Linq;
using System.Reflection;
using Castle.DynamicProxy;

namespace Amg.Build
{
    internal class OnceHook : IProxyGenerationHook
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

        public void MethodsInspected()
        {
            if (type != null)
            {
                AssertNoMutableFields(type);
            }
        }

        Type? type;

        static void AssertNoMutableFields(Type type)
        {
            var fields = type.GetFields(
                BindingFlags.Instance | BindingFlags.Static |
                BindingFlags.Public | BindingFlags.NonPublic)
                .Where(f => !f.IsInitOnly);

            if (fields.Any())
            {
                throw new InvalidOperationException("all fields must be readonly");
            }
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
            this.type = type;
            var intercept = Once.Has(methodInfo);
            return intercept;
        }
    }
}
