using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Amg.Build
{
    public static class TextFormatExtensions
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

        public static IWritable Table(this IEnumerable<IEnumerable<string>> data)
        {
            IEnumerable<int> Max(IEnumerable<int> e0, IEnumerable<int> e1)
            {
                return e0.ZipOrDefault(e1, Math.Max);
            }

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

    }
}
