namespace Ergo.SDK.Fuzzing;

public readonly record struct TermGeneratorProfile(
    string Name,
    int MaxIdentifierLength,
    int MaxComplexFunctorLength,
    int MaxComplexArgLength,
    int MaxComplexArity,
    int MaxComplexDepth,
    int MaxExpressionDepth,
    int MinProgramDirectives,
    int MaxProgramDirectives,
    int MinProgramClauses,
    int MaxProgramClauses,
    double NumberMagnitude,
    double NumberPrecision,
    bool IncludeQuotedStrings
)
{
    public static readonly char[] StartIdentifierChars = [
        .. "abcdefghijklmnopqrstuvwxyz"
    ];
    public static readonly char[] IdentifierChars = [
        .. "_",
        .. "abcdefghijklmnopqrstuvwxyz",
        .. "0123456789"
    ];
    public static readonly TermGeneratorProfile Default = new()
    {
        Name = nameof(Default),
        MaxIdentifierLength = 8,
        MaxComplexArgLength = 3,
        MaxComplexFunctorLength = 5,
        MaxComplexArity = 2,
        MaxComplexDepth = 2,
        MaxExpressionDepth = 2,
        MinProgramDirectives = 1,
        MaxProgramDirectives = 3,
        MinProgramClauses = 1,
        MaxProgramClauses = 3,
        NumberMagnitude = 1_000,
        NumberPrecision = 6,
        IncludeQuotedStrings = true
    };
    public static readonly TermGeneratorProfile Minimal = Default with
    {
        Name = nameof(Minimal),
        MaxIdentifierLength = 1,
        MaxComplexArgLength = 1,
        MaxComplexFunctorLength = 1,
        MaxComplexArity = 1,
        MaxComplexDepth = 1,
        MaxExpressionDepth = 1,
        MinProgramDirectives = 1,
        MaxProgramDirectives = 1,
        MinProgramClauses = 1,
        MaxProgramClauses = 1,
        NumberMagnitude = 1,
        NumberPrecision = 0,
        IncludeQuotedStrings = false
    };
    public static readonly TermGeneratorProfile StressTest = Default with
    {
        Name = nameof(StressTest),
        MaxIdentifierLength = 4,
        MaxComplexArgLength = 3,
        MaxComplexFunctorLength = 2,
        MaxComplexArity = 3,
        MaxComplexDepth = 5,
        MaxExpressionDepth = 5,
        MinProgramDirectives = 10,
        MaxProgramDirectives = 10,
        MinProgramClauses = 10,
        MaxProgramClauses = 10,
        NumberMagnitude = 10,
        NumberPrecision = 0,
        IncludeQuotedStrings = false
    };
    public static readonly TermGeneratorProfile Debug = Default with
    {
        Name = nameof(Debug)
    };
}
