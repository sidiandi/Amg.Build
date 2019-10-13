using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Amg.Extensions
{

    /// <summary>
    /// Mixed extensions
    /// </summary>
    public static class Utils
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

        /// <summary>
        /// Easy readable text format for a TimeSpan
        /// </summary>
        /// <param name="duration"></param>
        /// <returns></returns>
        internal static string HumanReadable(this TimeSpan duration)
        {
            var days = duration.TotalDays;
            if (days > 10)
            {
                return $"{days:F0}d";
            }
            if (days > 1)
            {
                return $"{days:F0}d{duration.Hours}h";
            }
            var hours = duration.TotalHours;
            if (hours > 1)
            {
                return $"{duration.Hours}h{duration.Minutes}m";
            }
            var minutes = duration.TotalMinutes;
            if (minutes > 30)
            {
                return $"{duration.Minutes}m";
            }
            if (minutes > 1)
            {
                return $"{duration.Minutes}m{duration.Seconds}s";
            }
            return $"{duration.Seconds}s";
        }

        /// <summary>
        /// Transforms input into a TextReader that reads from input and outputs all characters read on TextWriter output as well.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        public static TextReader Tee(this TextReader input, TextWriter output)
        {
            return new TeeTextReader(input, output);
        }


        /// <summary>
        /// Transforms input into a TextReader that reads from input and calls outputs on all lines read.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        public static TextReader Tee(this TextReader input, Action<string> output)
        {
            return new TeeTextReader(input, output.AsTextWriter());
        }

        /// <summary>
        /// Returns a TextWriter that calls output for every WriteLine
        /// </summary>
        /// <param name="output"></param>
        /// <returns></returns>
        public static TextWriter AsTextWriter(this Action<string> output)
        {
            return new ActionTextWriter(output);
        }

        /// <summary>
        /// "Caching" function: get an element of a dictionary or creates it and adds it if it not yet exists.
        /// </summary>
        /// <typeparam name="Key"></typeparam>
        /// <typeparam name="Value"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="key"></param>
        /// <param name="factory"></param>
        /// <returns></returns>
        public static Value GetOrAdd<Key, Value>(this IDictionary<Key, Value> dictionary, Key key, Func<Value> factory)
        {
            lock (dictionary)
            {
                if (!dictionary.TryGetValue(key, out Value value))
                {
                    Monitor.Exit(dictionary);
                    try
                    {
                        value = factory();
                    }
                    finally
                    {
                        Monitor.Enter(dictionary);
                    }
                    dictionary[key] = value;
                    return value;
                }
                return value;
            }
        }

        /// <summary>
        /// Merge two dictionaries.
        /// </summary>
        ///  Keys of b which are already present in a will overwrite the entry in a
        /// <typeparam name="Key"></typeparam>
        /// <typeparam name="Value"></typeparam>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static IDictionary<Key, Value> Merge<Key, Value>(this IDictionary<Key, Value> a, IDictionary<Key, Value> b)
        {
            var r = new Dictionary<Key, Value>();
            foreach (var i in a.Concat(b))
            {
                r[i.Key] = i.Value;
            }
            return r;
        }

        /// <summary>
        /// Add entries of newEntries to dictionaryToGrow
        /// </summary>
        /// <param name="dictionaryToGrow"></param>
        /// <param name="newEntries"></param>
        public static void Add(this StringDictionary dictionaryToGrow, IDictionary<string, string> newEntries)
        {
            foreach (var i in newEntries)
            {
                dictionaryToGrow[i.Key] = i.Value;
            }
        }

        /// <summary>
        /// Limit x in [a,b]
        /// </summary>
        public static DateTime Limit(this DateTime x, DateTime a, DateTime b)
        {
            if (x < a)
            {
                return a;
            }
            else
            {
                if (x > b)
                {
                    return b;
                }
                else
                {
                    return x;
                }
            }
        }

        /// <summary>
        /// Limit x in [a,b]
        /// </summary>
        public static int Limit(this int x, int a, int b)
        {
            if (x < a)
            {
                return a;
            }
            else
            {
                if (x > b)
                {
                    return b;
                }
                else
                {
                    return x;
                }
            }
        }

        /// <summary>
        /// Hex-encode data
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string Hex(this IEnumerable<byte> data)
        {
            return data.Select(_ => _.ToString("x2")).Join(String.Empty);
        }

        /// <summary>
        /// Returns the nuget version of the assembly.
        /// </summary>
        /// Expects this information in assembly metadata "NuGetVersionV2"
        /// <param name="a"></param>
        /// <returns></returns>
        public static string NugetVersion(this Assembly a) => a
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(_ => _.Key.Equals("NuGetVersionV2")).Value;
    }
}
