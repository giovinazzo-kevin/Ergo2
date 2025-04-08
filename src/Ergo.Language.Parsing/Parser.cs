using Ergo.Lang.Ast;
using Ergo.Lang.Ast.Extensions;
using Ergo.Lang.Ast.WellKnown;
using Ergo.Lang.Lexing;
using Ergo.Lang.Lexing.WellKnown;
using Ergo.Lang.Parsing.Extensions;
using Ergo.Shared.Interfaces;
using Ergo.Shared.Types;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Ergo.Lang.Parsing;
using static Operator.Fixity;

public class Parser : IDisposable
{
    public readonly Lexing.Lexer Lexer;
    public readonly ParserContext Context;

    #region AST
    public Func<Maybe<__string>> __string => Transact([() =>
         Expect<string>(Token.Type.String)
        .Select<__string>(x => x)
    ]);
    public Func<Maybe<__int>> __int => Transact([() =>
         Expect<int>(Token.Type.Integer)
        .Select<__int>(x => x)
    ]);
    public Func<Maybe<__double>> __double => Transact([() =>
         Expect<double>(Token.Type.Decimal)
        .Select<__double>(x => x)
    ]);
    public Func<Maybe<__bool>> __bool => Transact([() => 
         Expect<string>(Token.Type.Keyword, Symbols.Boolean.Contains)
        .Select<__bool>(x => Symbols.True.Contains(x))
    ]);
    public Func<Maybe<__string>> Cut => Transact([() =>
         Expect<string>(Token.Type.Keyword, Symbols.Cut.Contains)
        .Select(x => Literals.Cut)
    ]);
    public Func<Maybe<__string>> Identifier => Transact([() =>
         Expect<string>(Token.Type.Term, Ast.Atom.IsAtomIdentifier)
        .Select<__string>(x => x)
    ]);
    public Func<Maybe<Atom>> Atom => Transact([
        Cut.Cast<__string, Atom>,
        EmptyList.Cast<__string, Atom>,
        __string.Cast<__string, Atom>,
        __double.Cast<__double, Atom>,
        __int.Cast<__int, Atom>,
        __bool.Cast<__bool, Atom>,
        Identifier.Cast<__string, Atom>
    ]);
    public Func<Maybe<Variable>> Variable => Transact([() =>
         Expect<string>(Token.Type.Term, Ast.Variable.IsVariableIdentifier)
        .Select(Context.GetVariable)
    ]);
    public Func<Maybe<Complex>> Complex => Transact([() =>
         Atom().Map(functor => Maybe.Or(
             () => Parenthesized(ConsExpression(Operators.Conjunction))
                .Select(args => new Complex(functor, [.. args.Contents])),
             () => Parenthesized(Term)
                .Select(arg => new Complex(functor, [arg.Parenthesized(false)]))))
    ]);
    public Func<Maybe<Term>> Term => Transact([
        () => Parenthesized(Expression.Cast<Expression, Term>),
        () => Parenthesized(Term),
        List.Cast<List, Term>,
        Variable.Cast<Variable, Term>,
        Complex.Cast<Complex, Term>,
        Atom.Cast<Atom, Term>
    ]);
    public Func<Maybe<Expression>> Expression => Transact([
        BinaryExpression.Cast<BinaryExpression, Expression>,
        PrefixExpression.Cast<PrefixExpression, Expression>,
        PostfixExpression.Cast<PostfixExpression, Expression>
    ]);
    public Func<Maybe<PrefixExpression>> PrefixExpression => Transact([() =>
         ExpectOperator(Prefix)
        .Map(op => Maybe.Or(
                PostfixExpression.Cast<PostfixExpression, Term>,
                Term)
            .Select(arg => new PrefixExpression(op, arg)))
    ]);
    public Func<Maybe<PostfixExpression>> PostfixExpression => Transact([() =>
         Term()
        .Map(arg => ExpectOperator(Postfix)
            .Select(op => new PostfixExpression(op, arg)))
    ]);
    public Func<Maybe<Term>> BinaryExpressionLhs => Transact([
        PrefixExpression.Cast<PrefixExpression, Term>,
        PostfixExpression.Cast<PostfixExpression, Term>,
        Term
    ]);
    public Func<Maybe<Term>> BinaryExpressionRhs => Transact([
        Expression.Cast<Expression, Term>,
        Term
    ]);
    public Func<Maybe<BinaryExpression>> BinaryExpression => Transact([() =>
         BinaryExpressionLhs()
        .Map(lhs => ExpectOperator(Infix)
            .Map(op => BinaryExpressionRhs()
                .Select(rhs => Ast.BinaryExpression.Associate(new(op, lhs, rhs)))))
    ]);
    public Func<Operator, Func<Maybe<ConsExpression>>> ConsExpression => op => Transact([() =>
         BinaryExpression()
        .Where(x => x.Operator.Equals(op))
        .Where(x => x.IsCons)
        .Select(x => new ConsExpression(op, x.Lhs, x.Rhs))
    ]);
    public Func<Maybe<SignatureExpression>> SignatureExpression => Transact([() =>
         BinaryExpression()
        .Where(x => x.Operator.Equals(Operators.Signature)
            && x.Lhs is Atom
            && x.Rhs is __int)
        .Select(x => new SignatureExpression((Atom)x.Lhs, (__int)x.Rhs))
    ]);
    public Func<Maybe<BinaryExpression>> HeadTailExpression => Transact([() =>
         BinaryExpression()
        .Where(x => x.Operator.Equals(Operators.Pipe))
        .Where(x => x.IsHeadTail)
    ]);
    public Func<Maybe<List>> ListHeadTail => Transact([() =>
         Parenthesized(Collections.List, HeadTailExpression)
        .Select(x => new List(Ast.List.ExtractHead(x), x.Rhs))
    ]);
    public Func<Maybe<List>> ListNoTail => Transact([() =>
         Parenthesized(Collections.List, ConsExpression(Operators.Conjunction))
        .Select(x => new List(x.Contents))
    ]);
    public Func<Maybe<List>> ListSingleton => Transact([() =>
         Parenthesized(Collections.List, Term)
        .Select(x => new List([x]))
    ]);
    public Func<Maybe<__string>> EmptyList => Transact([() =>
         Parenthesized(Collections.List, () => Maybe.Some<Atom>(null!))
        .Select(_ => Literals.EmptyList)
    ]);
    public Func<Maybe<List>> List => Transact([
        ListHeadTail, 
        ListNoTail, 
        ListSingleton
    ]);
    public Func<Maybe<Directive>> Directive => Transact([() =>
        PrefixExpression()
        .Where(x => x.Operator.Equals(Operators.HornUnary))
        .Where(x => x.Arg is Atom || x.Arg is Complex)
        .Select(x => new Directive(x.Arg))
    ], isBlock: true);
    public Func<Maybe<Clause>> Clause => Transact([() =>
         BinaryExpression()
        .Where(x => x.Operator.Equals(Operators.HornBinary))
        .Select(x => new Clause(x.Lhs, x.Rhs))
    ], isBlock: true);
    public Func<Maybe<Fact>> Fact => Transact([() =>
         Term()
        .Select(x => new Fact(x))
    ], isBlock: true);
    public Func<Maybe<Clause>> ClauseOrFact => () => Maybe.Or(
        Clause, 
        Fact.Cast<Fact, Clause>
    );
    public Func<Maybe<Directive[]>> DirectiveDefinitions => () => ParseUntilFail(
        Definition(Directive));
    public Func<Maybe<Clause[]>> ClauseOrFactDefinitions => () => ParseUntilFail(
        Definition(ClauseOrFact));
    public Func<Maybe<Module>> Module => Transact([() =>
         DirectiveDefinitions()
        .Map(dirs =>
             ClauseOrFactDefinitions()
            .Select(clauses => new Module(dirs, clauses)))
    ]);
    #endregion
    #region Helpers
    [Conditional("PARSER_TRACE")]
    public static void Trace(IExplainable some, string method ) => 
         System.Diagnostics.Trace.WriteLine(some.Expl, method);
    public Func<Maybe<T>> Transact<T>(IEnumerable<Func<Maybe<T>>> parsers, bool isBlock = false, [CallerMemberName] string method = "")
        where T : IExplainable
    {
        return () =>
        {
            if (Context.IsFailureMemoized(Lexer.State, method))
                return default;
            using var tx = Lexer.Transact();
            if (isBlock)
                Context.EnterBlock();
            return Maybe.Or(parsers)
                .Do(tx.Commit, tx.Rollback)
                .Do(x => Trace(x, method), () => Context.MemoizeFailure(Lexer.State, method))
                .DoAlways(() => {
                    if (isBlock)
                        Context.ExitBlock();
                });
        };
    }
    public Maybe<T> Expect<T>(IEnumerable<Token.Type> types, Func<T, bool> pred)
    {
        return Lexer.ReadNext()
            .Where(Condition)
            .Select(token => (T)token.Value);
        bool Condition(Token token)
        {
            if (!types.Contains(token.Type_))
                return false;
            if (token.Value is not T t)
                return false;
            return pred(t);
        }
    }
    public Maybe<Operator> ExpectOperator(Func<Operator, bool> match) => 
         Lexer.ReadNextOperator(match);
    public Maybe<Operator> ExpectOperator(Operator.Fixity fixity, Func<Operator, bool>? match = null) => 
         ExpectOperator(x => x.Fixity_ == fixity && (match?.Invoke(x) ?? true));
    public Maybe<string> ExpectDelimiter(Func<string, bool> condition) => 
         Expect([Token.Type.Punctuation], condition);
    public Maybe<T> Expect<T>(Token.Type type, Func<T, bool> cond) => 
         Expect([type], cond);
    public Maybe<T> Expect<T>(Token.Type type) => 
         Expect<T>(type, _ => true);
    public Maybe<T> Expect<T>(IEnumerable<Token.Type> types) =>
         Expect<T>(types, _ => true);
    public Maybe<string> ExpectDefinitionTerminator() =>
         ExpectDelimiter(x => x == ".");
    public Maybe<T> Parenthesized<T>(string opening, string closing, Func<Maybe<T>> tryParse)
    {
        using var tx = Lexer.Transact();
        if (!Expect<string>(Token.Type.Punctuation, str => str.Equals(opening)).HasValue
            || !tryParse().TryGetValue(out var ret)
            || !Expect<string>(Token.Type.Punctuation, str => str.Equals(closing)).HasValue)
        {
            tx.Rollback();
            return default;
        }
        tx.Commit();
        return ret;
    }
    public Maybe<T> Parenthesized<T>(Func<Maybe<T>> tryParse) where T : Term => 
         Parenthesized(Collections.Tuple, tryParse)
        .Select(x => (T)x.Parenthesized());
    public Maybe<T> Parenthesized<T>(Collection collection, Func<Maybe<T>> tryParse) where T : Term =>
         Parenthesized(collection.OpeningDelim, collection.ClosingDelim, tryParse);
    public Func<Maybe<T>> Definition<T>(Func<Maybe<T>> parser, [CallerMemberName] string method = "") where T : Term => Transact([() =>
        Parse(parser)
            .Where(_ => ExpectDefinitionTerminator().HasValue)
    ], method: method);
    public T Parse<T>(Func<T> parser) => 
        parser();
    public Maybe<T[]> ParseUntilFail<T>(Func<Maybe<T>> parser)
    {
        var items = new List<T>();
        while (parser().TryGetValue(out var item))
            items.Add(item);
        if (items.Count == 0)
            return default;
        return (T[])[.. items];
    }
    public IEnumerable<T> ParseUntilFailEnumerable<T>(Func<Maybe<T>> parser)
    {
        while (parser().TryGetValue(out var item))
            yield return item;
    }
#endregion
    public Parser(Lexing.Lexer lexer, ParserContext? context = null)
    {
        Lexer = lexer;
        Context = context ?? new();
    }
    public void Dispose()
    {
        Lexer.Dispose();
        GC.SuppressFinalize(this);
    }
}