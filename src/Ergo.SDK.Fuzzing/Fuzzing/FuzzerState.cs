using Ergo.Shared.Types;

namespace Ergo.SDK.Fuzzing;

public readonly record struct FuzzerState(int Epoch, int Worker, Maybe<string> Result);
