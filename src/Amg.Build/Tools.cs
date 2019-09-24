namespace Amg.Build
{
    /// <summary>
    /// Frequently used tools
    /// </summary>
    public class Tools
    {
        /// <summary>
        /// Default tool settings
        /// </summary>
        public static ITool Default { get; set; } = new Tool();

        /// <summary>
        /// cmd.exe /c
        /// </summary>
        public static ITool Cmd => Default.WithFileName("cmd.exe").WithArguments("/c");
    }
}