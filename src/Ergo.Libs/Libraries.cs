using Ergo.Libs.Math;
using Ergo.Libs.Prologue;
using System.Reflection;

namespace Ergo.Libs;
public static class Libraries
{
    public static readonly Assembly[] Standard = [
        typeof(LibPrologue).Assembly,
        typeof(LibMath).Assembly
    ];
}
