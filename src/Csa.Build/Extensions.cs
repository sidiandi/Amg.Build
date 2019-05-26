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

        public static string EnsureParentDirectoryExists(this string path)
        {
            path.GetParentDirectory().EnsureDirectoryExists();
            return path;
        }

        public static string EnsureFileNotExists(this string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            return path;
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

        public static string EnsureDirectoryExists(this string dir)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return dir;
        }

        static string GetParentDirectory(this string path)
        {
            return System.IO.Path.GetDirectoryName(path);
        }

        public static async Task<string> WriteAllTextIfChangedAsync(this string path, string text)
        {
            var hasChanged = !File.Exists(path) || !(File.ReadAllText(path)).Equals(text);

            if (hasChanged)
            {
                File.WriteAllText(path.EnsureParentDirectoryExists(), text);
            }

            await Task.CompletedTask;

            return path;
        }
    }
}