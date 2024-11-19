using Ergo.IO;

namespace Ergo.Compiler.Emission;

public class KnowledgeBaseLocator
{
    public const string EXT = ".kb";
    public const string FILTER = $"*{EXT}";
    public readonly List<string> SearchPaths;
    public readonly KnowledgeBaseLocatorIndex Index;

    public KnowledgeBaseLocator(params List<string> paths)
    {
        SearchPaths = paths;
        Index = new(Scan);
    }

    protected virtual IEnumerable<FileInfo> Scan => SearchPaths
        .Where(Directory.Exists)
        .SelectMany(path => Directory.EnumerateFiles(path, FILTER, SearchOption.AllDirectories)
            .Select(file => new FileInfo(file)));
}
