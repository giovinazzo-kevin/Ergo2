using Ergo.Lang.Ast;
using Ergo.Lang.Ast.Extensions;
using Ergo.Shared.Types;

namespace Ergo.Libs.Dict.Ast;

public class Dict : Term
{
    public readonly Term DictFunctor;
    public readonly BinaryExpression[] Pairs;

    public override bool IsGround => DictFunctor.IsGround && Pairs.All(p => p.IsGround);

    public Dict(Term functor, IEnumerable<BinaryExpression> pairs)
    {
        DictFunctor = functor;
        // Sort pairs by key for canonical form
        Pairs = [.. pairs.OrderBy(p => p.Lhs)];
    }

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

    public override Maybe<Lang.Ast.Signature> Signature =>
        new Lang.Ast.Signature(Libs.Dict.WellKnown.Functor, 2);

    public override string Expl
    {
        get
        {
            var kvps = string.Join(", ", Pairs.Select(p => $"{p.Lhs.Expl}: {p.Rhs.Expl}"));
            return $"{DictFunctor.Expl}{{{kvps}}}";
        }
    }

    public override Term Clone() => new Dict(
        DictFunctor.Clone(),
        Pairs.Select(p => new BinaryExpression(p.Operator, p.Lhs.Clone(), p.Rhs.Clone())));
}
