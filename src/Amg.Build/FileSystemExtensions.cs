using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Amg.Build
{
    /// <summary>
    /// Extensions to work with file system objects.
    /// </summary>
    /// These extensions of `string` allow fluent handling of file and directory paths.
    /// For examples, see Amg.Build.Tests/FileSystemExtensionsTests.cs
    public static class FileSystemExtensions
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Creates the parent directory of path is necessary. 
        /// </summary>
        /// <param name="path"></param>
        /// <returns>path</returns>
        public static string EnsureParentDirectoryExists(this string path)
        {
            path.Parent().EnsureDirectoryExists();
            return path;
        }

        /// <summary>
        /// Ensure that the file given by path does not exist. Deletes also read-only files
        /// </summary>
        /// <param name="path"></param>
        /// <returns>path</returns>
        public static string EnsureFileNotExists(this string path)
        {
            if (File.Exists(path))
            {
                new FileInfo(path).DeleteReadOnly();
            }
            return path;
        }

        /// <summary>
        /// Appends path elements to path.
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="pathElements"></param>
        /// <returns>directory with pathElements appended.</returns>
        public static string Combine(this string directory, params string[] pathElements)
        {
            return Path.Combine(new[] { directory }.Concat(pathElements).ToArray());
        }

        /// <summary>
        /// Ensures that dir exists and is empty. Deletes also read-only files
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static string EnsureDirectoryIsEmpty(this string dir)
        {
            if (Directory.Exists(dir))
            {
                foreach (var i in new DirectoryInfo(dir).EnumerateFileSystemInfos())
                {
                    i.DeleteReadOnly();
                }
            }
            return dir.EnsureDirectoryExists();
        }

        /// <summary>
        /// Deletes the directory tree fileSystemInfo even if it contains read-only elements.
        /// </summary>
        /// <param name="fileSystemInfo"></param>
        public static void DeleteReadOnly(this FileSystemInfo fileSystemInfo)
        {
            var directoryInfo = fileSystemInfo as DirectoryInfo;
            if (directoryInfo != null)
            {
                foreach (FileSystemInfo childInfo in directoryInfo.GetFileSystemInfos())
                {
                    childInfo.DeleteReadOnly();
                }
            }

            fileSystemInfo.Attributes = FileAttributes.Normal;
            fileSystemInfo.Delete();
        }

        /// <summary>
        /// Makes a relative path absolute.
        /// </summary>
        /// <param name="path">Relative or absolute path.</param>
        /// <returns>Absolute path.</returns>
        public static string Absolute(this string path)
        {
            return Path.GetFullPath(path);
        }

        /// <summary>
        /// Returns true if path has any of the extensions `extensionWithDots`. Ignores case.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="extensionsWithDots"></param>
        /// <returns>True, if path has one of the passed extensions, false otherwise.</returns>
        public static bool HasExtension(this string path, params string[] extensionsWithDots)
        {
            var e = path.Extension();
            return extensionsWithDots.Any(_ => _.Equals(e, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Returns the extension of the path, including the dot (.).
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string Extension(this string path)
        {
            return Path.GetExtension(path);
        }

        /// <summary>
        /// Returns the file name of the path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string FileName(this string path)
        {
            return Path.GetFileName(path);
        }

        /// <summary>
        /// File name without extension
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string FileNameWithoutExtension(this string path)
        {
            return Path.GetFileNameWithoutExtension(path);
        }

        /// <summary>
        /// Ensures that the directory dir exists
        /// </summary>
        /// <param name="dir"></param>
        /// <returns>dir</returns>
        public static string EnsureDirectoryExists(this string dir)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return dir;
        }

        /// <summary>
        /// Returns the parent directory or null, if no parent directory exists for path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string Parent(this string path)
        {
            return System.IO.Path.GetDirectoryName(path);
        }

        /// <summary>
        /// Reads all text in a file. Returns null on error.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>text</returns>
        public static async Task<string> ReadAllTextAsync(this string path)
        {
            try
            {
                using (var r = new StreamReader(path))
                {
                    return await r.ReadToEndAsync();
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Write all text to a file
        /// </summary>
        /// <param name="path"></param>
        /// <param name="text"></param>
        /// <returns>path</returns>
        public static async Task<string> WriteAllTextAsync(this string path, string text)
        {
            using (var r = new StreamWriter(path.EnsureParentDirectoryExists()))
            {
                await r.WriteAsync(text);
            }
            return path;
        }

        /// <summary>
        /// Writes text to the file `path`, but only if `path` does not already contain the desired text.
        /// </summary>
        /// Useful when writing config files to avoid to re-buildi everything when nothing in the config has actually changed.
        /// <param name="path"></param>
        /// <param name="text"></param>
        /// <returns>path</returns>
        public static async Task<string> WriteAllTextIfChangedAsync(this string path, string text)
        {
            var hasChanged = !path.IsFile() || !((await path.ReadAllTextAsync()).Equals(text));

            if (hasChanged)
            {
                await path.WriteAllTextAsync(text);
            }

            return path;
        }

        /// <summary>
        /// Convenience wrapper for a single outputFile. See <![CDATA[ IsOutOfDate(this IEnumerable<string> outputFiles, IEnumerable<string> inputFiles) ]]>
        /// </summary>
        /// <param name="outputFile"></param>
        /// <param name="inputFiles"></param>
        /// <returns></returns>
        public static bool IsOutOfDate(this string outputFile, IEnumerable<string> inputFiles)
        {
            return new[] { outputFile }.IsOutOfDate(inputFiles);
        }

        /// <summary>
        /// Returns true if outputFiles cannot have been built from inputFiles.
        /// </summary>
        /// <param name="outputFiles"></param>
        /// <param name="inputFiles"></param>
        /// <returns>True if outputFiles cannot have been built from inputFiles, false otherwise</returns>
        public static bool IsOutOfDate(this IEnumerable<string> outputFiles, IEnumerable<string> inputFiles)
        {
            outputFiles = outputFiles.ToList();
            var outputModified = outputFiles.LastWriteTimeUtc();
            var inputModified = inputFiles.Except(outputFiles).LastWriteTimeUtc();
            var isOutOfDate = outputModified < inputModified;
            if (Logger.IsEnabled(Serilog.Events.LogEventLevel.Debug))
            {
                Logger.Debug(@"IsOutOfDate: {isOutOfDate}

Input files:
{@inputFiles}

Output files:
{@outputFiles}",
                isOutOfDate,
                inputFiles.Select(_ => new { Path = _, Changed = _.LastWriteTimeUtc() }),
                outputFiles.Select(_ => new { Path = _, Changed = _.LastWriteTimeUtc() }));
            }
            return isOutOfDate;
        }

        /// <summary>
        /// Last time something was written to paths
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        public static DateTime LastWriteTimeUtc(this IEnumerable<string> paths)
        {
            var files = paths.Select(_ => new { Path = _, LastWrite = _.LastWriteTimeUtc() })
                .ToList();

            var m = files
                .MaxElement(_ => _.LastWrite)
                .SingleOrDefault();

            if (m == null)
            {
                return DateTime.MinValue;
            }

            return m.LastWrite;
        }

        /// <summary>
        /// Last time something was written to path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static DateTime LastWriteTimeUtc(this string path)
        {
            return path.IsFile()
                ? new FileInfo(path).LastWriteTimeUtc
                : DateTime.MinValue;

        }

        /// <summary>
        /// Returns true if path points to an existing file.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool IsFile(this string path)
        {
            return File.Exists(path);
        }

        /// <summary>
        /// Returns true if path points to an existing directory.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool IsDirectory(this string path)
        {
            return Directory.Exists(path);
        }

        /// <summary>
        /// Split path into directory parts
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string[] SplitDirectories(this string path)
        {
            path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            return path.Split(Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Start a glob
        /// </summary>
        /// <param name="path"></param>
        /// <param name="pattern">Glob pattern to include. If omitted, an empty glob is returned.</param>
        /// <returns></returns>
        public static Glob Glob(this string path, string pattern = null)
        {
            var glob = new Glob(path);
            if (pattern != null)
            {
                glob = glob.Include(pattern);
            }
            return glob;
        }

        /// <summary>
        /// Gets a FileInfo or DirectoryInfo or null if file system object does not exist.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static FileSystemInfo GetFileSystemInfo(this string path)
        {
            if (path.IsFile())
            {
                return new FileInfo(path);
            }
            else if (path.IsDirectory())
            {
                return new DirectoryInfo(path);
            }
            else
            {
                return null;
            }
        }
    }
}