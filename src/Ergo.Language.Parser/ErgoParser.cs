using Ergo.Language.Ast;
using Ergo.Language.Ast.WellKnown;
using Ergo.Language.Lexer;
using Ergo.Language.Lexer.WellKnown;
using Ergo.Language.Parser.Extensions;
using Ergo.Shared.Interfaces;
using Ergo.Shared.Types;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Ergo.Language.Parser;
using static Operator.Fixity;

public class ErgoParser : IDisposable
{
    public readonly ErgoLexer Lexer;
    public readonly ParserContext Context;

    #region AST
    public Func<Maybe<__string>> __string => () =>
        Transact(out var tx)
        .Expect<string>(Token.Type.String)
        .Select<__string>(x => x)
        .Do(tx.Commit, tx.Rollback)
        .DoWhenSome(x => Trace(x));
    public Func<Maybe<__double>> __double => () => 
        Transact(out var tx)
        .Expect<double>(Token.Type.Number)
        .Select<__double>(x => x)
        .Do(tx.Commit, tx.Rollback)
        .DoWhenSome(x => Trace(x));
    public Func<Maybe<__bool>> __bool => () => 
        Transact(out var tx)
        .Expect<string>(Token.Type.Keyword, Symbols.Boolean.Contains)
        .Select<__bool>(x => Symbols.True.Contains(x))
        .Do(tx.Commit, tx.Rollback)
        .DoWhenSome(x => Trace(x));
    public Func<Maybe<__string>> Cut => () => 
         Transact(out var tx)
        .Expect<string>(Token.Type.Keyword, Symbols.Cut.Contains)
        .Select(x => Literals.Cut)
        .Do(tx.Commit, tx.Rollback)
        .DoWhenSome(x => Trace(x));
    public Func<Maybe<__string>> Identifier => () => 
         Transact(out var tx)
        .Expect<string>(Token.Type.Term, Ast.Atom.IsAtomIdentifier)
        .Select<__string>(x => x)
        .Do(tx.Commit, tx.Rollback)
        .DoWhenSome(x => Trace(x));
    public Func<Maybe<Atom>> Atom => () =>
        Maybe.Or(
            Cut.Cast<__string, Atom>,
            EmptyList.Cast<__string, Atom>,
            __string.Cast<__string, Atom>,
            __double.Cast<__double, Atom>,
            __bool.Cast<__bool, Atom>,
            Identifier.Cast<__string, Atom>)
        .DoWhenSome(x => Trace(x));
    public Func<Maybe<Variable>> Variable => () => 
         Transact(out var tx)
        .Expect<string>(Token.Type.Term, Ast.Variable.IsVariableIdentifier)
        .Select(Context.GetVariable)
        .Do(tx.Commit, tx.Rollback)
        .DoWhenSome(x => Trace(x));
    public Func<Maybe<IEnumerable<Term>>> Args => () => 
        Maybe.Or(
            () => Parenthesized(ConsExpression(Operators.Conjunction))
                .Select(x => x.Contents),
            () => Parenthesized(Term)
                .Select(x => Enumerable.Empty<Term>().Append(x.Parenthesized(false))))
        .DoWhenSome(x => Trace(new List(x)));
    public Func<Maybe<Complex>> Complex => () => 
         Transact(out var tx)
        .Atom().Map(functor => Args()
            .Select(args => new Complex(functor, [.. args])))
        .Do(tx.Commit, tx.Rollback)
        .DoWhenSome(x => Trace(x));
    public Func<Maybe<Term>> Term => () => 
        Maybe.Or(
            () => Parenthesized(Expression.Cast<Expression, Term>),
            () => Parenthesized(Term),
            List.Cast<List, Term>,
            Variable.Cast<Variable, Term>,
            Complex.Cast<Complex, Term>,
            Atom.Cast<Atom, Term>)
        .DoWhenSome(x => Trace(x));
    public Func<Maybe<Expression>> Expression => () => 
        Maybe.Or(
            BinaryExpression.Cast<BinaryExpression, Expression>,
            PrefixExpression.Cast<PrefixExpression, Expression>,
            PostfixExpression.Cast<PostfixExpression, Expression>)
        .DoWhenSome(x => Trace(x));
    public Func<Maybe<PrefixExpression>> PrefixExpression => () => 
         Transact(out var tx)
        .ExpectOperator(Prefix)
        .Map(op => Maybe.Or(
                PostfixExpression.Cast<PostfixExpression, Term>,
                Term)
            .Select(arg => new PrefixExpression(op, arg)))
        .Do(tx.Commit, tx.Rollback)
        .DoWhenSome(x => Trace(x));
    public Func<Maybe<PostfixExpression>> PostfixExpression => () =>
         Transact(out var tx)
        .Term()
        .Map(arg => ExpectOperator(Postfix)
            .Select(op => new PostfixExpression(op, arg)))
        .Do(tx.Commit, tx.Rollback)
        .DoWhenSome(x => Trace(x));
    public Func<Maybe<Term>> BinaryExpressionLhs => () =>
        Maybe.Or(
            PrefixExpression.Cast<PrefixExpression, Term>,
            PostfixExpression.Cast<PostfixExpression, Term>,
            Term)
        .DoWhenSome(x => Trace(x));
    public Func<Maybe<Term>> BinaryExpressionRhs => () =>
        Maybe.Or(
            Expression.Cast<Expression, Term>,
            Term)
        .DoWhenSome(x => Trace(x));
    public Func<Maybe<BinaryExpression>> BinaryExpression => () => 
         Transact(out var tx)
        .BinaryExpressionLhs()
        .Map(lhs => ExpectOperator(Infix)
            .Map(op => BinaryExpressionRhs()
                .Select(rhs => Ast.BinaryExpression.Associate(new(op, lhs, rhs)))))
        .Do(tx.Commit, tx.Rollback)
        .DoWhenSome(x => Trace(x));
    public Func<Operator, Func<Maybe<ConsExpression>>> ConsExpression => 
        (Operator op) => () => 
         Transact(out var tx)
        .BinaryExpression()
        .Where(x => x.Operator.Equals(op))
        .Where(x => x.IsCons)
        .Select(x => new ConsExpression(op, x.Lhs, x.Rhs))
        .Do(tx.Commit, tx.Rollback)
        .DoWhenSome(x => Trace(x));
    public Func<Maybe<BinaryExpression>> HeadTailExpression => () => 
         Transact(out var tx)
        .BinaryExpression()
        .Where(x => x.Operator.Equals(Operators.Pipe))
        .Where(x => x.IsHeadTail)
        .Do(tx.Commit, tx.Rollback)
        .DoWhenSome(x => Trace(x));
    public Func<Maybe<List>> ListHeadTail => () => 
         Transact(out var tx)
        .Parenthesized(Collections.List, HeadTailExpression)
            .Select(x => new List(ExtractListHead(x), x.Rhs))
        .Do(tx.Commit, tx.Rollback)
        .DoWhenSome(x => Trace(x));
    public Func<Maybe<List>> ListNoTail => () => 
         Transact(out var tx)
        .Parenthesized(Collections.List, ConsExpression(Operators.Conjunction))
            .Select(x => new List(x.Contents))
        .Do(tx.Commit, tx.Rollback)
        .DoWhenSome(x => Trace(x));
    public Func<Maybe<List>> ListSingleton => () => 
         Transact(out var tx)
        .Parenthesized(Collections.List, Term)
            .Select(x => new List([x]))
        .Do(tx.Commit, tx.Rollback)
        .DoWhenSome(x => Trace(x));
    public Func<Maybe<__string>> EmptyList => () => 
         Parenthesized(Collections.List, () => Maybe.Some<Atom>(null!))
        .Select(_ => Literals.EmptyList);
    public Func<Maybe<List>> List => () =>
         Maybe.Or(ListHeadTail, ListNoTail, ListSingleton)
        .DoWhenSome(x => Trace(x));
    public Func<Maybe<Directive>> Directive => () =>
         Transact(out var tx)
        .PrefixExpression()
        .Where(x => x.Operator.Equals(Operators.HornUnary))
        .Select(x => new Directive(x.Arg))
        .Do(tx.Commit, tx.Rollback)
        .DoWhenSome(x => Trace(x));
    public Func<Maybe<Clause>> Clause => () =>
         Transact(out var tx)
        .BinaryExpression()
        .Where(x => x.Operator.Equals(Operators.HornBinary))
        .Select(x => new Clause(x.Lhs, x.Rhs))
        .Do(tx.Commit, tx.Rollback)
        .DoWhenSome(x => Trace(x));
    public Func<Maybe<Fact>> Fact => () =>
         Transact(out var tx)
        .Term()
        .Select(x => new Fact(x))
        .Do(tx.Commit, tx.Rollback)
        .DoWhenSome(x => Trace(x));
    public Func<Maybe<Directive[]>> DirectiveDefinitions => () =>
        ParseUntilFail(
            Definition(Directive));
    public Func<Maybe<Clause[]>> FactOrClauseDefinitions => () =>
        ParseUntilFail(() =>
            Maybe.Or(
                Definition(Clause),
                Definition(Fact).Cast<Fact, Clause>));
    public Func<Maybe<Program>> Program => () =>
         Transact(out var tx)
        .DirectiveDefinitions()
        .Where(x => x.Length > 0)
        .Where(x => x[0].Functor is __string { Value: "module" })
        .Map(dirs => 
             FactOrClauseDefinitions()
            .Select(clauses => new Program(dirs, clauses)))
        .Do(tx.Commit, tx.Rollback)
        .DoWhenSome(x => Trace(x));
    #endregion
    #region Helpers
    [Conditional("TRACE")]
    public static void Trace(IExplainable some, [CallerMemberName] string method = "") => 
         System.Diagnostics.Trace.WriteLine(some.Expl, method);
    public ErgoParser Transact(out Tx<LexerState> tx)
    {
        tx = Lexer.Transact();
        return this;
    }
    public ErgoParser EnterBlock()
    {
        /*
            Pushes a new block onto the context's stack.
            Variables with the same name are only referentially equal when they are parsed in the same block.
            A block can be a directive, a clause or a fact -- but also a closure for a lambda.
         */
        Context.EnterBlock();
        return this;
    }
    public static IEnumerable<Term> ExtractListHead(BinaryExpression headTail)
    {
        if (headTail.Lhs is ConsExpression cons)
            return cons.Contents;
        if (headTail.Lhs is BinaryExpression { IsCons: true, Operator: var pOp } pseudoCons
            && pOp.Equals(Operators.Conjunction))
            return new ConsExpression(pseudoCons.Operator, pseudoCons.Lhs, pseudoCons.Rhs).Contents;
        return [headTail.Lhs];
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
        Transact(out var tx);
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
         Parenthesized("(", ")", tryParse)
        .Select(x => (T)x.Parenthesized());
    public Maybe<T> Parenthesized<T>(Collection collection, Func<Maybe<T>> tryParse) where T : Term =>
         Parenthesized(collection.OpeningDelim, collection.ClosingDelim, tryParse);
    public Func<Maybe<T>> Definition<T>(Func<Maybe<T>> parser) where T : Term=> () =>
         Transact(out var tx)
        .EnterBlock()
        .Parse(parser)
            .Where(_ => ExpectDefinitionTerminator().HasValue)
        .DoAlways(Context.ExitBlock)
        .Do(tx.Commit, tx.Rollback)
        .DoWhenSome(x => Trace(x));
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
    public ErgoParser(ErgoLexer lexer, ParserContext? context = null)
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