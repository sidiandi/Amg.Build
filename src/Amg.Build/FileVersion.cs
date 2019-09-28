using System;
using System.IO;
using System.Linq;

namespace Amg.Build
{
    class FileVersion : IEquatable<FileVersion>
    {
        public string name;
        public DateTime lastWriteTimeUtc;
        public long length;
        public FileVersion[] childs;

        public static FileVersion Get(string path)
        {
            if (path.IsFile())
            {
                var info = new FileInfo(path);
                return new FileVersion
                {
                    name = path.FileName(),
                    lastWriteTimeUtc = info.LastWriteTimeUtc,
                    length = info.Length,
                    childs = new FileVersion[] { }
                };
            }
            else if (path.IsDirectory())
            {
                var info = new DirectoryInfo(path);
                return new FileVersion
                {
                    name = path.FileName(),
                    lastWriteTimeUtc = info.LastWriteTimeUtc,
                    length = 0,
                    childs = path.EnumerateFileSystemEntries().Select(Get).ToArray()
                };
            }
            else
            {
                return null;
            }
        }

        public bool Equals(FileVersion other)
        {
            return name.Equals(other.name)
                && lastWriteTimeUtc.Equals(other.lastWriteTimeUtc)
                && childs.SequenceEqual(other.childs);
        }
    }
}
