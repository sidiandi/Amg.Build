using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading;

namespace Amg.Build
{

    /// <summary>
    /// Mixed extensions
    /// </summary>
    public static class Extensions
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
        /// Transforms x into type Y when x is not null. Returns null otherwise.
        /// </summary>
        /// <typeparam name="Y"></typeparam>
        /// <typeparam name="X"></typeparam>
        /// <param name="x"></param>
        /// <param name="mapper"></param>
        /// <returns></returns>
        public static Y? Map<Y, X>(this X? x, Func<X, Y?> mapper) where X: class where Y: class
        {
            if (x == null)
            {
                return default;
            }
            else
            {
                return mapper(x);
            }
        }

        /// <summary>
        /// Executes an action when x is not null.
        /// </summary>
        /// <typeparam name="Y"></typeparam>
        /// <typeparam name="X"></typeparam>
        /// <param name="x"></param>
        /// <param name="mapper"></param>
        /// <returns></returns>
        public static X? Map<X>(this X? x, Action<X>? onNotNull, Action? onNull = null) where X : class
        {
            if (x == null)
            {
                if (onNull != null)
                {
                    onNull();
                }
                return x;
            }
            else
            {
                if (onNotNull != null)
                {
                    onNotNull(x);
                }
                return x;
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
        /// Cut of the tail of a string if it is longer than maxLength
        /// </summary>
        /// <param name="x"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        public static string Truncate(this string x, int maxLength)
        {
            return (x.Length > maxLength)
                ? x.Substring(0, maxLength)
                : x;
        }

        /// <summary>
        /// Replace line breaks by ' '
        /// </summary>
        /// <returns></returns>
        public static string OneLine(this string x)
        {
            return x.SplitLines().Join(" ");
        }

        /// <summary>
        /// Limit string to maxLength. Replace tail end with md5 checksum to keep the string unique.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        public static string TruncateMd5(this string x, int maxLength)
        {
            if (x.Length > maxLength)
            {
                var md5 = x.Md5Checksum();
                return x.Truncate(maxLength - md5.Length) + md5;
            }
            else
            {
                return x;
            }
        }

        /// <summary>
        /// Hex encoded MD5 checksum
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static string Md5Checksum(this string x)
        {
            var md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            var bytes = System.Text.UTF8Encoding.UTF8.GetBytes(x);
            var hash = md5.ComputeHash(bytes);
            return hash.Hex();
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
        /// True, if abbreviation is a valid abbreviation of word.
        /// </summary>
        /// Abbreviation means that all characters of abbreviation appear in word in 
        /// exactly the order they appear in abbreviation.
        /// <param name="abbreviation"></param>
        /// <param name="word"></param>
        /// <returns></returns>
        public static bool IsAbbreviation(this string abbreviation, string word)
        {
            if (abbreviation.Length == 0)
            {
                return true;
            }

            if (word.Length == 0)
            {
                return false;
            }

            if (char.ToLower(word[0]) == char.ToLower(abbreviation[0]))
            {
                if (abbreviation.Length == 1)
                {
                    return true;
                }
                else if (word.Length == 1)
                {
                    return false;
                }
                else
                {
                    var restAbbreviation = abbreviation.Substring(1);
                    var restWords = Enumerable.Range(1, word.Length - 1).Select(_ => word.Substring(_));
                    return restWords.Max(_ => restAbbreviation.IsAbbreviation(_));
                }
            }
            else
            {
                return false;
            }
        }
    }
}
