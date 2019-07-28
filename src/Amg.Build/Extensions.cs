using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Amg.Build
{
    public static class Extensions
    {
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

        public static TextReader Tee(this TextReader input, TextWriter output)
        {
            return new TeeStream(input, output);
        }

        public static TextReader Tee(this TextReader input, Action<string> output)
        {
            return new TeeStream(input, new ActionStream(output));
        }

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
