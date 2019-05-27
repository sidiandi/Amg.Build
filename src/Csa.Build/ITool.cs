using System.Threading.Tasks;

namespace Csa.Build
{
    public interface ITool
    {
        Task<IToolResult> Run(params string[] args);
    }
}