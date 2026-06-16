using Ergo.Shared.Interfaces;
using Ergo.Shared.Types;
using System.Diagnostics;

namespace Ergo.Lang.Ast;

[DebuggerDisplay("{Expl}")]
public abstract class Term : IExplainable, IComparable<Term>
{
    public bool IsParenthesized { get; set; }
    public abstract bool IsGround { get; }
    public abstract string Expl { get; }
    public override string ToString() => Expl;
    public static implicit operator Term(string s) => Variable.IsVariableIdentifier(s) ? new Variable(s) : new __string(s);
    public static implicit operator Term(bool b) => new __bool(b);
    public static implicit operator Term(double d) => new __double(d);
    public virtual IEnumerable<Variable> Variables => [];
    public virtual Term[] Args => [];

    public virtual Maybe<Signature> Signature => default;

    /// <summary>
    /// Standard order of terms: variables &lt; numbers &lt; atoms &lt; compound terms.
    /// </summary>
    public int CompareTo(Term? other)
    {
        if (other is null) return 1;
        var rank = TermRank(this) - TermRank(other);
        if (rank != 0) return rank;
        return CompareWithinRank(this, other);
    }

    private static int TermRank(Term t) => t switch {
        Variable => 0,
        __int => 1,
        __double => 2,
        __bool => 3,
        __string => 4,
        Atom => 4,
        _ => 5
    };

    private static int CompareWithinRank(Term a, Term b) => (a, b) switch {
        (Variable va, Variable vb) => string.Compare(va.Name, vb.Name, StringComparison.Ordinal),
        (__int ia, __int ib) => ((int)ia.Value).CompareTo((int)ib.Value),
        (__double da, __double db) => ((double)da.Value).CompareTo((double)db.Value),
        (__bool ba, __bool bb) => ((bool)ba.Value).CompareTo((bool)bb.Value),
        (Atom aa, Atom ab) => string.Compare(aa.Expl, ab.Expl, StringComparison.Ordinal),
        (Complex ca, Complex cb) => CompareComplex(ca, cb),
        _ => string.Compare(a.Expl, b.Expl, StringComparison.Ordinal)
    };

    private static int CompareComplex(Complex a, Complex b)
    {
        var arityDiff = a.Args.Length - b.Args.Length;
        if (arityDiff != 0) return arityDiff;
        var functorDiff = string.Compare(a.Functor.Expl, b.Functor.Expl, StringComparison.Ordinal);
        if (functorDiff != 0) return functorDiff;
        for (int i = 0; i < a.Args.Length; i++) {
            var argDiff = a.Args[i].CompareTo(b.Args[i]);
            if (argDiff != 0) return argDiff;
        }
        return 0;
    }
}
