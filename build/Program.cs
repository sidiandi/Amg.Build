using System.Threading.Tasks;

namespace build
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var targets = new BuildTargets();
            await targets.Run(args);
        }
    }
}
