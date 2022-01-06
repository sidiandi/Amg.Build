using System.Threading.Tasks;

namespace Amg.FileSystem;

public interface IBackup : System.IDisposable
{
    Task<string> Move(string path);
}
