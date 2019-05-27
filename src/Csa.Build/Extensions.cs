using System;
using System.IO;
using System.Threading.Tasks;

namespace Csa.Build
{
    public static class Extensions
    {
        public static string Quote(this string x)
        {
            return "\"" + x.Replace("\"", "\\\"") + "\"";
        }

        public static object Dump(this object x) => GetWritable(_ => _.Dump(x));
        
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

        public static object GetWritable(Action<TextWriter> w)
        {
            return new Writable(w);
        }

        class Writable
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
                    writer(w);
                    return w.ToString();
                }
            }
        }

        public static TextReader Tee(this TextReader input, TextWriter output)
        {
            return new TeeStream(input, output);
        }
    }
}