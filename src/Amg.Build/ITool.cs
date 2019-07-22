using System.Threading.Tasks;

namespace Amg.Build
{
    public interface ITool
    {
        Task<IToolResult> Run(params string[] args);
    }
}