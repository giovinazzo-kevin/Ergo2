using System.Reflection;

namespace Ergo.Libs;

public static class Libraries
{
    public static readonly Assembly[] Standard = [
        typeof(Stdlib.Lib.Stdlib).Assembly,
        typeof(Prologue.Lib.Prologue).Assembly,
        typeof(Math.Lib.Math).Assembly,
        typeof(List.Lib.List).Assembly,
        typeof(Set.Lib.Set).Assembly,
        typeof(Dict.Lib.Dict).Assembly,
        typeof(IO.Lib.IO).Assembly
    ];
}
