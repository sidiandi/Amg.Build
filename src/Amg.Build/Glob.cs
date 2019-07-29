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
            var excludeFunc = new Func<string, bool>((string path) =>
            {
                var name = path.FileName();
                return exclude.Any(_ => _.Equals(name, System.StringComparison.OrdinalIgnoreCase));
            });

            var enumerable = include.SelectMany(i =>
            {
                return Find(root, i.SplitDirectories(), excludeFunc);
            });

            return enumerable.GetEnumerator();
        }

        static IEnumerable<string> Find(string root, string[] glob, Func<string, bool> exclude)
        {
            if (root.IsFile())
            {
                return new[] { root };
            }

            if (glob == null || glob.Length == 0)
            {
                return Enumerable.Empty<string>();
            }

            var first = glob[0];
            var rest = glob.Skip(1).ToArray();
            if (IsWildCard(first))
            {
                var childs = Directory.EnumerateFileSystemEntries(root, first)
                    .Where(_ => !exclude(_)).Select(_ => root.Combine(_));

                if (IsSkipAnyNumberOfDirectories(first))
                {
                    return childs.SelectMany(c => Find(c, rest, exclude))
                        .Concat(childs.SelectMany(c => Find(c, glob, exclude)));
                }
                else
                {
                    return childs.SelectMany(c => Find(c, rest, exclude));
                }
            }
            else
            {
                return Find(root.Combine(first), rest, exclude);
            }
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
    }
}