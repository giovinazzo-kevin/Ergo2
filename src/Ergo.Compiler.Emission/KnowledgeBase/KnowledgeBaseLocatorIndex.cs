namespace Ergo.Compiler.Emission;

public class KnowledgeBaseLocatorIndex
{
    private readonly IEnumerable<FileInfo> Source;
    private ILookup<string, FileInfo> _lookup = null!;

    public KnowledgeBaseLocatorIndex(IEnumerable<FileInfo> scan)
    {
        Source = scan;
        Update();
    }

    public void Update()
    {
        _lookup = Source
            .ToLookup(x => Path.GetFileNameWithoutExtension(x.Name));
    }

    public IEnumerable<FileInfo> Find(string KnowledgeBase)
    {
        if (!_lookup.Contains(KnowledgeBase))
            return [];
        return _lookup[KnowledgeBase];
    }
}
