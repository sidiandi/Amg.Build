using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Csa.Build;
using Serilog;

namespace Csa.CommandLine
{
    /// <summary>
    /// GetOpt compatible command line option parser
    /// </summary>
    /// Follows the conventions in [Program Argument Syntax Conventions](https://www.gnu.org/software/libc/manual/html_node/Argument-Syntax.html):
    public static partial class GetOptParser
    {
        private const string LongPrefix = "--";
        private const string ShortPrefix = "-";

        internal static void PrintOptions(object options)
        {
            PrintOptions(Console.Out, options);
        }

        internal static void PrintOptions(TextWriter @out, object options)
        {
            if (options == null)
            {
                return;
            }

            var opts = GetOptions(options).ToList();

            foreach (var o in opts.Where(_ => _.IsOperands))
            {
                @out.WriteLine($@"[{o.Long}]: <{Usage(o.Type)}>...
  {o.Description}
");
            }

            @out.WriteLine(@"Options:");
            foreach (var o in opts.Where(_ => !_.IsOperands))
            {
                @out.WriteLine($@"{o.Syntax} {o.Description}");
            }
        }

        internal static void PrintOptionsList(TextWriter @out, object options)
        {
            if (options == null)
            {
                return;
            }

            var opts = GetOptions(options).ToList();

            foreach (var o in opts.Where(_ => !_.IsOperands))
            {
                @out.WriteLine($@"{o.Syntax} {o.Description}");
            }
        }

        public static IEnumerable<GetOptOption> GetOptions(object options)
        {
            if (options != null)
            {
                foreach (var p in options.GetType().GetProperties())
                {
                    if (IsOption(p))
                    {
                        var o = new GetOptOption(options, p);
                        yield return o;
                    }
                    else if (IsOptionsContainer(p))
                    {
                        var container = p.GetValue(options);
                        var optionsOfContainer = GetOptions(container).ToList();
                        foreach (var i in optionsOfContainer)
                        {
                            yield return i;
                        }
                    }
                }
            }
        }

        static bool IsOptionsContainer(PropertyInfo p)
        {
            return HasPublicGetter(p)
                && !IsOptionType(p.PropertyType)
                && !IsDelegate(p.PropertyType);
        }

        static bool IsDelegate(Type t)
        {
            return typeof(MulticastDelegate).IsAssignableFrom(t);
        }

        /// <summary>
        /// Throws if the property value obtained by getOption is null.
        /// </summary>
        /// Use this in your code to assert that an option was set by the user. If not, an exception saying
        /// 
        /// GetOptOption --{optionName} is required.
        /// 
        /// will be thrown.
        /// 
        /// Example: 
        /// 
        ///   GetOpt.Require(() => options.SomeString);
        /// 
        /// will throw 
        /// 
        ///   new Exception("GetOptOption --some-string is required.")
        /// 
        /// if option.SomeString == null.
        /// <typeparam name="T"></typeparam>
        /// <param name="getOption"></param>
        /// <returns></returns>
        public static T Require<T>(Expression<Func<T>> getOption)
        {
            var v = getOption.Compile()();
            if (v == null || v.Equals("") || v.ToString().Equals(""))
            {
                string option;
                try
                {
                    option = LongPrefix + GetLongOptionNameForMember(((MemberExpression)getOption.Body).Member.Name);
                }
                catch
                {
                    option = getOption.ToString();
                }

                throw new Exception($"Option {option} is required.");
            }

            return v;
        }

        private static string Usage(Type type)
        {
            return type.IsEnum
                ? string.Join("|", Enum.GetNames(type))
                : type.Name.ToLower();
        }

        internal static TOptions Parse<TOptions>(IEnumerable<string> args)
        {
            var options = Activator.CreateInstance(typeof(TOptions));
            Parse(args, options);
            return (TOptions)options;
        }

        /// <summary>
        /// Parse args into options
        /// </summary>
        /// <param name="args"></param>
        /// <param name="options"></param>
        public static void Parse(IEnumerable<string> args, object options)
        {
            var context = new GetOptContext(options);
            using (var e = args.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    try
                    {
                        ReadOption(context, e);
                    }
                    catch (Exception ex)
                    {
                        throw new ParseException(args, e, ex);
                    }
                }
            }
        }

        private static void ReadOption(GetOptContext getOptContext, IEnumerator<string> args)
        {
            if (ReadOptionsStop(getOptContext, args))
            {
                return;
            }

            if (ReadLongOption(getOptContext, args))
            {
                return;
            }

            if (ReadShortOptions(getOptContext, args))
            {
                return;
            }

            if (ReadArgument(getOptContext, args))
            {
                return;
            }
        }

        internal static string GetLongOptionNameForMember(string memberName)
        {
            return new string(memberName.Take(1)
                .Select(char.ToLower)
                .Concat(memberName.Skip(1)
                    .SelectMany(_ => char.IsUpper(_)
                        ? new[]
                        {
                            '-',
                            char.ToLower(_)
                        }
                        : new[]
                        {
                            _
                        }))
                .ToArray());
        }

        private static bool IsOptionType(Type type)
        {
            if (type.IsArray)
            {
                return true;
            }

            return
                type.IsEnum ||
                type == typeof(string) ||
                type == typeof(double) ||
                type == typeof(int) ||
                type == typeof(bool);
        }

        static bool IsOption(PropertyInfo p)
        {
            return
                IsOptionType(p.PropertyType)
                && HasPublicSetter(p)
                && !String.IsNullOrEmpty(GetDescription(p));
        }

        static bool HasPublicSetter(PropertyInfo p)
        {
            return (p.SetMethod != null && p.SetMethod.IsPublic);
        }

        static bool HasPublicGetter(PropertyInfo p)
        {
            return (p.GetMethod != null && p.GetMethod.IsPublic);
        }


        internal static T FindByName<T>(IEnumerable<T> candidates, Func<T, string> name, string query)
        {
            var r = candidates.SingleOrDefault(option =>
                name(option).Equals(query, StringComparison.InvariantCultureIgnoreCase));

            if (r != null)
            {
                return r;
            }

            var matches = candidates.Where(option =>
                    name(option).StartsWith(query, StringComparison.InvariantCultureIgnoreCase))
                .ToArray();

            if (matches.Length > 1)
            {
                throw new Exception($@"{query.Quote()} is ambiguous. Could be

{matches.Select(name).Join()}

");
            }

            if (matches.Length == 1)
            {
                return matches[0];
            }

            throw new Exception($@"{query.Quote()} not found in

{candidates.Select(name).Join()}

");
        }

        private static GetOptOption FindLongOption(object x, string optionName)
        {
            var options = GetOptions(x).ToList();
            return FindByName(options, _ => _.Long, optionName);
        }

        private static GetOptOption FindOperands(object x)
        {
            return GetOptions(x).Single(_ => _.IsOperands);
        }

        private static GetOptOption FindShortOption(object x, char optionName)
        {
            return GetOptions(x).FirstOrDefault(o =>
                o.Short != null && o.Short.Value.Equals(optionName));
        }

        private static bool ReadArgument(GetOptContext getOptContext, IEnumerator<string> args)
        {
            FindOperands(getOptContext.Options).Set(args.Current);
            return true;
        }

        private static bool ReadShortOptions(GetOptContext getOptContext, IEnumerator<string> args)
        {
            if (getOptContext.OptionsStop)
            {
                return false;
            }

            if (!args.Current.StartsWith(ShortPrefix))
            {
                return false;
            }

            var option = args.Current.Substring(ShortPrefix.Length);

            while (!string.IsNullOrEmpty(option))
            {
                var optionName = option[0];
                var optionValue = option.Substring(1);

                var o = FindShortOption(getOptContext.Options, optionName);
                if (o == null)
                {
                    throw new Exception($"{ShortPrefix}{optionName} is not an option.");
                }

                if (o.IsFlag)
                {
                    option = optionValue;
                    o.SetFlag();
                }
                else
                {
                    option = string.Empty;
                    if (string.IsNullOrEmpty(optionValue))
                    {
                        args.MoveNext();
                        optionValue = args.Current;
                    }

                    o.Set(optionValue);
                }
            }

            return true;
        }

        private static bool ReadLongOption(GetOptContext getOptContext, IEnumerator<string> args)
        {
            if (getOptContext.OptionsStop)
            {
                return false;
            }

            if (!args.Current.StartsWith(LongPrefix))
            {
                return false;
            }

            var parts = args.Current.Substring(LongPrefix.Length).Split(new[] { '=' }, 2);
            var optionName = parts[0];

            var o = FindLongOption(getOptContext.Options, optionName);
            if (o == null)
            {
                throw new Exception($"{LongPrefix}{optionName} is not an option.");
            }

            if (o.IsFlag)
            {
                o.SetFlag();
            }
            else
            {
                string optionValue;
                if (parts.Length >= 2)
                {
                    optionValue = parts[1];
                }
                else
                {
                    args.MoveNext();
                    optionValue = args.Current;
                }

                o.Set(optionValue);
            }

            return true;
        }

        private static bool ReadOptionsStop(GetOptContext getOptContext, IEnumerator<string> args)
        {
            if (getOptContext.OptionsStop)
            {
                return false;
            }

            if (args.Current.Equals(LongPrefix))
            {
                getOptContext.OptionsStop = true;
                return true;
            }

            return false;
        }

        internal static string GetDescription(MemberInfo m)
        {
            var d = m.GetCustomAttribute<DescriptionAttribute>();
            return d == null ? string.Empty : d.Description;
        }
    }
}
