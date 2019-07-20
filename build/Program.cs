using System.Threading.Tasks;

namespace build
{
    class Program
    {
        static int Main(string[] args) => Csa.Build.Targets.Run<BuildTargets>(args);
    }
}
