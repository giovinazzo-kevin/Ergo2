﻿using Ergo.IO;
using Ergo.Language.Ast;
using Ergo.Language.Lexer;
using Ergo.Language.Parser;
using Ergo.Shared.Interfaces;
using Ergo.Shared.Types;
using Ergo.Tooling;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Ergo.PerformanceTests;

public class PerformanceTests
{
    static readonly string TEST_CASES_DIR = Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\ergo\test_cases");
    [Theory]
    [InlineData(1000, 100, nameof(TermGeneratorProfile.Debug))]
    [InlineData(0.05, 1000_00, nameof(TermGeneratorProfile.Minimal))]
    [InlineData(0.10, 1000_00, nameof(TermGeneratorProfile.Default))]
    [InlineData(0.30, 1000_0, nameof(TermGeneratorProfile.StressTest))]
    public void Atom(double TARGET_MS, int NUM_SAMPLES, string PROFILE)
    {
        TestCase(g => g.Atom, p => p.Atom, TARGET_MS, NUM_SAMPLES, PROFILE);
    }
    [Theory]
    [InlineData(1000, 100, nameof(TermGeneratorProfile.Debug))]
    [InlineData(0.05, 1000_00, nameof(TermGeneratorProfile.Minimal))]
    [InlineData(0.05, 1000_00, nameof(TermGeneratorProfile.Default))]
    [InlineData(0.10, 1000_0, nameof(TermGeneratorProfile.StressTest))]
    public void Variable(double TARGET_MS, int NUM_SAMPLES, string PROFILE)
    {
        TestCase(g => g.Variable, p => p.Variable, TARGET_MS, NUM_SAMPLES, PROFILE);
    }
    [Theory]
    [InlineData(1000, 100, nameof(TermGeneratorProfile.Debug))]
    [InlineData(1.00, 10000, nameof(TermGeneratorProfile.Minimal))]
    [InlineData(1.50, 10000, nameof(TermGeneratorProfile.Default))]
    [InlineData(5.00, 10000, nameof(TermGeneratorProfile.StressTest))]
    public void Complex(double TARGET_MS, int NUM_SAMPLES, string PROFILE)
    {
        TestCase(g => g.Complex, p => p.Complex, TARGET_MS, NUM_SAMPLES, PROFILE);
    }
    [Theory]
    [InlineData(1000, 100, nameof(TermGeneratorProfile.Debug))]
    [InlineData(0.50, 10000, nameof(TermGeneratorProfile.Minimal))]
    [InlineData(0.70, 10000, nameof(TermGeneratorProfile.Default))]
    [InlineData(2.50, 10000, nameof(TermGeneratorProfile.StressTest))]
    public void Term(double TARGET_MS, int NUM_SAMPLES, string PROFILE)
    {
        TestCase(g => g.Term, p => p.Term, TARGET_MS, NUM_SAMPLES, PROFILE);
    }
    [Theory]
    [InlineData(1000, 100, nameof(TermGeneratorProfile.Debug))]
    [InlineData(0.40, 1000, nameof(TermGeneratorProfile.Minimal))]
    [InlineData(0.70, 1000, nameof(TermGeneratorProfile.Default))]
    [InlineData(2.50, 1000, nameof(TermGeneratorProfile.StressTest))]
    public void PrefixExpression(double TARGET_MS, int NUM_SAMPLES, string PROFILE)
    {
        TestCase(g => g.PrefixExpression, p => p.PrefixExpression, TARGET_MS, NUM_SAMPLES, PROFILE);
    }
    [Theory]
    [InlineData(1000, 100, nameof(TermGeneratorProfile.Debug))]
    [InlineData(0.40, 1000, nameof(TermGeneratorProfile.Minimal))]
    [InlineData(0.70, 1000, nameof(TermGeneratorProfile.Default))]
    [InlineData(2.50, 1000, nameof(TermGeneratorProfile.StressTest))]
    public void PostfixExpression(double TARGET_MS, int NUM_SAMPLES, string PROFILE)
    {
        TestCase(g => g.PostfixExpression, p => p.PostfixExpression, TARGET_MS, NUM_SAMPLES, PROFILE);
    }
    [Theory]
    [InlineData(1000, 100, nameof(TermGeneratorProfile.Debug))]
    [InlineData(0.40, 1000, nameof(TermGeneratorProfile.Minimal))]
    [InlineData(0.70, 1000, nameof(TermGeneratorProfile.Default))]
    [InlineData(2.50, 1000, nameof(TermGeneratorProfile.StressTest))]
    public void BinaryExpression(double TARGET_MS, int NUM_SAMPLES, string PROFILE)
    {
        TestCase(g => g.BinaryExpression, p => p.BinaryExpression, TARGET_MS, NUM_SAMPLES, PROFILE);
    }
    protected void TestCase<T>(Func<TermGenerator, Func<IExplainable>> generate, Func<ErgoParser, Func<Maybe<T>>> parse, double TARGET_MS, int NUM_SAMPLES, string PROFILE, [CallerMemberName] string methodName = null!)
    {
        if (!GetProfile(PROFILE).TryGetValue(out var profile)) return;
        var file = SetupTestCase(generate, profile, NUM_SAMPLES, out var ops, methodName);
        var measure = Parse(file, parse, TARGET_MS, NUM_SAMPLES, ops);
    }

    public Measure Parse<T>(ErgoFileStream file, Func<ErgoParser, Func<Maybe<T>>> p, double targetMsPerElement, int targetSamples, OperatorLookup ops)
    {
        var parser = BuildParser(ops, file);
        var measure = MeasureParser(parser, p(parser));
        Assert.Equal(targetSamples, measure.Count);
#if !DEBUG
        Assert.InRange(measure.PerElement.TotalMilliseconds, 0, targetMsPerElement);
        // !!AN OPTIMIZATION HAS CAUSED AN INCREASE IN PERFORMANCE BY AN ORDER OF MAGNITUDE!!
        Assert.True(measure.PerElement.TotalMilliseconds > targetMsPerElement / 10);
#endif
        return measure;
    }
    static Measure MeasureParser<T>(ErgoParser parser, Func<Maybe<T>> p)
    {
        var sw = new Stopwatch();
        int count = 0;
        sw.Start();
        foreach (var _ in parser.ParseUntilFailEnumerable(p))
            ++count;
        sw.Stop();
        return new(count, sw.Elapsed);
    }
    static ErgoParser BuildParser(OperatorLookup ops, ErgoFileStream file)
    {
        var lexer = new ErgoLexer(file, ops);
        return new ErgoParser(lexer);
    }
    static void DeleteCachedTestCases()
    {
        if (Directory.Exists(TEST_CASES_DIR))
            Directory.Delete(TEST_CASES_DIR);
    }
    static Maybe<TermGeneratorProfile> GetProfile(string name) => name switch
    {
#if PROFILE_DEBUG
        nameof(TermGeneratorProfile.Debug) => TermGeneratorProfile.Debug,
#endif
#if PROFILE_DEFAULT
        nameof(TermGeneratorProfile.Default) => TermGeneratorProfile.Default,
#endif
#if PROFILE_MINIMAL
        nameof(TermGeneratorProfile.Minimal) => TermGeneratorProfile.Minimal,
#endif
#if PROFILE_STRESSTEST
        nameof(TermGeneratorProfile.StressTest) => TermGeneratorProfile.StressTest,
#endif
        _ => Maybe<TermGeneratorProfile>.None
    };
    static ErgoFileStream SetupTestCase(Func<TermGenerator, Func<IExplainable>> generator, TermGeneratorProfile profile, int numSamples, out OperatorLookup ops, [CallerMemberName] string name = "")
    {
        ops = new();
        ops.AddOperators([
            new(900, Operator.Type.fx, "@-"),
            new(900, Operator.Type.xf, "-@"),
            new(900, Operator.Type.xfx, "@-@"),
        ]);
        var dir = Path.Combine(TEST_CASES_DIR, @$"{profile.Name}\");
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        name = $"{name}_{numSamples}.ergo";
        var fileInfo = new FileInfo(Path.Combine(dir, name));
        if (fileInfo.Exists)
            return ErgoFileStream.Open(fileInfo);
        {
            var gen = new TermGenerator(ops) { Profile = profile };
            using var fs = fileInfo.Create();
            using var fw = new StreamWriter(fs);
            var lines = Enumerable.Range(0, numSamples)
                .Select(_ => generator(gen)());
            foreach (var line in lines)
            {
                fw.WriteLine(line.Expl);
#if DEBUG
                fw.Flush();
#endif
            }
#if !DEBUG
            fw.Flush();
#endif
        }
        return ErgoFileStream.Open(fileInfo);
    }
}
