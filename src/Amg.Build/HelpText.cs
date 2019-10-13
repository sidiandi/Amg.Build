using Amg.Build.Extensions;
using Amg.CommandLine;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using static Amg.Build.CommandObject;

namespace Amg.Build
{

    internal static class HelpText
    {
        private static void PrintOptionsList(TextWriter @out, object options)
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
        private static string Syntax(MethodInfo method)
        {
            return new[] { GetOptParser.GetLongOptionNameForMember(method.Name), }
            .Concat(method.GetParameters().Select(ParameterSyntax))
            .Join(" ");
        }

        static string ParameterSyntax(ParameterInfo p)
        {
            if (p.ParameterType.IsArray)
            { 
                return $"[{p.Name}]...";
            }

            if (p.HasDefaultValue)
            {
                return $"[{p.Name}]";
            }

            return $"<{p.Name}>";
        }

        const string indent = " ";

        private static void PrintTargetsList(TextWriter @out, object targets)
        {
            var publicTargets = Commands(targets.GetType());
            publicTargets
                .Select(_ => new { indent, Syntax = Syntax(_), Description = Description(_) })
                .ToTable(header: false)
                .Write(@out);
        }

        public static void Print(TextWriter @out, CombinedOptions options)
        {
            var name = Assembly.GetEntryAssembly().GetName().Name;
            @out.WriteLine($@"Usage: {name} [options] <command> [command parameters]...
");
            var targets = Commands(options.OnceProxy.GetType());
            if (targets.Any())
            {
                @out.WriteLine(@"Commands:");
                PrintTargetsList(@out, options.OnceProxy);
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