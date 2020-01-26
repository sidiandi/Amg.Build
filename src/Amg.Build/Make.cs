using Amg.Extensions;
using Amg.FileSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Amg.Extensions.Yaml;

namespace Amg.Build
{
    public static class Make
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

        public static async Task<IEnumerable<string>> Rule(
            IEnumerable<string> targets,
            IEnumerable<string> prerequisites,
            Func<IEnumerable<string>, IEnumerable<string>, Task> recipe)
        {
            var versionFile = typeof(Make).GetProgramDataDirectory().Combine(Md5Checksum(targets) + ".yml");
            var lastVersion = await TryReadFile<FileVersion[]>(versionFile);
            var currentVersion = await FileVersion.Get(prerequisites);

            if (lastVersion is null || !lastVersion.SequenceEqual(currentVersion))
            {
                await recipe(targets, prerequisites);
                await WriteFile(versionFile, currentVersion);
            }
            else
            {
                Logger.Information("targets {@targets} are up to date with respect to their prerequisites {@prerequisites}.", targets, prerequisites);
            }

            return targets;
        }

        public static async Task<string> Rule(
            string target,
            string prerequisite,
            Func<string, string, Task> recipe)
        {
            var o = new[] { target };
            var i = new[] { prerequisite };
            return (await Rule(o, i, async (_o, _i) => await recipe(target, prerequisite)))
                .First();
        }
    }
}
