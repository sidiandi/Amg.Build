using System;
using System.Linq;
using System.Reflection;

namespace Amg.Build
{
    /// <summary>
    /// Mark method to be executed only once during the lifetime of its class instance.
    /// </summary>
    /// Can only be applied to virtual methods.
    public class OnceAttribute : Attribute
    {
    }

    internal static class Once
    {
        public static bool HasOnceMethods(object x)
        {
            var type = x.GetType();
            return type.GetMethods(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic)
                .Any(_ => Has(_));
        }

        public static PropertyInfo GetPropertyInfo(MethodInfo method)
        {
            if (!method.IsSpecialName) return null;
            return method.DeclaringType.GetProperty(method.Name.Substring(4),
              BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
        }

        public static bool Has(MemberInfo member)
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
    }
}