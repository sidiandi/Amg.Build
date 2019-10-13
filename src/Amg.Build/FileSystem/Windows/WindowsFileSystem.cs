using System;
using System.Threading;

namespace Amg.FileSystem.Windows
{
    /// <summary>
    /// Operations that access the Windows file system
    /// </summary>
    internal class FileSystem : IFileSystem
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

        public static FileSystem Current
        {
            get
            {
                if (current == null)
                {
                    current = new FileSystem();
                }
                return current;
            }
        }

        static FileSystem? current = null;

        /// <summary>
        /// Create a hardlink at fileName that points to existingFileName
        /// </summary>
        /// <param name="fileName">Path of the newly created link</param>
        /// <param name="existingFileName">Path of the existing file to which the link will point</param>
        public void CreateHardLink(string fileName, string existingFileName)
        {
            NativeMethods.CreateHardLink(fileName, existingFileName, IntPtr.Zero)
                .CheckApiCall($"Cannot create hard link: {fileName} -> {existingFileName}");
        }

        public IHardLinkInfo GetHardLinkInfo(string path)
        {
            return HardLinkInfo.Get(path);
        }

        const int ERROR_ALREADY_EXISTS = 183;
        const int ERROR_PATH_NOT_FOUND = 3;

        public void CopyFile(
            string existingFileName,
            string newFileName,
            IProgress<CopyFileProgress>? progress = null,
            CancellationToken cancellationToken = new CancellationToken(),
            CopyFileOptions? options = null)
        {
            Int32 pbCancel = 0;

            if (options == null)
            {
                options = new CopyFileOptions();
            }

            var begin = DateTime.UtcNow;

            var progressCallback = progress != null 
                ? new NativeMethods.CopyProgressRoutine(
                (
                    long TotalFileSize,
                    long TotalBytesTransferred,
                    long StreamSize,
                    long StreamBytesTransferred,
                    uint dwStreamNumber,
                    NativeMethods.CopyProgressCallbackReason dwCallbackReason,
                    IntPtr hSourceFile,
                    IntPtr hDestinationFile,
                    IntPtr lpData
                ) =>
                {
                    progress.Report(new CopyFileProgress(
                        begin,
                        TotalFileSize,
                        TotalBytesTransferred,
                        StreamSize,
                        StreamBytesTransferred,
                        (int) dwStreamNumber));

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return NativeMethods.CopyProgressResult.PROGRESS_CANCEL;
                    }
                    else
                    {
                        return NativeMethods.CopyProgressResult.PROGRESS_CONTINUE;
                    }
                }) 
                : null;

            NativeMethods.CopyFileEx(
                existingFileName,
                newFileName,
                progressCallback,
                IntPtr.Zero,
                ref pbCancel,
                (options.AllowDecryptedDestination ? NativeMethods.CopyFileFlags.COPY_FILE_ALLOW_DECRYPTED_DESTINATION : 0) |
                (options.CopySymlink ? NativeMethods.CopyFileFlags.COPY_FILE_COPY_SYMLINK : 0) |
                (options.FailIfExists ? NativeMethods.CopyFileFlags.COPY_FILE_FAIL_IF_EXISTS : 0) |
                (options.NoBuffering ? NativeMethods.CopyFileFlags.COPY_FILE_NO_BUFFERING : 0) |
                (options.OpenSourceForWrite ? NativeMethods.CopyFileFlags.COPY_FILE_OPEN_SOURCE_FOR_WRITE : 0) |
                (options.Restartable ? NativeMethods.CopyFileFlags.COPY_FILE_RESTARTABLE : 0))
                .CheckApiCall(String.Format("copy from {0} to {1}", existingFileName, newFileName));
        }
    }
}
