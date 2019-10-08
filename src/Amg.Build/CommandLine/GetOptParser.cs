using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Amg.Build;

namespace Amg.CommandLine
{
    /// <summary>
    /// GetOpt compatible command line option parser
    /// </summary>
    /// Follows the conventions in [Program Argument Syntax Conventions](https://www.gnu.org/software/libc/manual/html_node/Argument-Syntax.html):
    internal static class GetOptParser
    {
        internal const string LongPrefix = "--";
        internal const string ShortPrefix = "-";

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

                throw new InvalidOperationException($"Option {option} is required.");
            }

            return v;
        }

        internal static string Usage(Type type)
        {
            return GetLongOptionNameForMember(type.Name);
        }

        internal static string ValueSyntax(Type type)
        {
            return type.IsEnum
                ? $"{GetLongOptionNameForMember(type.Name)}={string.Join("|", Enum.GetNames(type).Select(GetLongOptionNameForMember))}"
                : String.Empty;
        }

        internal static TOptions Parse<TOptions>(
            string[] args,
            bool ignoreUnknownOptions = false
            )
        {
            var rest = new ArraySegment<string>(args);
            return Parse<TOptions>(ref rest, ignoreUnknownOptions: ignoreUnknownOptions);
        }

        internal static TOptions Parse<TOptions>(
            ref ArraySegment<string> args,
            bool ignoreUnknownOptions = false
            )
        {
            var options = Activator.CreateInstance(typeof(TOptions));
            Parse(ref args, options, ignoreUnknownOptions: ignoreUnknownOptions);
            return (TOptions)options;
        }

        /// <summary>
        /// Parse args into options
        /// </summary>
        /// <param name="args"></param>
        /// <param name="options"></param>
        public static void Parse(
            string[] args,
            object options,
            bool ignoreUnknownOptions = false)
        {
            var r = new ArraySegment<string>(args);
            Parse(ref r, options, ignoreUnknownOptions: ignoreUnknownOptions);
        }
        
        /// <summary>
            /// Parse args into options
            /// </summary>
            /// <param name="args"></param>
            /// <param name="options"></param>
            public static void Parse(
            ref ArraySegment<string> args, 
            object options,
            bool ignoreUnknownOptions = false)
        {
            var context = new GetOptContext(options);
            var rest = args;
            while (rest.Count > 0)
            {
                try
                {
                    ReadOption(ref rest, context);
                }
                catch (Exception ex)
                {
                    if (ignoreUnknownOptions)
                    {
                        GetFirst(ref rest);
                    }
                    else
                    {
                        throw new CommandLineArgumentException(rest, ex);
                    }
                }
            }
        }

        private static void ReadOption(ref ArraySegment<string> args, GetOptContext getOptContext)
        {
            if (ReadOptionsStop(ref args, getOptContext))
            {
                return;
            }

            if (ReadLongOption(ref args, getOptContext))
            {
                return;
            }

            if (ReadShortOptions(ref args, getOptContext))
            {
                return;
            }

            if (ReadArgument(ref args, getOptContext))
            {
                return;
            }

            throw new CommandLineArgumentException(args, "Cannot read option");
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

        private static GetOptOption FindLongOption(object x, string optionName)
        {
            var options = GetOptions(x).ToList();
            return options.FindByName(_ => _.Long, optionName, "options");
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

        internal static string GetFirst(ref ArraySegment<string> a)
        {
            var f = a[0];
            a = a.Slice(1);
            return f;
        }

        private static bool ReadArgument(ref ArraySegment<string> args, GetOptContext getOptContext)
        {
            var rest = args;
            FindOperands(getOptContext.Options).Set(GetFirst(ref rest));
            args = rest;
            return true;
        }

        private static bool ReadShortOptions(ref ArraySegment<string> args, GetOptContext getOptContext)
        {
            if (getOptContext.OptionsStop)
            {
                return false;
            }

            var r = args;
            var current = GetFirst(ref r);
            if (!current.StartsWith(ShortPrefix))
            {
                return false;
            }

            var option = current.Substring(ShortPrefix.Length);

            while (!string.IsNullOrEmpty(option))
            {
                var optionName = option[0];
                var optionValue = option.Substring(1);

                var o = FindShortOption(getOptContext.Options, optionName);
                if (o == null)
                {
                    throw new ParserException($"{ShortPrefix}{optionName} is not an option.");
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
                        optionValue = GetFirst(ref r);
                    }

                    o.Set(optionValue);
                }
            }

            args = r;
            return true;
        }

        private static bool ReadLongOption(ref ArraySegment<string> args, GetOptContext getOptContext)
        {
            if (getOptContext.OptionsStop)
            {
                return false;
            }

            var r = args;
            var c = GetFirst(ref r);

            if (!c.StartsWith(LongPrefix))
            {
                return false;
            }

            var parts = c.Substring(LongPrefix.Length).Split(new[] { '=' }, 2);
            var optionName = parts[0];

            var o = FindLongOption(getOptContext.Options, optionName);
            if (o == null)
            {
                throw new InvalidOperationException($"{LongPrefix}{optionName} is not an option.");
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
                    optionValue = GetFirst(ref r);
                }

                o.Set(optionValue);
            }

            args = r;
            return true;
        }

        private static bool ReadOptionsStop(ref ArraySegment<string> args, GetOptContext getOptContext)
        {
            if (getOptContext.OptionsStop)
            {
                return false;
            }

            var r = args;
            var c = GetFirst(ref r);
            if (c.Equals(LongPrefix))
            {
                getOptContext.OptionsStop = true;
                args = r;
                return true;
            }

            return false;
        }

        internal static string GetDescription(MemberInfo m)
        {
            var d = m.GetCustomAttribute<DescriptionAttribute>();
            return d == null ? string.Empty : d.Description;
        }

        /// <summary>
        /// Invokes method of instance using arguments as parameters
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="method"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public static object Invoke(object instance, MethodInfo method, string[] arguments)
        {
            var parameters = ParseParameters(method, arguments);
            return method.Invoke(instance, parameters);
        }

        internal static object[] ParseParameters(MethodInfo method, string[] arguments)
        {
            return method.GetParameters()
                .ZipOrDefault(arguments, (p, v) => GetOptOption.Parse(p.ParameterType, v))
                .ToArray();
        }
    }
}
