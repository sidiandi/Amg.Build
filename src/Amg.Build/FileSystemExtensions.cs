using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Amg.Build
{
    public static class FileSystemExtensions
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static string EnsureParentDirectoryExists(this string path)
        {
            path.Parent().EnsureDirectoryExists();
            return path;
        }

        /// <summary>
        /// Ensure that the file given by path does not exist. Deletes also read-only files
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string EnsureFileNotExists(this string path)
        {
            if (File.Exists(path))
            {
                new FileInfo(path).DeleteReadOnly();
            }
            return path;
        }

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

        public static string Absolute(this string path)
        {
            return Path.GetFullPath(path);
        }

        public static bool HasExtension(this string path, params string[] extensionsWithDots)
        {
            var e = path.Extension();
            return extensionsWithDots.Any(_ => _.Equals(e, StringComparison.OrdinalIgnoreCase));
        }

        public static string Extension(this string path)
        {
            return Path.GetExtension(path);
        }

        public static string FileName(this string path)
        {
            return Path.GetFileName(path);
        }

        public static string FileNameWithoutExtension(this string path)
        {
            return Path.GetFileNameWithoutExtension(path);
        }

        public static string EnsureDirectoryExists(this string dir)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return dir;
        }

        public static string Parent(this string path)
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

        public static bool IsOutOfDate(this string outputFile, IEnumerable<string> inputFiles)
        {
            return new[] { outputFile }.IsOutOfDate(inputFiles);
        }

        public static bool IsOutOfDate(this IEnumerable<string> outputFiles, IEnumerable<string> inputFiles)
        {
            outputFiles = outputFiles.ToList();
            var outputModified = outputFiles.LastWriteTimeUtc();
            var inputModified = inputFiles.Except(outputFiles).LastWriteTimeUtc();
            return outputModified < inputModified;
        }

        public static DateTime LastWriteTimeUtc(this IEnumerable<string> paths)
        {
            var m = paths.Select(_ => new { Path = _, LastWrite = _.LastWriteTimeUtc() })
                .MaxElement(_ => _.LastWrite);

            Logger.Information("{0}", m);
            return m.LastWrite;
        }

        public static DateTime LastWriteTimeUtc(this string path)
        {
            return path.IsFile()
                ? new FileInfo(path).LastWriteTimeUtc
                : DateTime.MinValue;

        }

        public static bool IsFile(this string path)
        {
            return File.Exists(path);
        }

        public static bool IsDirectory(this string path)
        {
            return Directory.Exists(path);
        }

        public static IEnumerable<string> Glob(this string path)
        {
            if (path.IsDirectory())
            {
                return Directory.EnumerateFileSystemEntries(path, "*", SearchOption.AllDirectories)
                    .Select(_ => _.Absolute());
            }
            else if (path.IsFile())
            {
                return new[] { path };
            }
            else
            {
                return Enumerable.Empty<string>();
            }
        }
    }
}