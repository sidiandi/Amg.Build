using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Csa.Build
{
    public static class FileSystemExtensions
    {
        public static string EnsureParentDirectoryExists(this string path)
        {
            path.GetParentDirectory().EnsureDirectoryExists();
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

        public static string CatDir(this string directory, params string[] pathElements)
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

        public static string GetFullPath(this string path)
        {
            return Path.GetFullPath(path);
        }

        public static string EnsureDirectoryExists(this string dir)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return dir;
        }

        public static string GetParentDirectory(this string path)
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
    }
}