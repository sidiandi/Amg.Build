using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Amg.Build
{
    internal static class CommandObject
    {
        internal static bool IsPublicCommand(MethodInfo method)
        {
            return method.GetCustomAttribute<DescriptionAttribute>() != null;
        }

        public static IEnumerable<MethodInfo> Commands(Type type)
        {
            return type.GetMethods()
                .Where(IsPublicCommand)
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
            var commands = Commands(commandObject.GetType());
            var defaultTarget = new[]
            {
                commands.FirstOrDefault(_ => _.GetCustomAttribute<DefaultAttribute>() != null),
                commands.FindByNameOrDefault(_ => _.Name, "All"),
                commands.FindByNameOrDefault(_ => _.Name, "Default"),
            }.FirstOrDefault(_ => _ != null);

            return defaultTarget;
        }
    }
}