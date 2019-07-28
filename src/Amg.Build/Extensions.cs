using System;
using System.Collections.Generic;
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
