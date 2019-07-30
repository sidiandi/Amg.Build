using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Amg.Build
{
    /// <summary>
    /// Search files
    /// </summary>
    public class Glob : IEnumerable<string>
    {
        string[] include = new string[] { };
        string[] exclude = new string[] { };
        private readonly string root;

        Glob Copy()
        {
            return (Glob)MemberwiseClone();
        }

        /// <summary />
        public Glob(string root)
        {
            this.root = root;
        }

        /// <summary>
        /// Include path in file search
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public Glob Include(string path)
        {
            var g = Copy();
            g.include = g.include.Concat(new[] { path }).ToArray();
            return g;
        }

        /// <summary>
        /// Exclude a file name pattern from directory traversal
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public Glob Exclude(string pattern)
        {
            var g = Copy();
            g.exclude = g.exclude.Concat(new[] { pattern }).ToArray();
            return g;
        }

        /// <summary />
        public IEnumerator<string> GetEnumerator()
        {
            var enumerable = EnumerateFileSystemInfos()
                .Select(_ => _.FullName);

            return enumerable.GetEnumerator();
        }

        static IEnumerable<FileSystemInfo> Find(DirectoryInfo root, string[] glob, Func<FileSystemInfo, bool> exclude)
        {
            if (glob == null || glob.Length == 0)
            {
                return Enumerable.Empty<FileSystemInfo>();
            }

            var first = glob[0];
            var rest = glob.Skip(1).ToArray();
            if (IsSkipAnyNumberOfDirectories(first))
            {
                return Find(root.EnumerateFileSystemInfos(), glob, exclude);
            }
            else
            {
                return Find(root.EnumerateFileSystemInfos(first), rest, exclude);
            }
        }

        static IEnumerable<FileSystemInfo> Find(
            IEnumerable<FileSystemInfo> fileSystemInfos,
            string[] glob,
            Func<FileSystemInfo, bool> exclude)
        {
            return fileSystemInfos
                .Where(_ => !exclude(_))
                .SelectMany(c =>
                {
                    if (c is FileInfo f)
                    {
                        return (IEnumerable<FileSystemInfo>)new[] { c };
                    }
                    else if (c is DirectoryInfo d)
                    {
                        return new[] { c }.Concat(Find(d, glob, exclude));
                    }
                    else
                    {
                        return Enumerable.Empty<FileSystemInfo>();
                    }
                });
        }

        static bool IsSkipAnyNumberOfDirectories(string dirname)
        {
            return dirname.Equals("**");
        }

        static bool IsWildCard(string dirname)
        {
            return dirname.Contains('*');
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Enumerate as FileSystemInfo sequence
        /// </summary>
        /// <returns></returns>
        public IEnumerable<FileSystemInfo> EnumerateFileSystemInfos()
        {
            var excludeFunc = new Func<FileSystemInfo, bool>((FileSystemInfo i) =>
            {
                var name = i.Name;
                return exclude.Any(_ => _.Equals(name, System.StringComparison.OrdinalIgnoreCase));
            });

            var r = root.GetFileSystemInfo();

            return include.SelectMany(i =>
            {
                return Find(new[] { r }, i.SplitDirectories(), excludeFunc);
            });
        }

        /// <summary>
        /// Enumerate files only, not directories
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> EnumerateFiles()
        {
            return EnumerateFileSystemInfos().Where(_ => _ is FileInfo).Select(_ => _.FullName);
        }
    }
}