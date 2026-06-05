using Ergo.Libs.IO;
using Ergo.Libs.List;
using Ergo.Libs.Math;
using Ergo.Libs.Prologue;
using Ergo.Libs.Stdlib;
using System.Reflection;

namespace Ergo.Libs;
public static class Libraries
{
    public static readonly Assembly[] Standard = [
        typeof(LibStdlib).Assembly,
        typeof(LibPrologue).Assembly,
        typeof(LibMath).Assembly,
        typeof(LibList).Assembly,
        typeof(LibIO).Assembly
    ];
}
