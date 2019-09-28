using System;
using System.IO;
using System.Linq;

namespace Amg.Build
{
    class FileVersion : IEquatable<FileVersion>
    {
        public string Name { get; private set; }
        public DateTime LastWriteTimeUtc { get; private set; }
        public long Length { get; private set; }
        public FileVersion[] Childs { get; private set; }

        public static FileVersion Get(string path)
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
                var info = new DirectoryInfo(path);
                return new FileVersion
                {
                    Name = path.FileName(),
                    LastWriteTimeUtc = info.LastWriteTimeUtc,
                    Length = 0,
                    Childs = path.EnumerateFileSystemEntries()
                    .Where(_ => !(_.FileName().Equals("bin") || _.FileName().Equals("obj")))
                    .Select(Get).ToArray()
                };
            }
            else
            {
                return null;
            }
        }

        public bool Equals(FileVersion other)
        {
            return Name.Equals(other.Name)
                && LastWriteTimeUtc.Equals(other.LastWriteTimeUtc)
                && Childs.SequenceEqual(other.Childs);
        }

        public bool IsNewer(FileVersion current)
        {
            return MinLastWriteTime > current.MaxLastWriteTime;
        }

        DateTime MinLastWriteTime => new[] { LastWriteTimeUtc }.Concat(Childs.Select(_ => _.MinLastWriteTime)).Min();

        DateTime MaxLastWriteTime => new[] { LastWriteTimeUtc }.Concat(Childs.Select(_ => _.MaxLastWriteTime)).Max();
    }
}
