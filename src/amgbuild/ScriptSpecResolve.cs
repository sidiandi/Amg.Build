using Amg.Build;
using Amg.FileSystem;
using System;
using System.Linq;

namespace amgbuild
{
    static class ScriptSpecResolve
    {
        public static SourceCodeLayout Resolve(string? spec, string baseDir)
        {
            if (spec == null)
            {
                return GetDefaultSourceCodeLayout(baseDir);
            }

            if (!spec.HasExtension(SourceCodeLayout.CmdExtension))
            {
                spec = spec + SourceCodeLayout.CmdExtension;
            }

            if (Is(spec))
            {
                return new SourceCodeLayout(spec);
            }

            throw new ArgumentException($"No script {spec} found in {baseDir}");
        }

        static SourceCodeLayout GetDefaultSourceCodeLayout(string dir)
        {
            dir = dir.Absolute();
            var name = dir.FileNameWithoutExtension();
            var fromCmdFile = dir.Glob("*.cmd").EnumerateFiles()
                .Where(_ => Is(_))
                .Select(_ => new SourceCodeLayout(_))
                .FirstOrDefault();

            if (fromCmdFile != null)
            {
                return fromCmdFile;
            }

            var parent = dir.ParentOrNull();
            if (parent is {})
            {
                var cmdForDir = parent.Combine(name + SourceCodeLayout.CmdExtension);
                if (Is(cmdForDir))
                {
                    return new SourceCodeLayout(cmdForDir);
                }
                return GetDefaultSourceCodeLayout(parent);
            }

            throw new ArgumentException($"No script found in {dir}");
        }

        static bool Is(string cmdFile)
        {
            var name = cmdFile.FileNameWithoutExtension();
            var dir = cmdFile.Parent().Combine(name);
            var csProj = dir.Combine(name + ".csproj");
            return cmdFile.IsFile()
                && dir.IsDirectory()
                && csProj.IsFile();
        }
    }
}
