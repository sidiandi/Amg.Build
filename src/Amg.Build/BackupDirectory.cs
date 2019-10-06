using System;
using System.Threading.Tasks;

namespace Amg.Build
{
    public class BackupDirectory
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

        public async Task<string> Move(string file)
        {
            var dest = backupDirectory.Combine(file.RelativeTo(directory))
                .EnsureParentDirectoryExists();
            Logger.Information("Backup {file} at {backup}", file, dest);
            return await file.Move(dest);
        }
    }
}
