using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Amg.Build
{
    /// <summary>
    /// Mixed extensions
    /// </summary>
    public static class Extensions
    {
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
            return new TeeStream(input, output);
        }


        /// <summary>
        /// Transforms input into a TextReader that reads from input and calls outputs on all lines read.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        public static TextReader Tee(this TextReader input, Action<string> output)
        {
            return new TeeStream(input, new ActionStream(output));
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
                if (dictionary.TryGetValue(key, out Value value))
                {
                }
                else
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
        /// Transforms x into type Y when x is not null. Returns null otherwise.
        /// </summary>
        /// <typeparam name="Y"></typeparam>
        /// <typeparam name="X"></typeparam>
        /// <param name="x"></param>
        /// <param name="mapper"></param>
        /// <returns></returns>
        public static Y Map<Y, X>(this X x, Func<X, Y> mapper) where X: new()
        {
            if (x == null)
            {
                return default(Y);
            }
            else
            {
                return mapper(x);
            }
        }
    }
}
