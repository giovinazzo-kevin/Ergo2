using Ergo.Lang.Ast;
using Ergo.Lang.Ast.Extensions;
using Ergo.Libs.Dict;
using Ergo.Shared.Types;

namespace Ergo.Libs.Dict.Ast;

public class Dict(Either<Atom, Variable> functor, IEnumerable<BinaryExpression> pairs) 
    : Complex(WellKnown.Functor, [CastFunctor(functor), CastPairs(pairs)])
{
    static Term CastFunctor(Either<Atom, Variable> functor)
    {
        Maybe<Atom> atom = functor;
        Maybe<Variable> var = functor;
        return atom.Cast<Term>()
            .Or(var.Cast<Term>())
            .GetOrThrow();
    }

    static Term CastPairs(IEnumerable<BinaryExpression> pairs)
    {
        var unique = pairs.OrderBy(p => p.Lhs).DistinctBy(p => p.Lhs).ToList();
        if (unique.Count == 0)
            return List.WellKnown.EmptyList;
        return new List.Ast.List(unique);
    }

    public Term DictFunctor => Args[0];
    public IEnumerable<BinaryExpression> Pairs => Args[1] is not List.Ast.List ? [] : ((List.Ast.List)Args[1]).Head.Cast<BinaryExpression>();
    public int Length => Args[1] is not List.Ast.List ? 0 : ((List.Ast.List)Args[1]).Count;

    public override bool IsGround => DictFunctor.IsGround && Pairs.All(p => p.IsGround);

    public override IEnumerable<Variable> Variables
    {
        get
        {
            if (DictFunctor is Variable v)
                yield return v;
            foreach (var vv in Pairs.SelectMany(p => p.Lhs.GetVariables().Concat(p.Rhs.GetVariables())))
                yield return vv;
        }
    }

    public override Term[] Arguments =>
        [DictFunctor, .. Pairs.SelectMany(p => new[] { p.Lhs, p.Rhs })];

    public override Maybe<Signature> Signature =>
        new Signature(WellKnown.Functor, 2);

    public override string Expl
    {
        get
        {
            var kvps = string.Join(", ", Pairs.Select(p => $"{p.Lhs.Expl}: {p.Rhs.Expl}"));
            return $"{DictFunctor.Expl}{{{kvps}}}";
        }
    }
}
