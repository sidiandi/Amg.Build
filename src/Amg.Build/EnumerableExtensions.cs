using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Amg.Build
{
    /// <summary>
    /// Extensions for IEnumerable
    /// </summary>
    public static class EnumerableExtensions
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

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
        public static string Join(this IEnumerable<object?> e, string separator)
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
        public static IEnumerable<string> SplitLines(this string? multiLineString)
        {
            if (multiLineString != null)
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
        public static IEnumerable<TResult> ZipOrDefaultValue<TFirst, TSecond, TResult>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second, Func<TFirst, TSecond, TResult> resultSelector)
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
                            firstHasElement ? i0.Current : default,
                            secondHasElement ? i1.Current : default
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
        /// Zips together two sequences. The shorter sequence is padded with default values.
        /// </summary>
        /// <typeparam name="TFirst"></typeparam>
        /// <typeparam name="TSecond"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <param name="resultSelector"></param>
        /// <returns></returns>
        public static IEnumerable<TResult> ZipPadSecond<TFirst, TSecond, TResult>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second, Func<TFirst, TSecond, TResult> resultSelector)
        {
            using (var i0 = first.GetEnumerator())
            using (var i1 = second.GetEnumerator())
            {
                while (true)
                {
                    var firstHasElement = i0.MoveNext();
                    var secondHasElement = i1.MoveNext();
                    if (firstHasElement)
                    {
                        yield return resultSelector(
                            i0.Current,
                            secondHasElement ? i1.Current : default
                            );
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

#nullable disable
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
            where TResult : class
            where TFirst : class
            where TSecond : class
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
        /// Pads the sequence with null if it is not long enough
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="e"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static IEnumerable<T> Pad<T>(this IEnumerable<T> e, int count)
        {
            using (var currentItem = e.GetEnumerator())
            {
                for (int i = 0; i < count; ++i)
                {
                    yield return currentItem.MoveNext()
                        ? currentItem.Current
                        : default(T);
                }
            }
        }
#nullable enable

        /// <summary>
        /// Returns the element i for which selector(i) is maximal
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="Y"></typeparam>
        /// <param name="e"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static T? MaxElement<T, Y>(this IEnumerable<T> e, Func<T, Y> selector)
            where T : class
            where Y : IComparable 
        {
            using (var i = e.GetEnumerator())
            {
#nullable disable
                T max = default(T);
                Y maxValue = default(Y);
                bool found = false;

                if (i.MoveNext())
                {
                    max = i.Current;
                    maxValue = selector(i.Current);
                    found = true;
                }
                while (i.MoveNext())
                {
                    var value = selector(i.Current);
                    if (value.CompareTo(maxValue) == 1)
                    {
                        maxValue = value;
                        max = i.Current;
                    }
                }
                return found ? max : default(T);
#nullable enable
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
        public static T? FindByNameOrDefault<T>(this IEnumerable<T> candidates, Func<T, string> name, string query) where T : class
        {
            try
            {
                return candidates.FindByName(name, query);
            }
            catch (Exception)
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
        /// <param name="itemsName"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">When query does not identify a named element.</exception>
        public static T FindByName<T>(
            this IEnumerable<T> candidates, 
            Func<T, string> name, 
            string query,
            string? itemsName = null)
        {
            var r = candidates.SingleOrDefault(option =>
                name(option).Equals(query, StringComparison.InvariantCultureIgnoreCase));

            if (r != null)
            {
                return r;
            }

            var matches = candidates.Where(option => query.IsAbbreviation(name(option)))
                .ToList();

            if (matches.Count > 1)
            {
                throw new ArgumentOutOfRangeException($@"{query.Quote()} is ambiguous. Could be

{matches.Select(name).Join()}

");
            }

            if (matches.Count == 1)
            {
                return matches[0];
            }

            throw new ArgumentOutOfRangeException(nameof(query), query, $@"{query.Quote()} not found in {itemsName}

{candidates.Select(_ => "  " + name(_)).Join()}
");
        }

        static IProgress<T> ToProgress<T>(Action<T> a)
        {
            return new ProgressAction<T>(a);
        }

        /// <summary>
        /// Shows progress information is enumerating takes longer
        /// </summary>
        /// <param name="e"></param>
        /// <param name="metric"></param>
        /// <param name="updateInterval"></param>
        /// <param name="metricUnit"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public static IEnumerable<T> Progress<T>(this IEnumerable<T> e, 
            Func<T, double>? metric = null, 
            TimeSpan updateInterval = default(TimeSpan),
            string? metricUnit = null,
            string? description = null,
            Serilog.ILogger? logger = null
            )
        {
            if (description == null)
            {
                description = String.Empty;
            }
            else
            {
                description = description + " ";
            }

            if (metric == null)
            {
                metric = (item) => 1.0;
            }

            if (updateInterval == default(TimeSpan))
            {
                updateInterval = TimeSpan.FromSeconds(2);
            }

            if (metricUnit == null)
            {
                metricUnit = String.Empty;
            }

            if (logger == null)
            {
                logger = Logger;
            }

            Func<double, string> format = (double x) => x.MetricShort();

            var speedUnit = $"{metricUnit}/s";

            var progress = ToProgress((ProgressUpdate<T> p) =>
            {
                logger.Information("{description}{total}{unit} complete, {speed}{speedUnit}: {item}",
                    description,
                    format(p.Total),
                    metricUnit,
                    format(p.Speed),
                    speedUnit, p.Current);
            });

            return e.Progress(progress,
                metric: metric,
                updateInterval: updateInterval
                );
        }

        public static IEnumerable<T> Progress<T>(this IEnumerable<T> e,
            IProgress<ProgressUpdate<T>> progress,
            Func<T, double>? metric = null,
            TimeSpan updateInterval = default(TimeSpan)
            )
        {
            if (metric == null)
            {
                metric = (item) => 1.0;
            }
            if (updateInterval == default(TimeSpan))
            {
                updateInterval = TimeSpan.FromSeconds(2);
            }
            double total = 0.0;
            TimeSpan nextUpdate = TimeSpan.FromTicks(updateInterval.Ticks * 5);
            var stopwatch = Stopwatch.StartNew();
            var speedWindow = TimeSpan.FromTicks(updateInterval.Ticks * 5);
            double delta = 0.0;
            double speed = 0.0;
            TimeSpan lastUpdate = TimeSpan.Zero;

            return e.Select(_ =>
            {
                delta += metric(_);
                var elapsed = stopwatch.Elapsed;
                if (elapsed > nextUpdate)
                {
                    var deltaT = elapsed - lastUpdate;
                    total += delta;
                    speed = (speed * speedWindow.TotalSeconds + delta / deltaT.TotalSeconds) / (speedWindow.TotalSeconds + deltaT.TotalSeconds);
                    progress.Report(new ProgressUpdate<T>(_, elapsed, total, speed));

                    nextUpdate = stopwatch.Elapsed + updateInterval;
                    delta = 0.0;
                    lastUpdate = elapsed;
                }

                return _;
            });
        }

        /// <summary>
        /// Progress when iterating an IEnumerable T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class ProgressUpdate<T>
        {
            /// <summary />
            public ProgressUpdate(T current, TimeSpan elapsed, double total, double speed)
            {
                this.Current = current;
                Elapsed = elapsed;
                Total = total;
                Speed = speed;
            }

            /// <summary>
            /// Current item of the enumeration
            /// </summary>
            public T Current { get; }

            /// <summary>
            /// Total metric
            /// </summary>
            public double Total { get; }

            /// <summary>
            /// metric Speed (windowed)
            /// </summary>
            public double Speed { get; }

            /// <summary>
            /// Total elapsed time since start of the enumeration
            /// </summary>
            public TimeSpan Elapsed { get; }
        }

        /// <summary>
        /// Shows progress information is enumerating takes longer
        /// </summary>
        /// <param name="e"></param>
        /// <param name="progress"></param>
        /// <param name="metric"></param>
        /// <param name="updateInterval"></param>
        /// <returns></returns>
        /// <summary>
        /// Splits a string and returns the re-combined components
        /// </summary>
        /// "1.2.3".Lineage(".") returns ["1", "1.2", "1.2.3"]
        /// <param name="x"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static IEnumerable<string> Lineage(this string x, string separator)
        {
            var p = x.Split(new[] { separator }, StringSplitOptions.None);
            return p.Lineage().Select(_ => _.Join(separator));
        }

        /// <summary>
        /// Sequence of sequences that contain successively more elements of e
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="e"></param>
        /// <returns></returns>
        public static IEnumerable<IEnumerable<T>> Lineage<T>(this IEnumerable<T> e)
        {
            var count = 0;
            foreach (var i in e)
            {
                ++count;
                yield return e.Take(count);
            }
        }

        public static IEnumerable<T> NotNull<T>(this IEnumerable<T?> e) where T : class
        {
#pragma warning disable S1905 // Redundant casts should not be used
            // cast required from T? to T
            return e.Where(_ => _ != null).Cast<T>();
#pragma warning restore S1905 // Redundant casts should not be used
        }

        public static async Task<IEnumerable<T>> Result<T>(this IEnumerable<Task<T>> e)
        {
            var results = new List<T>();
            foreach (var i in e)
            {
                results.Add(await i);
            }
            return results;
        }

        /// <summary>
        /// Returns e except the last c elements
        /// </summary>
        /// <param name="e"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static IEnumerable<T> TakeAllBut<T>(this IEnumerable<T> e, int c)
        {
            if (c == 0)
            {
                return e;
            }
            if (c < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(c), c, "cannot be negative");
            }
            return TakeAllButInternal(e, c);
        }

        static IEnumerable<T> TakeAllButInternal<T>(IEnumerable<T> e, int c)
        { 
            var buffer = new T[c];
            int bi = 0;
            using (var i = e.GetEnumerator())
            {
                while (bi < c && i.MoveNext())
                {
                    buffer[bi++] = i.Current;
                }
                while (i.MoveNext())
                {
                    if (bi == c)
                    {
                        bi = 0;
                    }
                    var o = buffer[bi];
                    buffer[bi] = i.Current;
                    ++bi;
                    yield return o;
                }
            }
        }

        public static bool StartsWith<T>(this IEnumerable<T> e, IEnumerable<T> start)
        {
            return start.ZipPadSecond(e, (i1, i2) => object.Equals(i1, i2)).All(_ => _);
        }

        public static int IndexOf<T>(this IEnumerable<T> e, T searchValue)
        {
            int index = 0;
            foreach (var i in e)
            {
                if (object.Equals(i, searchValue))
                {
                    return index;
                }
                ++index;
            }
            return -1;
        }
    }
}
