namespace Ergo.IO;

public class ModuleLocatorIndex
{
    private readonly IEnumerable<FileInfo> Source;
    private ILookup<string, FileInfo> _lookup = null!;

    public ModuleLocatorIndex(IEnumerable<FileInfo> scan)
    {
        Source = scan;
        Update();
    }

    public void Update()
    {
        _lookup = Source
            .ToLookup(x => Path.GetFileNameWithoutExtension(x.Name));
    }

    public IEnumerable<FileInfo> Find(string module)
    {
        if (!_lookup.Contains(module))
            return [];
        return _lookup[module];
    }
}

