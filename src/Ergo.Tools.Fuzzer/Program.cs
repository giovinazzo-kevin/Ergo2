using Ergo.Language.Ast;
using Ergo.Language.Lexing;
using Ergo.SDK.Fuzzing;
var opLookup = new OperatorLookup([
    new (60, Operator.Type.xf, (__string)"$"),
    new (500, Operator.Type.yfx, (__string)"+"),
    new (500, Operator.Type.yfx, (__string)"-"),
    new (400, Operator.Type.yfx, (__string)"*"),
    new (400, Operator.Type.yfx, (__string)"/"),
    new(900, Operator.Type.fx, "@-"),
    new(900, Operator.Type.xf, "-@"),
    new(900, Operator.Type.xfx, "@-@"),
]);
var fuzzer = new ErgoFuzzer(TermGeneratorProfile.StressTest, opLookup);
fuzzer.BatchCompleted += Fuzzer_BatchCompleted;
var query = fuzzer.FindInvalidInputs(
    g => g.Module, 
    p => p.Module,
    maxEpochs: 0,
    workersPerEpoch: 16);
await foreach (var input in query)
    Console.WriteLine(input);
void Fuzzer_BatchCompleted(FuzzerState[] states)
{
    Console.Title = $"Attempts: {states[0].Epoch * states.Length}";
}
