namespace Ergo.Shared.Extensions;

public static class TypeExtensions
{
    public static string ToLibraryName(this Type x) => (x.Namespace?.Split('.').LastOrDefault() ?? x.Name).ToLower();
}
