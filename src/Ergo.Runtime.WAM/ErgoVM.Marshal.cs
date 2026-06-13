using Ergo.Lang.Ast;
using Ergo.Lang.Ast.WellKnown;
using Signature = Ergo.Compiler.Emission.Signature;
using Term = Ergo.Compiler.Emission.Term;

namespace Ergo.Runtime.WAM;

public partial class ErgoVM
{
    /// <summary>
    /// Marshals an AST Term onto the WAM heap, returning a
    /// single-word term value suitable for storing in A 
    /// registers or heap cells.
    /// </summary>
    public __WORD WriteHeapTerm(Lang.Ast.Term term)
    {
        // Try abstract term handlers first (before Complex, since abstract terms may inherit from it)
        if (KB.AbstractTerms.Count > 0) {
            foreach (var (sig, abs) in KB.AbstractTerms) {
                if (abs.AstType.IsInstanceOfType(term)) {
                    return ((WellKnown.Delegates.Put)abs.Put)(this, term);
                }
            }
        }
        switch (term) {
            case Atom a: {
                    var c = _QUERY.AddConstant(a);
                    return (Term)(CON, c);
                }
            case Variable: {
                    var addr = H;
                    Heap[H++] = (Term)(REF, addr);
                    return (Term)(REF, addr);
                }
            case Complex s: {
                    var fAddr = H;
                    var fc = _QUERY.AddConstant(s.Functor);
                    Heap[H++] = (Signature)(fc, s.Arity);
                    for (int i = 0; i < s.Args.Length; i++)
                        Heap[H++] = WriteHeapTerm(s.Args[i]);
                    return (Term)(STR, fAddr);
                }
            default:
                throw new NotSupportedException(
                    $"WriteHeapTerm: {term.GetType().Name}");
        }
    }
}
