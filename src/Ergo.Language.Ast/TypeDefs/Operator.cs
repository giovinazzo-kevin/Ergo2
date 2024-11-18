namespace Ergo.Lang.Ast;

public readonly record struct Operator(int Precedence, Operator.Type Type_, params Atom[] Functors)
{
    public enum Type { fx, fy, xf, yf, xfx, xfy, yfx }
    public enum Fixity { Prefix, Infix, Postfix }
    public enum Associativity { None, Left, Right }

    static readonly (
            Dictionary<(Fixity Fixity, Associativity Associativity), Type> To,
            Dictionary<Type, (Fixity Fixity, Associativity Associativity)> From
        ) Map = (
            new() {
                { (Fixity.Prefix, Associativity.None), Type.fx  },
                { (Fixity.Postfix, Associativity.None), Type.xf },
                { (Fixity.Infix, Associativity.None), Type.xfx  },
                { (Fixity.Infix, Associativity.Right), Type.xfy },
                { (Fixity.Infix, Associativity.Left), Type.yfx  },
                { (Fixity.Prefix, Associativity.Right), Type.fy },
                { (Fixity.Postfix, Associativity.Left), Type.yf }
            },
            new() {
                { Type.fx, (Fixity.Prefix, Associativity.None) },
                { Type.xf, (Fixity.Postfix, Associativity.None) },
                { Type.fy, (Fixity.Prefix, Associativity.Right) },
                { Type.yf, (Fixity.Postfix, Associativity.Left) },
                { Type.xfx, (Fixity.Infix, Associativity.None) },
                { Type.xfy, (Fixity.Infix, Associativity.Right) },
                { Type.yfx, (Fixity.Infix, Associativity.Left) }
            }
        );

    public readonly Fixity Fixity_ = Map.From[Type_].Fixity;
    public readonly Associativity Associativity_ = Map.From[Type_].Associativity;
    public readonly Atom CanonicalFunctor = Functors[0];
}
