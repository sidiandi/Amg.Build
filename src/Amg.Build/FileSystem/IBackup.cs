using System.Threading.Tasks;

namespace Amg.Build.FileSystem
{
    public interface IBackup : System.IDisposable
    {
        Task<string> Move(string path);
    }
}
