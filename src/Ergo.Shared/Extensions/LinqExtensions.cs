namespace Ergo.Shared.Extensions;

public static class LinqExtensions
{
    public static IEnumerable<(T, int)> Iterate<T>(this IEnumerable<T> source) => source.Select((x, i) => (x, i));
}
