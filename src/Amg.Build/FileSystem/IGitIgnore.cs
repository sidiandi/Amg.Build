namespace Amg.FileSystem
{
    public interface IGitIgnore
    {
        bool IsIgnored(string path);
    }
}