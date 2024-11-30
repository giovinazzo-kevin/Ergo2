namespace Ergo.IO;

public class ModuleLocator
{
    protected const string EXT = "ergo";
    protected const string FILTER = $"*.{EXT}";
    public readonly List<string> SearchPaths;
    public readonly ModuleLocatorIndex Index;

    public static readonly ModuleLocator Default = new([@".\ergo"]);

    public ModuleLocator(params List<string> paths)
    {
        SearchPaths = paths;
        Index = new(Scan);
    }

    protected virtual IEnumerable<FileInfo> Scan => SearchPaths
        .Where(Directory.Exists)
        .SelectMany(path => Directory.EnumerateFiles(path, FILTER, SearchOption.AllDirectories)
            .Select(file => new FileInfo(file)));
}

