using Ergo.Shared.Types;
using System;
using System.IO;

namespace Ergo.IO;

public class ModuleLocator
{
    protected const string EXT = "ergo";
    protected const string FILTER = $"*.{EXT}";
    public readonly List<string> SearchPaths;
    public readonly ModuleLocatorIndex Index;

    public ModuleLocator(params List<string> paths)
    {
        SearchPaths = paths;
        Index = new(Scan);
    }

    protected virtual IEnumerable<FileInfo> Scan => SearchPaths
        .SelectMany(path => Directory.EnumerateFiles(path, FILTER, SearchOption.AllDirectories)
            .Select(file => new FileInfo(file)));
}

