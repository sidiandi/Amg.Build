using System;
using System.Collections.Generic;
using System.Linq;

namespace Amg.Build
{
    public static class EnumerableExtensions
    {
        public static string Join(this IEnumerable<object> e, string separator)
        {
            return string.Join(separator, e.Where(_ => _ != null));
        }

        public static string Join(this IEnumerable<object> e)
        {
            return e.Join(System.Environment.NewLine);
        }

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

            throw new Exception($@"{query.Quote()} not found in

{candidates.Select(name).Join()}

");
        }
    }
}
