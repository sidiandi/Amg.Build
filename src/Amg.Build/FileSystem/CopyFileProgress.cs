using System;

namespace Amg.FileSystem;

internal class CopyFileProgress
{
    public CopyFileProgress(
        DateTime Begin,
        long TotalFileSize,
        long TotalBytesTransferred,
        long StreamSize,
        long StreamBytesTransferred,
        int StreamNumber)
    {
        this.Begin = Begin;
        this.TotalFileSize = TotalFileSize;
        this.TotalBytesTransferred = TotalBytesTransferred;
        this.StreamSize = TotalBytesTransferred;
        this.StreamBytesTransferred = StreamBytesTransferred;
        this.StreamNumber = StreamNumber;
    }

    public DateTime Begin { get; private set; }
    public long TotalFileSize { get; private set; }
    public long TotalBytesTransferred { get; private set; }
    public long StreamSize { get; private set; }
    public long StreamBytesTransferred { get; private set; }
    public int StreamNumber { get; private set; }
}
