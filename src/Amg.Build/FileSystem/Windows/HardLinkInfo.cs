using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Amg.FileSystem.Windows;

class HardLinkInfo : IHardLinkInfo
{
    public static HardLinkInfo Get(string path)
    {
        return new HardLinkInfo(path);
    }

    HardLinkInfo(string path)
    {
        this.path = path;
    }

    readonly string path;

    NativeMethods.BY_HANDLE_FILE_INFORMATION GetByHandleFileInformation()
    {
        using (var handle = NativeMethods.CreateFile(
            path,
            System.IO.FileAccess.Read,
            System.IO.FileShare.Read, IntPtr.Zero,
            System.IO.FileMode.Open, System.IO.FileAttributes.Normal,
            IntPtr.Zero))
        {
            if (handle.IsInvalid)
            {
                throw new Win32Exception(path);
            }

            NativeMethods.GetFileInformationByHandle(handle, out var fileInfo).CheckApiCall(path);
            return fileInfo;
        }
    }

    public int FileLinkCount
    {
        get
        {
            return (int)GetByHandleFileInformation().NumberOfLinks;
        }
    }

    public long FileIndex
    {
        get
        {
            var fileInfo = GetByHandleFileInformation();
            return (long)(((ulong)fileInfo.FileIndexHigh << 32) + (ulong)fileInfo.FileIndexLow);
        }
    }

    static string[] GetFileSiblingHardLinks(string filepath)
    {
        List<string> result = new List<string>();
        uint stringLength = 256;
        StringBuilder sb = new StringBuilder(256);
        NativeMethods.GetVolumePathName(filepath, sb, stringLength);
        string volume = sb.ToString();
        sb.Length = 0; stringLength = 256;
        IntPtr findHandle = NativeMethods.FindFirstFileNameW(filepath, 0, ref stringLength, sb);
        if (findHandle.ToInt64() != -1)
        {
            do
            {
                StringBuilder pathSb = new StringBuilder(volume, 256);
                NativeMethods.PathAppend(pathSb, sb.ToString());
                result.Add(pathSb.ToString());
                sb.Length = 0; stringLength = 256;
            } while (NativeMethods.FindNextFileNameW(findHandle, ref stringLength, sb));
            NativeMethods.FindClose(findHandle);
            return result.ToArray();
        }
        return new string[] { };
    }

    public IEnumerable<string> HardLinks => GetFileSiblingHardLinks(path);
}
