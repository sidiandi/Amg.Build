namespace Amg.Build
{
    /// <summary>
    /// Frequently used tools
    /// </summary>
    public class Tools
    {
        /// <summary>
        /// cmd.exe /c
        /// </summary>
        public static ITool Cmd => new Tool("cmd.exe").WithArguments("/c");
    }
}