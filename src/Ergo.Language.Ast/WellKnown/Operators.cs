namespace Ergo.Language.Ast.WellKnown;
using static Operator.Type;

public static class Operators
{
    public static readonly Operator HornUnary = new(1200, fx, Functors.Horn);
    public static readonly Operator HornBinary = new(1200, xfx, Functors.Horn);

    public static readonly Operator Disjunction = new(1100, xfy, Functors.Semicolon, Functors.Disjunction);
    public static readonly Operator Conjunction = new(1000, xfy, Functors.Comma, Functors.Conjunction);

    public static readonly Operator Addition = new(500, yfx, Functors.Addition);
    public static readonly Operator Subtraction = new(500, yfx, Functors.Subtraction);
    public static readonly Operator Multiplication = new(400, yfx, Functors.Multiplication);
    public static readonly Operator Division = new(400, yfx, Functors.Division);

    public static readonly Operator Signature = Division;
    public static readonly Operator Module = new(600, xfy, Functors.Module);

    public static readonly Operator Pipe = new(1105, xfy, Functors.Pipe);
    public static readonly Operator List = new(900, xfy, Functors.List);
    public static readonly Operator Set = new(900, xfy, Functors.Set);

    public static readonly HashSet<Operator> RESERVED = [List, Set];
}
