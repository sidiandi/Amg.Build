using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Amg.Build
{
    public static class Extensions
    {
        public static string Quote(this string x)
        {
            return "\"" + x.Replace("\"", "\\\"") + "\"";
        }

        public static string Join(this IEnumerable<object> e, string separator)
        {
            return string.Join(separator, e.Where(_ => _ != null));
        }

        public static string Join(this IEnumerable<object> e)
        {
            return e.Join(System.Environment.NewLine);
        }

        public static IWritable Dump(this object x) => GetWritable(_ => _.Dump(x));
        
        public static TextWriter Dump(this TextWriter w, object x)
        {
            var type = x.GetType();
            if (type.IsPrimitive || type.Equals(typeof(string)))
            {
                w.WriteLine(x.ToString());
            }
            else
            {
                foreach (var p in type.GetProperties())
                {
                    try
                    {
                        w.WriteLine($"{p.Name}: {p.GetValue(x, new object[] { })}");
                    }
                    catch { }
                }
            }
            return w;
        }

        public static IWritable ToTable<T>(this IEnumerable<T> e, bool header = false)
        {
            var type = typeof(T);
            var properties = type.GetProperties();
            var index = new object[] { };
            if (header)
            {
                return Table(new[] { properties.Select(_ => _.Name) }
                    .Concat(e.Select(_ => properties.Select(p => p.GetValue(_, index).SafeToString()))));
            }
            else
            {
                return Table(e.Select(_ => properties.Select(p => p.GetValue(_, index).SafeToString())));
            }
        }

        public static IWritable ToPropertiesTable(this object x)
        {
            return x.GetType()
                .GetProperties()
                .Select(p => new { p.Name, Value = p.GetValue(x, new object[] { }) })
                .ToTable(header: false);
        }

        public static string SafeToString(this object x)
        {
            try
            {
                return x.ToString();
            }
            catch
            {
                return String.Empty;
            }
        }

        public static IWritable Table(IEnumerable<IEnumerable<string>> data)
        {
            return GetWritable(w =>
            {
                var columnWidth = data.Select(_ => _.Select(c => c.Length)).Aggregate(Max);
                var columnSeparator = " ";

                foreach (var row in data)
                {
                    w.WriteLine(
                        row.Zip(columnWidth, (cell, width) => new { cell, width })
                        .Select(c => c.cell + new string(' ', c.width - c.cell.Length))
                        .Join(columnSeparator));
                }
            });
        }

        public static string TimeBar(int width, DateTime rangeBegin, DateTime rangeEnd, DateTime begin, DateTime end)
        {
            int Pos(DateTime t)
            {
                return (int)((t - rangeBegin).TotalSeconds / (rangeEnd - rangeBegin).TotalSeconds * width);
            }
            var beginPos = Pos(begin);
            var endPos = Math.Max(Pos(end), beginPos + 1);
            const char empty = ' ';
            const char full = '#';
            return new string(empty, beginPos) + new string(full, endPos - beginPos) + new string(empty, width - endPos);
        }

        static IEnumerable<int> Max(IEnumerable<int> e0, IEnumerable<int> e1)
        {
            return e0.ZipOrDefault(e1, Math.Max);
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

        public static IWritable GetWritable(this Action<TextWriter> w)
        {
            return new Writable(w);
        }

        class Writable : IWritable
        {
            private readonly Action<TextWriter> writer;

            public Writable(Action<TextWriter> writer)
            {
                this.writer = writer;
            }

            public override string ToString()
            {
                using (var w = new StringWriter())
                {
                    Write(w);
                    return w.ToString();
                }
            }

            public void Write(TextWriter textWriter)
            {
                writer(textWriter);
            }
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
            if (dictionary.TryGetValue(key, out Value value))
            {
            }
            else
            {
                value = dictionary[key] = factory();
            }
            return value;
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

        public static T MaxElement<T, Y>(this IEnumerable<T> e, Func<T, Y> selector) where Y : IComparable
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
    }
}
