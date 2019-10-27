using Amg.Extensions;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Amg.FileSystem
{
    class FileVersion : IEquatable<FileVersion>
    {
        public string Name { get; set; }
        public DateTime LastWriteTimeUtc { get; set; }
        public long Length { get; set; }
        public FileVersion[] Childs { get; set; }

        public FileVersion()
        {
            Name = String.Empty;
            Childs = new FileVersion[] { };
        }

        public override string ToString() => Dump().ToString();

        public IWritable Dump(string? indent = null) => TextFormatExtensions.GetWritable(w =>
        {
            if (indent == null) indent = String.Empty;

            w.Write(indent);
            w.WriteLine(new object[] { Name, LastWriteTimeUtc, Length }.Join("|"));
            indent = indent + "  ";
            foreach (var i in Childs)
            {
                i.Dump(indent).Write(w);
            }
        });

        public static bool DefaultIgnore(string  _)
        {
            var n = _.FileName();
            return n.Equals("bin")
                || n.Equals("obj")
                || n.Equals(".vs");
        }

        public static async Task<FileVersion?> Get(string path)
        {
            return await Get(path, _ => DefaultIgnore(_));
        }

        public static async Task<FileVersion?> Get(string path, Func<string, bool> ignore)

        {
            if (path.IsFile())
            {
                var info = new FileInfo(path);
                return new FileVersion
                {
                    Name = path.FileName(),
                    LastWriteTimeUtc = info.LastWriteTimeUtc,
                    Length = info.Length,
                    Childs = new FileVersion[] { }
                };
            }
            else if (path.IsDirectory())
            {
                return new FileVersion
                {
                    Name = path.FileName(),
                    LastWriteTimeUtc = default(DateTime),
                    Length = 0,
                    Childs = (await path.EnumerateFileSystemEntries()
                    .Where(_ => !ignore(_))
                    .Select(async _ => await Get(_))
                    .Result())
                    .NotNull()
                    .ToArray()
                };
            }
            else
            {
                return null;
            }
        }

        public bool Equals(FileVersion other)
        {
            var thisNodeEquals = Name.Equals(other.Name)
                && LastWriteTimeUtc.Equals(other.LastWriteTimeUtc)
                && Length.Equals(other.Length);

            if (!thisNodeEquals)
            {
                return false;
            }

            return Childs.SequenceEqual(other.Childs);
        }

        public override bool Equals(object obj)
        {
            return obj switch
            {
                FileVersion f => Equals(f),
                _ => false
            };
        }

        public override int GetHashCode()
        {
            return LastWriteTimeUtc.GetHashCode() + 23 * Name.GetHashCode();
        }

        public bool IsNewer(FileVersion current)
        {
            return MinLastWriteTime > current.MaxLastWriteTime;
        }

        DateTime MinLastWriteTime => new[] { LastWriteTimeUtc }.Concat(Childs.Select(_ => _.MinLastWriteTime)).Min();

        DateTime MaxLastWriteTime => new[] { LastWriteTimeUtc }.Concat(Childs.Select(_ => _.MaxLastWriteTime)).Max();
    }
}
