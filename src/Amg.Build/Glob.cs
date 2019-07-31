using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Amg.Build
{
    /// <summary>
    /// Search files
    /// </summary>
    public class Glob : IEnumerable<string>
    {
        string[] include = new string[] { };
        Func<FileSystemInfo, bool>[] exclude = new Func<FileSystemInfo, bool>[] { };
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
        /// <param name="pathWithWildcards"></param>
        /// <returns></returns>
        public Glob Include(string pathWithWildcards)
        {
            var g = Copy();
            g.include = g.include.Concat(new[] { pathWithWildcards }).ToArray();
            return g;
        }

        /// <summary>
        /// Exclude a file name pattern from directory traversal
        /// </summary>
        /// <param name="wildcardPattern"></param>
        /// <returns></returns>
        public Glob Exclude(string wildcardPattern)
        {
            var g = Copy();
            g.exclude = g.exclude.Concat(ExcludeFuncFromWildcard(wildcardPattern)).ToArray();
            return g;
        }

        /// <summary>
        /// Exclude a file system object from directory traversal if it fulfills the condition
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        public Glob Exclude(Func<FileSystemInfo, bool> condition)
        {
            var g = Copy();
            g.exclude = g.exclude.Concat(condition).ToArray();
            return g;
        }

        /// <summary>
        /// Turns a wildcard (*,?) pattern as used by DirectoryInfo.EnumerateFileSystemInfos into a Regex
        /// </summary>
        /// Supports wildcard characters * and ?. Case-insensitive.
        /// <param name="wildcardPattern"></param>
        /// <returns></returns>
        public static Regex RegexFromWildcard(string wildcardPattern)
        {
            var patternString = string.Concat(
                wildcardPattern.Select(c =>
                {
                    switch (c)
                    {
                        case '?':
                            return ".";
                        case '*':
                            return ".*";
                        default:
                            return Regex.Escape(new string(c, 1));
                    }
                }));

            patternString = "^" + patternString + "$";

            return new Regex(patternString, RegexOptions.IgnoreCase);
        }

        internal static Func<FileSystemInfo, bool> ExcludeFuncFromWildcard(string wildcardPattern)
        {
            var re = RegexFromWildcard(wildcardPattern);
            return new Func<FileSystemInfo, bool>(fsi =>
            {
                return re.IsMatch(fsi.Name);
            });
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
                exclude.Any(_ => _(i)));

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