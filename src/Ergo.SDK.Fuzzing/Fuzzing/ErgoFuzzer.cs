using Ergo.IO;
using Ergo.Language.Lexing;
using Ergo.Language.Parsing;
using Ergo.Shared.Interfaces;
using Ergo.Shared.Types;
using System.Runtime.CompilerServices;

namespace Ergo.SDK.Fuzzing;

public class ErgoFuzzer(TermGeneratorProfile profile, OperatorLookup ops)
{
    private readonly TermGenerator _gen = new(ops) { Profile = profile };
    public ErgoFuzzer() : this(TermGeneratorProfile.Default, new()) { }

    public event Action<FuzzerState[]> BatchCompleted = _ => { };

    public async IAsyncEnumerable<string> FindInvalidInputs<T>(
        Func<TermGenerator, Func<T>> g, 
        Func<Parser, Func<Maybe<T>>> p,
        int maxEpochs = 0,
        int workersPerEpoch = 4,
        [EnumeratorCancellation] CancellationToken ct = default)
        where T : IExplainable
    {
        var states = new FuzzerState[workersPerEpoch];
        var workers = new Task<Maybe<string>>[workersPerEpoch];
        for (int i = 0; maxEpochs <= 0 || i < maxEpochs; i++)
        {
            if (ct.IsCancellationRequested)
                yield break;
            for (int j = 0; j < workersPerEpoch; j++)
                workers[j] = Task.Run(Worker, ct);
            await Task.WhenAll(workers);
            for (int j = 0; j < workersPerEpoch; j++)
                states[j] = new(i, j, workers[j].Result);
            BatchCompleted?.Invoke(states);
            if (ct.IsCancellationRequested)
                yield break;
            foreach (var invalidTerm in workers.SelectMany(x => x.Result.AsEnumerable()))
                yield return invalidTerm;
        }

        Maybe<string> Worker()
        {
            var term = g(_gen)();
            var expl = term.Expl;
            var file = ErgoFileStream.Create(expl);
            using var lexer = new Lexer(file, ops);
            using var parser = new Parser(lexer);
            var result = p(parser)();
            if (!result.HasValue)
                return expl;
            return default;
        }
    }

}
