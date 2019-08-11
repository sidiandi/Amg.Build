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
                        var stringRepresentation = p.GetValue(x, new object[] { }).SafeToString();
                        if (stringRepresentation.SplitLines().Skip(1).Any())
                        {
                            w.WriteLine($@"{p.Name}:
{stringRepresentation}
");
                        }
                        else
                        {
                            w.WriteLine($"{p.Name}: {stringRepresentation}");
                        }
                    }
                    catch { }
                }
            }
            return w;
        }

        static string CellText(object x)
        {
            return x.SafeToString().OneLine().Truncate(80);
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
                    .Concat(e.Select(_ => properties.Select(p => CellText(p.GetValue(_, index))))));
            }
            else
            {
                return Table(e.Select(_ => properties.Select(p => CellText(p.GetValue(_, index)))));
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
            if (!(rangeBegin < rangeEnd))
            {
                throw new ArgumentOutOfRangeException($"(rangeBegin < rangeEnd) {rangeBegin} {rangeEnd}");
            }

            int Pos(DateTime t)
            {
                t = t.Limit(rangeBegin, rangeEnd);
                return (int)((t - rangeBegin).TotalSeconds / (rangeEnd - rangeBegin).TotalSeconds * width);
            }
            var beginPos = Pos(begin).Limit(0, width - 1);
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

        /// <summary>
        /// Skip lines in the middle if text exceed maxLines lines.
        /// </summary>
        /// <param name="longMultilineText"></param>
        /// <param name="maxLines"></param>
        /// <returns></returns>
        public static IWritable ReduceLines(this string longMultilineText, int maxLines)
        {
            return ReduceLines(longMultilineText, maxLines, maxLines / 2);
        }

        /// <summary>
        /// Skip lines if text exceed maxLines lines.
        /// </summary>
        /// <param name="longMultilineText"></param>
        /// <param name="maxLines"></param>
        /// <param name="skipAt">specifies after how many lines lines should be skipped.</param>
        /// <returns></returns>
        public static IWritable ReduceLines(this string longMultilineText, int maxLines, int skipAt) => GetWritable(w =>
        {
            var e = longMultilineText.SplitLines().GetEnumerator();

            var part1 = new List<string>();
            var part2 = new List<string>();

            var part1MaxLines = skipAt;
            var part2MaxLines = maxLines - skipAt;

            for (; e.MoveNext();)
            {
                part1.Add(e.Current);
                if (part1.Count >= part1MaxLines)
                {
                    break;
                }
            }

            int skipped = 0;

            for (; e.MoveNext();)
            {
                part2.Add(e.Current);
                if (part2.Count >= part2MaxLines)
                {
                    part2.RemoveAt(0);
                    ++skipped;
                }
            }

            foreach (var line in part1)
            {
                w.WriteLine(line);
            }

            if (skipped > 0)
            {
                w.WriteLine($"... {skipped} lines skipped ...");
            }

            foreach (var line in part2)
            {
                w.WriteLine(line);
            }
        });

        /// <summary>
        /// Formats a number using metric prefixes
        /// </summary>
        /// https://en.wikipedia.org/wiki/Unit_prefix#Metric_prefixes
        /// <param name="x"></param>
        /// <param name="digits"></param>
        /// <returns></returns>
        public static string Metric(this double x, int digits = 3)
        {
            var prefixes = new[]
            {
                "tera", // 10^12
                "giga",
                "mega",
                "kilo",
                String.Empty,
                "milli",
                "micro",
                "nano",
                "pico",
            };
            return MetricImpl(x, prefixes, digits);
        }

        /// <summary>
        /// Formats a number using metric prefixes (short names like µ, m, k, M, G)
        /// </summary>
        /// https://en.wikipedia.org/wiki/Unit_prefix#Metric_prefixes
        /// <param name="x"></param>
        /// <param name="digits"></param>
        /// <returns></returns>
        public static string MetricShort(this double x, int digits = 3)
        {
            var prefixes = new[]
            {
                "T", // 10^12
                "G",
                "M",
                "k",
                String.Empty,
                "m",
                "µ",
                "n",
                "p",
            };
            return MetricImpl(x, prefixes, digits);
        }

        static string MetricImpl(this double x, string[] prefixes, int digits = 3)
        {
            var e = Math.Log10(Math.Abs(x));
            var i = (int)(4 - (Math.Log10(Math.Abs(x))-2) / 3);
            if (i >= prefixes.Length && i < prefixes.Length + 2)
            {
                i = prefixes.Length - 1;
            }
            if (i<0 || i>= prefixes.Length)
            {
                return x.ToString("E" + digits.ToString());
            }
            var prefix = Math.Pow(10.0, 12 - i * 3);
            var value = Math.Sign(x) * x / prefix;
            return value.ToString("G" + digits.ToString()) + " " + prefixes[i];
        }
    }
}
