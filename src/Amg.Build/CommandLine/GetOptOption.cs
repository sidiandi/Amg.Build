using Amg.Build;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Amg.CommandLine
{
    internal static partial class GetOptParser
    {
        public class GetOptOption
        {
            private readonly object _object;

            private readonly PropertyInfo _property;

            public GetOptOption(object x, PropertyInfo p)
            {
                _property = p;
                _object = x;
            }

            public Type Type => _property.PropertyType.IsArray
                ? _property.PropertyType.GetElementType()
                : _property.PropertyType;

            public Action<string> Set => v => SetStringValue(_property, _object, v);

            public bool IsFlag => Type == typeof(bool) || Type == typeof(bool[]);

            public char? Short
            {
                get
                {
                    var s = _property.GetCustomAttribute<ShortAttribute>();

                    return s?.Name;
                }
            }

            public string Long => GetOptParser.GetLongOptionNameForMember(_property.Name);

            public string Description
            {
                get
                {
                    return new[]
                    {
                        _property.GetCustomAttribute<DescriptionAttribute>().Map(_ => _.Description),
                        ValueSyntax(this.Type)
                    }.Join(" ");
                }
            }

            private static string ShortOptionUsage(GetOptOption o)
            {
                if (o.Short == null)
                {
                    return string.Empty;
                }

                return o.IsFlag
                    ? $"{ShortPrefix}{o.Short} | "
                    : $"{ShortPrefix}{o.Short}<{Usage(o.Type)}> | ";
            }

            private static string LongOptionUsage(GetOptOption o)
            {
                return o.IsFlag
                    ? $"{LongPrefix}{o.Long}"
                    : $"{LongPrefix}{o.Long}=<{Usage(o.Type)}>";
            }

            public string Syntax => $@"{ShortOptionUsage(this)}{LongOptionUsage(this)}";

            public bool IsOperands => this._property.GetCustomAttribute<OperandsAttribute>() != null;

            public void SetFlag()
            {
                Set(true.ToString());
            }

            public static object Parse(Type toType, string stringValue)
            {
                try
                {
                    object value = null;
                    if (toType == typeof(bool))
                    {
                        value = bool.Parse(stringValue);
                    }
                    else if (toType == typeof(int))
                    {
                        value = int.Parse(stringValue);
                    }
                    else if (toType == typeof(double))
                    {
                        value = double.Parse(stringValue);
                    }
                    else if (toType == typeof(string))
                    {
                        value = stringValue;
                    }
                    else if (toType.IsEnum)
                    {
                        var enumName = Enum.GetNames(toType).FindByName(_ => _, stringValue, "values");
                        value = Enum.Parse(toType, enumName);
                    }

                    return value;
                }
                catch (Exception e)
                {
                    throw new Exception($"{stringValue.Quote()} is not a value of type {toType.Name}. Exception {e}");
                }
            }

            private static void SetStringValue(PropertyInfo p, object x, string stringValue)
            {
                if (p.PropertyType.IsArray)
                {
                    var elementType = p.PropertyType.GetElementType();
                    var existingArray = (Array)p.GetValue(x);
                    if (existingArray == null)
                    {
                        existingArray = Array.CreateInstance(elementType, 0);
                        p.SetValue(x, existingArray);
                    }

                    var newElement = Parse(elementType, stringValue);
                    var newArray = Array.CreateInstance(elementType, existingArray.Length + 1);
                    existingArray.CopyTo(newArray, 0);
                    newArray.SetValue(newElement, newArray.Length - 1);
                    p.SetValue(x, newArray);
                }
                else
                {
                    var value = Parse(p.PropertyType, stringValue);
                    p.SetValue(x, value);
                }
            }
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

        private static object[] ParseParameters(MethodInfo method, string[] arguments)
        {
            return method.GetParameters()
                .ZipOrDefault(arguments, (p, v) => GetOptOption.Parse(p.ParameterType, v))
                .ToArray();
        }
    }
}
