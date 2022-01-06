using Amg.Extensions;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Amg.FileSystem;

internal class GitIgnoreImpl : IGitIgnore
{
    public bool IsIgnored(string path)
    {
        var repo = GetRepository(path);
        if (repo == null)
        {
            return false;
        }

        var relativePath = GetRelativePath(path, repo);
        return repo.Ignore.IsPathIgnored(relativePath);
    }

    static string GetRelativePath(string path, IRepository repo)
    {
        return path.RelativeTo(repo.Info.Path.Parent())
            .SplitDirectories()
            .Join("/");
    }

    IRepository? GetRepository(string path)
    {
        var r = repositories.FirstOrDefault(r => path.IsDescendantOrSelf(r.Info.Path));
        if (r == null)
        {
            var root = FindRepositoryRoot(path);
            if (root != null)
            {
                r = new Repository(root);
                repositories.Add(r);
            }
        }
        return r;
    }

    static string? FindRepositoryRoot(string p)
    {
        return p.Up().FirstOrDefault(r => r.Combine(".git").IsDirectory());
    }

    readonly IList<IRepository> repositories = new List<IRepository>();

    static bool IsComment(string line)
    {
        return String.IsNullOrEmpty(line) || line.StartsWith("#");
    }
}
