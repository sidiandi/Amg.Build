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
        /// <param name="args"></param>
        /// <returns></returns>
        Task<IToolResult> Run(params string[] args);
    }
}