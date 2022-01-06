using Amg.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Amg.FileSystem;

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

    internal static Regex RegexFromWildcard(string wildcardPattern)
    {
        return new Regex("^" + WildcardToRegexPattern(wildcardPattern) + "$", RegexOptions.IgnoreCase);
    }

    static string WildcardToRegexPattern(string wildcardPattern)
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
                    case '/':
                        return Regex.Escape(new string(Path.DirectorySeparatorChar, 1));
                    default:
                        return Regex.Escape(new string(c, 1));
                }
            }));
        return patternString;
    }

    /// <summary>
    /// Turns a wildcard (*,?) pattern as used by DirectoryInfo.EnumerateFileSystemInfos into a Regex
    /// </summary>
    /// Supports wildcard characters * and ?. Case-insensitive.
    /// <param name="wildcardPattern"></param>
    /// <returns></returns>
    internal static Regex SubstringRegexFromWildcard(string wildcardPattern)
    {
        return new Regex(WildcardToRegexPattern(wildcardPattern), RegexOptions.IgnoreCase);
    }

    internal Func<FileSystemInfo, bool> ExcludeFuncFromWildcard(string wildcardPattern)
    {
        var f = wildcardPattern.SplitDirectories()
            .Select(RegexFromWildcard)
            .Select(_ => new Func<string, bool>(s => _.IsMatch(s)))
            .ToArray();

        return new Func<FileSystemInfo, bool>(fsi =>
        {
            var d = fsi.FullName.Substring(root.Length).SplitDirectories();
            return Match(d, f);
        });
    }

    internal static bool Match<T>(IEnumerable<T> parts, IEnumerable<Func<T, bool>> predicates)
    {
        var v = parts.ToArray();
        var p = predicates.ToArray();
        for (int i = 0; i <= v.Length - p.Length; ++i)
        {
            int pi = 0;
            for (; pi < p.Length; ++pi)
            {
                if (!p[pi](v[i + pi]))
                {
                    break;
                }
            }
            if (pi == p.Length)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary />
    public IEnumerator<string> GetEnumerator()
    {
        var enumerable = EnumerateFileSystemInfos()
            .Select(_ => _.FullName);

        return enumerable.GetEnumerator();
    }

    static IEnumerable<FileSystemInfo> Find(FileSystemInfo fileSystemInfo, string[] glob, Func<FileSystemInfo, bool> exclude)
    {
        if (glob == null || glob.Length == 0)
        {
            return new[] { fileSystemInfo };
        }

        if (!(fileSystemInfo is DirectoryInfo root))
        {
            return Enumerable.Empty<FileSystemInfo>();
        }

        // do not follow junction points
        if ((root.Attributes & FileAttributes.ReparsePoint) != 0)
        {
            return Enumerable.Empty<FileSystemInfo>();
        }

        var first = glob[0];
        var rest = glob.Skip(1).ToArray();
        var leaf = rest.Length == 0;

        if (IsSkipAnyNumberOfDirectories(first))
        {
            return (leaf
                ? Find(root, new[] { "*" }, exclude)
                : Find(root, rest, exclude))
                .Concat(root.EnumerateDirectories()
                .Where(_ => !exclude(_))
                .SelectMany(_ => Find(_, glob, exclude)));
        }
        else
        {
            return root.EnumerateFileSystemInfos(first)
                .Where(_ => !exclude(_))
                .SelectMany(c =>
                {
                    if (leaf)
                    {
                        return new[] { c };
                    }
                    else if (c is DirectoryInfo d)
                    {
                        return Find(d, rest, exclude);
                    }
                    else
                    {
                        return Enumerable.Empty<FileSystemInfo>();
                    }
                });
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
        return GetEnumerator();
    }

    /// <summary>
    /// Enumerate as FileSystemInfo sequence
    /// </summary>
    /// <returns></returns>
    public IEnumerable<FileSystemInfo> EnumerateFileSystemInfos()
    {
        var excludeFunc = new Func<FileSystemInfo, bool>((FileSystemInfo i) =>
            exclude.Any(_ => _(i)));

        var rootInfo = root.Info();
        return rootInfo == null
            ? Enumerable.Empty<FileSystemInfo>()
            : include.SelectMany(i => Find(rootInfo, i.SplitDirectories(), excludeFunc));
    }
}
