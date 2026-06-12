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
            case List list: {
                    var elems = list.Head.ToArray();
                    if (elems.Length == 0) {
                        var c = _QUERY.AddConstant(Collections.List.EmptyElement);
                        return (Term)(CON, c);
                    }
                    var tail = WriteHeapTerm(list.Tail);
                    for (int i = elems.Length - 1; i >= 0; i--) {
                        var pairAddr = H;
                        Heap[H++] = WriteHeapTerm(elems[i]);
                        Heap[H++] = tail;
                        tail = (Term)(LIS, pairAddr);
                    }
                    return tail;
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
