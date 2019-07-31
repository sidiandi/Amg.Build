using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Amg.Build
{
    /// <summary>
    /// Extensions for IEnumerable
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Concat one (1) new element
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="e"></param>
        /// <param name="newElement"></param>
        /// <returns></returns>
        public static IEnumerable<T> Concat<T>(this IEnumerable<T> e, T newElement)
        {
            return e.Concat(Enumerable.Repeat(newElement, 1));
        }

        /// <summary>
        /// Convert to strings and concatenate with separator
        /// </summary>
        /// <param name="e"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string Join(this IEnumerable<object> e, string separator)
        {
            return string.Join(separator, e.Where(_ => _ != null));
        }

        /// <summary>
        /// Convert to strings and concatenate with newline
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static string Join(this IEnumerable<object> e)
        {
            return e.Join(System.Environment.NewLine);
        }

        /// <summary>
        /// Split a string into lines
        /// </summary>
        /// <param name="multiLineString"></param>
        /// <returns></returns>
        public static IEnumerable<string> SplitLines(this string multiLineString)
        {
            using (var r = new StringReader(multiLineString))
            {
                while (true)
                {
                    var line = r.ReadLine();
                    if (line == null)
                    {
                        break;
                    }
                    yield return line;
                }
            }
        }

        /// <summary>
        /// Zips together two sequences. The shorter sequence is padded with default values.
        /// </summary>
        /// <typeparam name="TFirst"></typeparam>
        /// <typeparam name="TSecond"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <param name="resultSelector"></param>
        /// <returns></returns>
        public static IEnumerable<TResult> ZipOrDefault<TFirst, TSecond, TResult>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second, Func<TFirst, TSecond, TResult> resultSelector)
        {
            using (var i0 = first.GetEnumerator())
            using (var i1 = second.GetEnumerator())
            {
                while (true)
                {
                    var firstHasElement = i0.MoveNext();
                    var secondHasElement = i1.MoveNext();
                    if (firstHasElement || secondHasElement)
                    {
                        yield return resultSelector(
                            firstHasElement ? i0.Current : default(TFirst),
                            secondHasElement ? i1.Current : default(TSecond)
                            );
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the element i for which selector(i) is maximal
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="Y"></typeparam>
        /// <param name="e"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static T MaxElement<T, Y>(this IEnumerable<T> e, Func<T, Y> selector) where Y : IComparable
        {
            try
            {
                var m = e.First();

                var maxValue = selector(m);
                foreach (var i in e.Skip(1))
                {
                    var value = selector(i);
                    if (value.CompareTo(maxValue) == 1)
                    {
                        maxValue = value;
                        m = i;
                    }
                }
                return m;
            }
            catch (System.InvalidOperationException)
            {
                return default(T);
            }
        }

        /// <summary>
        /// Find an element by name. The name of an element i is determined by name(i). 
        /// </summary>
        /// Abbreviations are allowed: query can also be a substring of the name as long as it uniquely
        /// identifies an element.
        /// <typeparam name="T"></typeparam>
        /// <param name="candidates"></param>
        /// <param name="name">calculates the name of an element</param>
        /// <param name="query">the name (part) to be found.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">When query does not identify a named element.</exception>
        public static T FindByName<T>(this IEnumerable<T> candidates, Func<T, string> name, string query)
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

            throw new ArgumentOutOfRangeException($@"{query.Quote()} not found in

{candidates.Select(name).Join()}

");
        }
    }
}
