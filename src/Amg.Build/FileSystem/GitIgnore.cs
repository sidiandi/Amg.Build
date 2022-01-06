
namespace Amg.FileSystem;

public static class GitIgnore
{
    public static IGitIgnore Create()
    {
        return new GitIgnoreImpl();
    }
}
