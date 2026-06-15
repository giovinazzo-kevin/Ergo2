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
    public __WORD write_heap_term(Lang.Ast.Term term)
    {
        // Try abstract term handlers first (before Complex, since abstract terms may inherit from it)
        if (_QUERY.Source.AbstractTerms.Count > 0) {
            foreach (var (sig, abs) in _QUERY.Source.AbstractTerms) {
                if (abs.AstType.IsInstanceOfType(term)) {
                    return ((WellKnown.Delegates.Put)abs.Put)(this, term);
                }
            }
        }
        switch (term) {
            case Atom a: {
                    var c = _QUERY.Bytecode.AddConstant(a);
                    return (Term)(CON, c);
                }
            case Variable: {
                    var addr = H;
                    Heap[H++] = (Term)(REF, addr);
                    return (Term)(REF, addr);
                }
            case Complex s: {
                    var fAddr = H;
                    var fc = _QUERY.Bytecode.AddConstant(s.Functor);
                    Heap[H++] = (Signature)(fc, s.Arity);
                    for (int i = 0; i < s.Args.Length; i++)
                        Heap[H++] = write_heap_term(s.Args[i]);
                    return (Term)(STR, fAddr);
                }
            default:
                throw new NotSupportedException(
                    $"write_heap_term: {term.GetType().Name}");
        }
    }

    public Lang.Ast.Term read_heap_term(__ADDR addr)
    {
#if WAM_TRACE
        Trace.WriteLine($"[WAM] read_heap_term addr={addr}");
#endif
        addr = deref(addr);
        var term = (Term)Store[addr];
        return Read(term);

        Lang.Ast.Term Read(Term term)
        {
            // Follow REF chains for bound variables
            if (term.Tag == REF) {
                var a = deref(term.Value);
                var resolved = (Term)Store[a];
                if (resolved.Tag == REF && resolved.Value == a)
                    return new Lang.Ast.Variable($"_{a}"); // Unbound
                return Read(resolved);
            }
            return term.Tag switch {
                CON => Constants[term.Value],
                STR => ReadStructure(term.Value),
                ABS => ReadAbstract(term.Value),
                _ => throw new NotSupportedException($"Tag {term.Tag} not supported")
            };
        }

        Lang.Ast.Term ReadStructure(__ADDR addr)
        {
            var functor = (Signature)Heap[addr]; // e.g. likes/2
            var args = new Lang.Ast.Term[functor.N];

            for (int i = 0; i < functor.N; i++)
                args[i] = Read(Heap[addr + 1 + i]);

            var atom = Constants[functor.F];
            if (_QUERY.Source.Reconstructors.TryGetValue((atom.Value, functor.N), out var reconstruct))
                return reconstruct(args);

            return new Lang.Ast.Complex(atom, args);
        }

        Lang.Ast.Term ReadAbstract(__ADDR addr)
        {
            var sig = Heap[addr];
            if (_QUERY.Source.AbstractTerms.TryGetValue(sig, out var abs)) {
                return ((WellKnown.Delegates.Get)abs.Get)(this, addr);
            }
            throw new NotSupportedException($"No abstract term handler registered for signature {sig}");
        }
    }

}
