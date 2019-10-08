using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Amg.Build
{
    internal static class CommandObject
    {
        internal static bool IsTarget(MethodInfo method)
        {
            return Once.Has(method);
        }

        internal static bool IsPublicTarget(MethodInfo method)
        {
            return IsTarget(method) &&
                method.GetCustomAttribute<DescriptionAttribute>() != null;
        }

        public static IEnumerable<MethodInfo> Commands(Type type)
        {
            return type.GetMethods()
                .Where(IsPublicTarget)
                .ToList();
        }

        public static IEnumerable<MethodInfo> Targets(Type type)
        {
            return type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Where(IsTarget)
                .ToList();
        }

        public static string Description(MethodInfo method)
        {
            var a = method.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
            return a == null
                ? String.Empty
                : a.Description;
        }

        public static MethodInfo? GetDefaultTarget(object commandObject)
        {
            var t = Targets(commandObject.GetType());
            var defaultTarget = new[]
            {
                t.FirstOrDefault(_ => _.GetCustomAttribute<DefaultAttribute>() != null),
                t.FindByNameOrDefault(_ => _.Name, "All"),
                t.FindByNameOrDefault(_ => _.Name, "Default"),
            }.FirstOrDefault(_ => _ != null);

            return defaultTarget;
        }
    }
}