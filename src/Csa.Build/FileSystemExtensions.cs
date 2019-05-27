using System;
using System.IO;
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

        public static string EnsureFileNotExists(this string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            return path;
        }

        public static string EnsureDirectoryIsEmpty(this string dir)
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
            return dir.EnsureDirectoryExists();
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