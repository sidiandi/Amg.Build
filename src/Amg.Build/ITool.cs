using System.Threading.Tasks;

namespace Amg.Build
{
    /// <summary>
    /// Command line tool wrapper
    /// </summary>
    public interface ITool
    {
        /// <summary>
        /// Runs the command line tool
        /// </summary>
        /// <param name="args">Command line argument. One string per argument. Will be quoted automatically (i.e. when a argument contains whitespace)</param>
        /// <returns></returns>
        Task<IToolResult> Run(params string[] args);
    }
}