using Amg.Extensions;
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

        public static IEnumerable<MethodInfo> Commands(object commandObject)
        {
            var type = commandObject.GetType();
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

        public static MethodInfo? GetDefaultCommand(object commandObject)
        {
            var commands = Commands(commandObject);
            var defaultTarget = new[]
            {
                commands.FirstOrDefault(_ => _.GetCustomAttribute<DefaultAttribute>() != null),
                commands.FindByNameOrDefault(_ => _.Name, "All"),
                commands.FindByNameOrDefault(_ => _.Name, "Default"),
            }.FirstOrDefault(_ => _ != null);

            return defaultTarget;
        }

        public static bool HasDefaultCommand(object commandObject)
        {
            return GetDefaultCommand(commandObject) != null;
        }
    }
}