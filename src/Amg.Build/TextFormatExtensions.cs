using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Amg.Build
{
    /// <summary>
    /// Extensions for formatted text output
    /// </summary>
    public static class TextFormatExtensions
    {
        /// <summary>
        /// Quote only if x contains whitespace.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static string QuoteIfRequired(this string x)
        {
            return x.Any(Char.IsWhiteSpace)
                ? x.Quote()
                : x;
        }

        /// <summary>
        /// Quotes a string.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static string Quote(this string x)
        {
            return "\"" + x.Replace("\"", "\\\"") + "\"";
        }


        /// <summary>
        /// Writes instance properties
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static IWritable Dump(this object x) => GetWritable(_ => _.Dump(x));

        static TextWriter Dump(this TextWriter w, object x)
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

        /// <summary>
        /// prints the properties of T in a table
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="e"></param>
        /// <param name="header">true: print property names as header</param>
        /// <returns></returns>
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

        /// <summary>
        /// object properties as table
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static IWritable ToPropertiesTable(this object x)
        {
            return x.GetType()
                .GetProperties()
                .Select(p => new { p.Name, Value = p.GetValue(x, new object[] { }) })
                .ToTable(header: false);
        }

        /// <summary>
        /// like ToString, but never throws. x can also be null.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Print a table from a sequence of rows containing sequences of column data.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static IWritable Table(this IEnumerable<IEnumerable<string>> data)
        {
            IEnumerable<int> Max(IEnumerable<int> e0, IEnumerable<int> e1)
            {
                return e0.ZipOrDefault(e1, Math.Max);
            }

            return GetWritable(w =>
            {
                var columnWidth = data.Select(_ => _.Select(c => c.Length)).Aggregate(Enumerable.Empty<int>(), Max);
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

        /// <summary>
        /// represents a time interval in a larger time interval as time line.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="rangeBegin"></param>
        /// <param name="rangeEnd"></param>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Converts an TextWriter output action to an object that can yields the output as ToString().
        /// </summary>
        /// <param name="w"></param>
        /// <returns></returns>
        public static IWritable GetWritable(this Action<TextWriter> w)
        {
            return new Writable(w);
        }
    }
}
