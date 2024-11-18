using Ergo.Lang.Ast;

namespace Ergo.Lang.Unification.Extensions;

public static class TermExtensions
{
    public static bool Unify(this Term a, Term b) => (a, b) switch
    {
        (Variable v1, Variable v2) 
            => Assign(v1, v2) && Assign(v2, v1),
        (Variable v1, _) 
            => Assign(v1, b),
        (_, Variable v2) 
            => Assign(v2, a),
        (Atom, Atom) when Equals(a, b)
            => true,
        (Complex ca, Complex cb) when ca.Arity == cb.Arity && ca.Functor.Equals(cb.Functor) 
            => UnifyArgs(ca.Args, cb.Args),
        _ => false
    };

    static bool Assign(Variable lhs, Term rhs)
    {
        lhs.Value = rhs;
        return true;
    }

    static bool UnifyArgs(Term[] a1, Term[] a2)
    {
        for (int i = 0; i < a1.Length; i++)
            if (!Unify(a1[i], a2[i]))
                return false;
        return true;
    }
}