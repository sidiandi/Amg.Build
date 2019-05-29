using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Csa.Build
{
    public static class Extensions
    {
        public static string Quote(this string x)
        {
            return "\"" + x.Replace("\"", "\\\"") + "\"";
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

        public static IWritable ToTable<T>(this IEnumerable<T> e)
        {
            var type = typeof(T);
            var properties = type.GetProperties();
            var index = new object[] { };
            return Table(new []{ properties.Select(_ => _.Name) }
                .Concat(e.Select(_ => properties.Select(p => p.GetValue(_, index).SafeToString()))));
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

                foreach (var row in data)
                {
                    foreach (var c in row.Zip(columnWidth, (cell, width) => new { cell, width }))
                    {
                        w.Write(c.cell);
                        w.Write(new string(' ', c.width - c.cell.Length + 1));
                    }
                    w.WriteLine();
                }
            });
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
    }
}