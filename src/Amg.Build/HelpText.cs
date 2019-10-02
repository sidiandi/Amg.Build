using Amg.CommandLine;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Amg.Build
{
    internal static class HelpText
    {
        private static void PrintOptionsList(TextWriter @out, Options options)
        {
            GetOptParser.GetOptions(options)
                .Where(_ => !_.IsOperands)
                .Where(IncludeInHelp)
                .Select(_ => new { indent, _.Syntax, _.Description })
                .ToTable(header: false)
                .Write(@out);
        }

        static bool IncludeInHelp(GetOptOption option)
        {
            return !option.Long.Equals("ignore-clean");
        }
            
        internal static bool IsTarget(MethodInfo method)
        {
            return Once.Has(method);
        }

        internal static bool IsPublicTarget(MethodInfo method)
        {
            return IsTarget(method) &&
                method.GetCustomAttribute<DescriptionAttribute>() != null;
        }

        public static IEnumerable<MethodInfo> PublicTargets(Type type)
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

        private static string Description(MethodInfo method)
        {
            var a = method.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
            return a == null
                ? String.Empty
                : a.Description;
        }

        private static string Syntax(MethodInfo method)
        {
            return new[] { GetOptParser.GetLongOptionNameForMember(method.Name), }
            .Concat(method.GetParameters().Select(_ => $"<{_.Name}>"))
            .Join(" ");
        }

        const string indent = " ";

        private static void PrintTargetsList(TextWriter @out, object targets)
        {
            var publicTargets = PublicTargets(targets.GetType());
            publicTargets
                .Select(_ => new { indent, Syntax = Syntax(_), Description = Description(_) })
                .ToTable(header: false)
                .Write(@out);
        }

        public static void Print(TextWriter @out, Options options)
        {
            var name = Assembly.GetEntryAssembly().GetName().Name;
            @out.WriteLine($@"Usage: {name} [options] <command> [command parameters]...
");
            var targets = PublicTargets(options.Targets.GetType());
            if (targets.Any())
            {
                @out.WriteLine(@"Commands:");
                PrintTargetsList(@out, options.Targets);
                @out.WriteLine();
            }
            @out.WriteLine(@"Options:");
            PrintOptionsList(@out, options);
            @out.WriteLine();
            @out.WriteLine(@"Exit codes:");
            PrintExitCodeList(@out, typeof(RunContext.ExitCode));
        }

        private static void PrintExitCodeList(TextWriter @out, Type enumType)
        {
            @out.WriteLine(
                Enum.GetValues(enumType).Cast<object>()
                .Select(e => new
                {
                    indent = " ",
                    value = (int)e,
                    description = Enum.GetName(enumType, e)
                })
                .ToTable());
        }
    }
}