namespace Ergo.Compiler.Emission.WellKnown;

public static class Delegates
{
    public delegate void EmitGet(Emitter emitter, EmitterContext ctx, int sig, Lang.Ast.Term[] args, int Ai, Dictionary<string, int>? varsByName);
    public delegate void EmitPut(Emitter emitter, EmitterContext ctx, int sig, Lang.Ast.Term[] args, int Ai, Dictionary<string, int>? varsByName, bool deep);
}
