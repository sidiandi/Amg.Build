using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Amg.FileSystem;

/// <summary>
/// Convenience extensions for Glob
/// </summary>
public static class GlobExtensions
{
    /// <summary>
    /// Enumerate files
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<FileInfo> EnumerateFileInfos(this Glob glob)
    {
        return glob.EnumerateFileSystemInfos().OfType<FileInfo>();
    }

    /// <summary>
    /// Enumerate files only, not directories
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<string> EnumerateFiles(this Glob glob)
    {
        return glob.EnumerateFileInfos().Select(_ => _.FullName);
    }
}
