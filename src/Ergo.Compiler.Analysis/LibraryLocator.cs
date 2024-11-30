using Ergo.Shared.Extensions;
using System.Reflection;

namespace Ergo.Compiler.Analysis;

public class LibraryLocator
{
    private readonly ILookup<string, Type> _types;

    public LibraryLocator(params Assembly[] lookInAssemblies)
    {
        _types = lookInAssemblies
            .SelectMany(x => x.DefinedTypes)
            .Where(t => t.IsAssignableTo(typeof(Library))
                && t.GetConstructors().Any(c => c.GetParameters() is { Length: 1 } p
                    && p[0].ParameterType == typeof(Module)))
            .ToLookup(x => x.ToLibraryName(), x => x.AsType(), StringComparer.OrdinalIgnoreCase);
    }

    public IEnumerable<Type> Find(string moduleName)
    {
        if (!_types.Contains(moduleName))
            return [];
        return _types[moduleName];
    }
}
