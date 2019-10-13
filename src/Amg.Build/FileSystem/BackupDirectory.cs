using System;
using System.Threading.Tasks;

namespace Amg.Build.FileSystem
{

    sealed class BackupDirectory : IBackup
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly string directory;
        private readonly string backupDirectory;

        public BackupDirectory(string directory)
        : this(directory, System.IO.Path.GetTempPath())
        {
        }

        public BackupDirectory(string directory, string backupRoot)
        {
            this.directory = directory;
            this.backupDirectory = backupRoot.Combine(directory.FileName() + "." + DateTime.UtcNow.ToFileName())
                .GetNotExisting()
                .EnsureDirectoryExists();
        }

        public void Dispose()
        {
            // no activities are required to close a backup to a directory
        }

        /// <summary>
        /// Moves path to backup directory if it exists 
        /// </summary>
        /// <param name="path"></param>
        /// <returns>backup location</returns>
        public async Task<string> Move(string path)
        {
            var dest = backupDirectory.Combine(path.RelativeTo(directory))
                .EnsureParentDirectoryExists();

            if (path.Exists())
            {
                Logger.Information("Backup {file} at {backup}", path, dest);
                return await path.Move(dest);
            }
            else
            {
                return dest;
            }
        }
    }
}
