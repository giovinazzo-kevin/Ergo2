using Ergo.IO;
using Ergo.Language.Ast;
using Ergo.Language.Ast.Extensions;
using Ergo.Language.Ast.WellKnown;
using Ergo.Language.Lexer;
using Ergo.Language.Parser;
using Ergo.SDK.Fuzzing;
using Ergo.Shared.Extensions;
using Ergo.Shared.Interfaces;
using Ergo.Shared.Types;
using System.Collections;
using System.Reflection;

namespace Ergo.UnitTests;

public class ParserTestGenerator<T> : IEnumerable<object[]>
    where T : IExplainable
{
#if ENABLE_TESTGEN
    const int NUM_SAMPLES = 100;
#else
    const int NUM_SAMPLES = 0;
#endif
    private readonly TermGenerator _generator = new(
        ops: new(ParserTests.TestOperators), 
        rng:
#if DETERMINISTIC_TESTGEN
            new Random(0)
#else
            new Random()
#endif
    )
    {
        Profile = TermGeneratorProfile.Debug
    };
    private static readonly PropertyInfo _field = typeof(TermGenerator)
        .GetProperties(BindingFlags.Instance | BindingFlags.Public)
        .Single(x => x.PropertyType == typeof(Func<T>) && x.Name == typeof(T).Name);
    private IEnumerable<object[]> Generate() => 
         Enumerable.Range(0, NUM_SAMPLES)
        .Select(GenerateCase);
    object[] GenerateCase(int i)
    {
        return [
            ((Func<T>)_field.GetValue(_generator)!)().Expl
        ];
    }
    public IEnumerator<object[]> GetEnumerator() => Generate().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class ParserTests
{
    public static readonly Operator[] TestOperators = [
        new (60, Operator.Type.xf, (__string)"$"),
        new (500, Operator.Type.yfx, (__string)"+"),
        new (500, Operator.Type.yfx, (__string)"-"),
        new (400, Operator.Type.yfx, (__string)"*"),
        new (400, Operator.Type.yfx, (__string)"/"),
        new(900, Operator.Type.fx, "@-"),
        new(900, Operator.Type.xf, "-@"),
        new(800, Operator.Type.fy, "#-"),
        new(800, Operator.Type.yf, "-#"),
        new(900, Operator.Type.xfx, "@@"),
        new(900, Operator.Type.xfy, "##"),
        new(900, Operator.Type.yfx, "#@"),
        new(800, Operator.Type.fy, "#-"),
        new(800, Operator.Type.yf, "-#"),
    ];

    protected T Expect<T>(string input, Func<ErgoParser, Func<Maybe<T>>> parserFunc, bool parenthesized = false)
    {
        var stream = ErgoFileStream.Create(input);
        var opLookup = new OperatorLookup();
        opLookup.AddOperators(TestOperators);
        var lexer = new ErgoLexer(stream, opLookup);
        var parser = new ErgoParser(lexer);
        var result = parenthesized 
            ? parser.Parenthesized("(", ")", parserFunc(parser)) 
            : parserFunc(parser)();
        return result.GetOrThrow();
    }

    [Theory]
    [InlineData("'test string'")]
    [InlineData("\"test string\"")]
    public void __string(string input)
    {
        var result = Expect(input, p => p.__string);
        Assert.Equal(input.Replace("'", "").Replace("\"", ""), (string)result.Value);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    public void __bool(string input)
    {
        var result = Expect(input, p => p.__bool);
        Assert.Equal(Parse(), (bool)result.Value);
        bool Parse()
        {
            if (input == (string)Literals.True.Value)
                return true;
            if (input == (string)Literals.False.Value)
                return false;
            return bool.Parse(input);
        }
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-4592.123")]
    [InlineData("235.73459")]
    [InlineData("+2149593.01293")]
    public void __double(string input)
    {
        var result = Expect(input, p => p.__double);
        Assert.InRange(double.Parse(input) - (double)result.Value, -0.001, 0.001);
    }

    [Theory]
    [InlineData("identifier")]
    [InlineData("snake_case")]
    [InlineData("camelCase")]
    [InlineData("mixedCASE__")]
    public void Identifier(string input) => Expect(input, p => p.Identifier);

    [Theory]
    [InlineData("'test string'")]
    [InlineData("\"test string\"")]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("0")]
    [InlineData("-4592.123")]
    [InlineData("235.73459")]
    [InlineData("+2149593.01293")]
    [InlineData("identifier")]
    [InlineData("snake_case")]
    [InlineData("camelCase")]
    [InlineData("mixedCASE__")]
    [ClassData(typeof(ParserTestGenerator<Atom>))]
    public void Atom(string input) => Expect(input, p => p.Atom);

    [Theory]
    [InlineData("_")]
    [InlineData("X")]
    [InlineData("MyVariable")]
    [InlineData("My_Variable")]
    [ClassData(typeof(ParserTestGenerator<Variable>))]
    public void Variable(string input) => Expect(input, p => p.Variable);
    [Theory]
    [InlineData("my_complex(arg1)")]
    [InlineData("my_complex(arg1,arg2,g(arg3,f(arg4,arg5)))")]
    [ClassData(typeof(ParserTestGenerator<Complex>))]
    public void Complex(string input)
    {
        var result = Expect(input, p => p.Complex);
        Assert.Equal(input, result.Expl);
    }
    [Theory]
    [InlineData("term")]
    [InlineData("Term")]
    [InlineData("732.")]
    [InlineData("!")]
    [ClassData(typeof(ParserTestGenerator<Term>))]
    public void Term(string input) => Expect(input, p => p.Term);
    [Theory]
    [InlineData(":- a")]
    [InlineData(":- V")]
    [InlineData(":- c(x)")]
    [InlineData(":- mixed$")]
    [ClassData(typeof(ParserTestGenerator<PrefixExpression>))]
    public void PrefixExpression(string input)
    {
        var result = Expect(input, p => p.PrefixExpression);
        Assert.Equal(input, result.Expl);
    }
    [Theory]
    [InlineData("a$")]
    [InlineData("V$")]
    [InlineData("c(x)$")]
    [InlineData("[]-@")]
    [InlineData("ay2aj('O2  d'(!))-@")]
    [ClassData(typeof(ParserTestGenerator<PostfixExpression>))]
    public void PostfixExpression(string input)
    {
        var result = Expect(input, p => p.PostfixExpression);
        Assert.Equal(input, result.Expl);
    }
    [Theory]
    [InlineData("a :- b")]
    [InlineData("a|b")]
    [InlineData("a, (b, (c, d))")]
    [InlineData("((a, b), c), d")]
    [InlineData("a, b, c, d")]
    [ClassData(typeof(ParserTestGenerator<BinaryExpression>))]
    public void BinaryExpression(string input)
    {
        var result = Expect(input, p => p.BinaryExpression);
        Assert.Equal(input, result.Expl);
    }
    [Theory]
    [InlineData("a / b + c", "+(/(a,b),c)")]
    [InlineData("a + b / c", "+(a,/(b,c))")]
    [InlineData("a , b , c , d , e", "','(a,','(b,','(c,','(d,e))))")]
    [InlineData("a * b * c * d * e", "*(*(*(*(a,b),c),d),e)")]
    [InlineData("a / b / c / d / e", "/(/(/(/(a,b),c),d),e)")]
    [InlineData("a + b + c + d + e", "+(+(+(+(a,b),c),d),e)")]
    [InlineData("a - b - c - d - e", "-(-(-(-(a,b),c),d),e)")]
    [InlineData("a * b + c / a - d + e * f", "+(-(+(*(a,b),/(c,a)),d),*(e,f))")]
    [InlineData("a + b | c * d ; e", "'|'(+(a,b),;(*(c,d),e))")]
    [InlineData("a , b , c | Rest", "'|'(','(a,','(b,c)),Rest)")]
    public void BinaryExpressionWithPrecedence(string input, string expected)
    {
        var result = Expect(input, p => p.BinaryExpression);
        Assert.Equal(expected, result.ExplCanonical);
    }
    [Theory]
    [InlineData("a, b, c, d, e, f, g")]
    [InlineData("a, [b]")]
    [InlineData("a, [b, c]")]
    [ClassData(typeof(ParserTestGenerator<ConsExpression>))]
    public void ConsExpression(string input)
    {
        var result = Expect(input, p => p.ConsExpression(Operators.Conjunction));
        Assert.Equal(input, result.Expl);
    }
    [Theory]
    [InlineData("[a]", 1)]
    [InlineData("[a, b, c, D, E, F, 7, 8, 9, !]", 10)]
    [InlineData("[a, b, c|Rest]", 3)]
    [InlineData("[[a], b]", 2)]
    [InlineData("[[a, b], c]",2)]
    [InlineData("[[a, b], [c]]",2)]
    [InlineData("[a, [b]]",2)]
    [InlineData("[a, (b), [b, c]]",3)]
    [InlineData("[a, b, [b, c|Rest]]",3)]
    [InlineData("[a, b|[c]]", 3, "[a, b, c]")]
    [InlineData("[a, b|[c, d]]", 4, "[a, b, c, d]")]
    [InlineData("[a, b|[c, d|[e, f]]]", 6, "[a, b, c, d, e, f]")]
    [InlineData("[a, b|[c, d|Rest]]", 4, "[a, b, c, d|Rest]")]
    [InlineData("[a, b|[c, d|[e, f|Rest]]]", 6, "[a, b, c, d, e, f|Rest]")]
    [InlineData("[a, b|[c, d|[e, f|[]]]]", 6, "[a, b, c, d, e, f]")]
    public void List(string input, int len, string? expected = null)
    {
        var result = Expect(input, p => p.List);
        Assert.Equal(expected ?? input, result.Expl);
        Assert.Equal(len, result.Count);
    }
    [Fact]
    public void EmptyList()
    {
        Expect("[]", p => p.Atom);
        Expect("[]", p => p.Term);
        Expect("[]", p => p.EmptyList);
    }
    [Theory]
    [InlineData(":- no_args")]
    [InlineData(":- module(my_module,[])")]
    [ClassData(typeof(ParserTestGenerator<Directive>))]
    public void Directive(string input)
    {
        var result = Expect(input, p => p.Directive);
        Assert.Equal(input, result.Expl);
    }
    [Theory]
    [InlineData("fact")]
    [InlineData("complex_fact(a,b,C)")]
    [ClassData(typeof(ParserTestGenerator<Fact>))]
    public void Fact(string input)
    {
        var result = Expect(input, p => p.Fact);
        Assert.Equal(input, result.Expl);
    }
    [Theory]
    [InlineData(
@"clause(A) :-
    other_clause(A)")]
    [ClassData(typeof(ParserTestGenerator<Clause>))]
    public void Clause(string input)
    {
        var result = Expect(input, p => p.Clause);
        Assert.Equal(input.Replace("\r", ""), result.Expl);
    }
    [Theory]
    [InlineData(
@":- module(my_module,[]).
:- import(other_module).

fact.
other_fact(5).
clause :-
    fact.
clause(X) :-
    other_fact(X).
clause(Y) :-
    other_fact(Y),
    fact,
    other_fact(6).
")]
    [ClassData(typeof(ParserTestGenerator<Program>))]
    public void Program(string input)
    {
        var result = Expect(input, p => p.Program);
        Assert.Equal(input.Replace("\r", ""), result.Expl);
    }
    [Theory]
    [InlineData(":- directive(X, X).")]
    [InlineData(":- directive(X, X, f(X), Y, g(Y, h(Z)), Z, Z, Z, Z, _, _).")]
    public void SameDirectiveVariablesMustHaveReferenceEquality(string input)
    {
        var result = Expect(input, p => p.DirectiveDefinitions);
        foreach (var directive in result)
        {
            var variables = directive.GetVariables().ToHashSet();
            Assert.All(variables.GroupBy(x => x.Name), g => Assert.Single(g));
        }
    }
    [Theory]
    [InlineData("fact(X, X).")]
    [InlineData("fact(X, X, f(X), Y, g(Y, (h(Z))), Z, (Z), Z, Z, _, _).")]
    [InlineData("clause(X, X) :- fact(X), other_fact(X).")]
    public void SameClauseVariablesMustHaveReferenceEquality(string input)
    {
        var result = Expect(input, p => p.ClauseOrFactDefinitions);
        foreach (var clause in result)
        {
            var variables = clause.GetVariables();
            var groups = variables.GroupBy(x => x.Name);
            foreach (var group in groups)
            {
                var exemplar = group.First();
                Assert.All(group, x => Assert.True(ReferenceEquals(exemplar, x)));
                exemplar.Value = 1;
                Assert.All(group, x => Assert.Equal(1, x.Value));
            }
        }
    }
    [Theory]
    [InlineData("fact(X).", "fact(X).")]
    public void OtherClauseVariablesMustNotHaveReferenceEquality(string input1, string input2)
    {
        var result1 = Expect(input1, p => p.ClauseOrFactDefinitions);
        var result2 = Expect(input2, p => p.ClauseOrFactDefinitions);
        foreach (var (clause1, clause2) in result1.Zip(result2))
        {
            var variables1 = clause1.GetVariables();
            var variables2 = clause2.GetVariables();
            var names = variables1.Select(x => x.Name)
                .Concat(variables2.Select(x => x.Name))
                .Distinct();
            var lookup1 = variables1.ToLookup(x => x.Name);
            var lookup2 = variables2.ToLookup(x => x.Name);
            foreach (var name in names)
            {
                var exemplar1 = lookup1[name].First();
                var exemplar2 = lookup2[name].First();
                Assert.All(lookup1[name], x => Assert.True(ReferenceEquals(exemplar1, x)));
                Assert.All(lookup1[name], x => Assert.False(ReferenceEquals(exemplar2, x)));
                Assert.All(lookup2[name], x => Assert.True(ReferenceEquals(exemplar2, x)));
                Assert.All(lookup2[name], x => Assert.False(ReferenceEquals(exemplar1, x)));
                (exemplar1.Value, exemplar2.Value) = (1, 2);
                Assert.All(lookup1[name], x => Assert.Equal(1, x.Value));
                Assert.All(lookup2[name], x => Assert.Equal(2, x.Value));
            }
        }
    }
    [Theory]
    [InlineData("(a)")]
    [InlineData("(0)")]
    public void Parenthesized(string input)
    {
        var result = Expect(input, p => p.Atom, parenthesized: true);
        Assert.Equal(input, result.Parenthesized().Expl);
    }
}
