using Ergo.Lang.Ast;
using Ergo.Lang.Ast.WellKnown;
using Ergo.Lang.Lexing;
using Ergo.Shared.Types;
using System.Text;

namespace Ergo.SDK.Fuzzing;

public class TermGenerator
{
    private readonly TermGeneratorContext _ctx = new();
    private readonly Random _rng;
    private readonly Operator[] _prefixOps;
    private readonly Operator[] _postfixOps;
    private readonly Operator[] _infixOps;
    public TermGeneratorProfile Profile { get; set; } = TermGeneratorProfile.Default;

    public TermGenerator(OperatorLookup? ops = null, Random? rng = null)
    {
        _rng = rng ?? new();
        ops ??= new();
        _prefixOps = ops.Values.Where(x => x.Fixity_ == Operator.Fixity.Prefix).ToArray();
        _postfixOps = ops.Values.Where(x => x.Fixity_ == Operator.Fixity.Postfix).ToArray();
        _infixOps = ops.Values.Where(x => x.Fixity_ == Operator.Fixity.Infix).ToArray();
    }


    public Func<__string> __string => () => 
        new(String(
            shouldStartWithDiscard: 
                Profile.IncludeQuotedStrings 
                && Chance(1, Math.Log2(Profile.MaxIdentifierLength) / Profile.MaxIdentifierLength),
            shouldStartWithUppercase: 
                Profile.IncludeQuotedStrings 
                && Chance(1, 5),
            mayContainSpaces: 
                Profile.IncludeQuotedStrings 
                && Chance(1, 2),
            minLength: 1,
            maxLength: Profile.MaxIdentifierLength
        ));
    public Func<__double> __double => () =>
        new(PositiveOrNegativeValue);
    public Func<__bool> __bool => () =>
        new(Chance(1, 2));
    public Func<__string> Cut => () =>
        Literals.Cut;
    public Func<__string> EmptyList => () =>
        Literals.EmptyList;
    public Func<Atom> Atom => () =>
         Choose<Atom>([
             Cut,
             EmptyList,
             __string,
             __double,
             __bool,
         ]);
    public Func<Variable> Variable => () =>
        _ctx.Parser.GetVariable(String(
            shouldStartWithDiscard: Chance(1, 2),
            shouldStartWithUppercase: true,
            mayContainSpaces: false,
            minLength: 1,
            maxLength: Profile.MaxIdentifierLength
        ));
    public Func<Complex> Complex => () =>
    {
        using var _ = Transact(Profile with { MaxComplexDepth = Profile.MaxComplexDepth - 1 });
        var functor = Get(__string, Profile with { 
            MaxIdentifierLength = Profile.MaxComplexFunctorLength });
        if (Profile.MaxComplexDepth <= 0)
            return new Complex(functor, [Literals.False]);
        var args = Get(() => Many(1, Profile.MaxComplexArity, Term), Profile with { 
            MaxIdentifierLength = Profile.MaxComplexArgLength });
        return new Complex(functor, args);
    };
    public Func<Term> Term => () =>
        Choose<Term>([
            Atom,
            Variable,
            Complex
        ]);
    public Func<PrefixExpression> PrefixExpression => () =>
        new(Choose(_prefixOps), Term());
    public Func<PostfixExpression> PostfixExpression => () =>
        new(Choose(_postfixOps), Term());
    public Func<UnaryExpression> UnaryExpression => () =>
        Choose<UnaryExpression>([
            PrefixExpression,
            PostfixExpression
        ]);
    public Func<BinaryExpression> BinaryExpression => () =>
    {
        using var _ = Transact(Profile with { MaxExpressionDepth = Profile.MaxExpressionDepth - 1 });
        if (Profile.MaxExpressionDepth <= 0)
            return new(Choose(_infixOps), Term(), Term());
        return Lang.Ast.BinaryExpression.AddNecessaryParentheses(
            new(Choose(_infixOps), ExpressionOrTerm(), ExpressionOrTerm()));
    };
    public Func<ConsExpression> ConsExpression => () =>
    {
        var cons = new ConsExpression(Operators.Conjunction, Choose<Term>([ConsExpression, ExpressionOrTerm]), ExpressionOrTerm());
        var exp = Lang.Ast.BinaryExpression.AddNecessaryParentheses(cons);
        cons = new ConsExpression(exp.Operator, exp.Lhs, exp.Rhs);
        return cons;
    };
    public Func<Expression> Expression => () =>
        Choose<Expression>([
            UnaryExpression,
            BinaryExpression
        ]);
    public Func<Term> ExpressionOrTerm => () =>
        Choose<Term>([
            Expression,
            Term
        ]);
    public Func<Fact> Fact => () =>
    {
        var expr = UnaryExpression();
        return new Fact(expr.Arg);
    };
    public Func<Clause> Clause => () =>
    {
        var expr = BinaryExpression();
        return new Clause(expr.Lhs, expr.Rhs);
    };
    public Func<Clause> FactOrClause => () =>
        Choose<Clause>([Clause, Fact]);
    public Func<Directive> Directive => () =>
    {
        return new Directive(Complex());
    };
    public Func<Module> Module => () =>
    {
        var module = new Directive(new Complex("module", __string(), Literals.EmptyList));
        var numDirectives = _rng.Next(Profile.MinProgramDirectives, Profile.MaxProgramDirectives + 1);
        var numClauses = _rng.Next(Profile.MinProgramClauses, Profile.MaxProgramClauses + 1);
        var directives = new Directive[numDirectives];
        var clauses = new Clause[numClauses];
        for (int i = 0; i < numDirectives; i++)
            directives[i] = Directive();
        for (int i = 0; i < numClauses; i++)
            clauses[i] = Clause();
        return new Module(directives.Prepend(module), clauses);
    };
    #region Helpers
    public string String(
        bool shouldStartWithUppercase = false,
        bool shouldStartWithDiscard = false,
        bool mayContainSpaces = false,
        int minLength = 4,
        int maxLength = 16
    )
    {
        if (maxLength == 0)
            return "";
        var sb = new StringBuilder();
        var length = _rng.Next(minLength, maxLength);
        for (int i = 0; i < length; i++)
        {
            var addSpace = i > 0
                && mayContainSpaces
                && Chance(1, 10);
            sb.Append(addSpace
                ? ' '
                : Choose(TermGeneratorProfile.IdentifierChars));
        }
        sb.Insert(0, Choose(TermGeneratorProfile.StartIdentifierChars));
        if (shouldStartWithUppercase)
        {
            sb.Remove(0, 1);
            sb.Insert(0, char.ToUpper(Choose(TermGeneratorProfile.StartIdentifierChars)));
        }
        if (shouldStartWithDiscard)
        {
            sb.Remove(0, 1);
            sb.Insert(0, '_');
        }
        return sb.ToString();
    }
    public bool Chance(double numerator, double denominator) => 
        _rng.NextDouble() > numerator / denominator;
    public T Choose<T>(Func<object>[] choices) =>
        (T)_rng.GetItems(choices, choices.Length)[0]();
    public T Choose<T>(T[] choices) =>
        _rng.GetItems(choices, choices.Length)[0];
    public T[] Many<T>(int min , int max, Func<T> one) =>
         Enumerable.Range(0, _rng.Next(min, max + 1))
        .Select(_ => one())
        .ToArray();
    public Transaction<TermGeneratorProfile> Transact(TermGeneratorProfile tmp)
    {
        var oldProfile = Profile;
        Profile = tmp;
        return new(oldProfile, state => Profile = state);
    }
    public T Get<T>(Func<T> get, TermGeneratorProfile tmp)
    {
        using var _ = Transact(tmp);
        return get();
    }
    public double PositiveValue => Math.Round(_rng.NextDouble() * Profile.NumberMagnitude, 3);
    public double NegativeValue => -PositiveValue;
    public double PositiveOrNegativeValue => Math.Round((_rng.NextDouble() - 0.5) * 2 * Profile.NumberMagnitude, 3);
    #endregion
}