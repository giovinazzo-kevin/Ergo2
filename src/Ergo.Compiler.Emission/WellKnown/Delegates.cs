namespace Ergo.Compiler.Emission.WellKnown;

public static class Delegates
{
    public delegate void EmitRead(Emitter emitter, EmitterContext ctx, Lang.Ast.Term[] args, int Ai);
    public delegate void EmitWrite(Emitter emitter, EmitterContext ctx, Lang.Ast.Term[] args, int Ai, Dictionary<string, int>? varsByName, bool deep);
}
