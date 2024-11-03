namespace Ergo.Tooling;

public readonly record struct TermGeneratorProfile(
    string Name,
    int MaxIdentifierLength,
    int MaxComplexFunctorLength,
    int MaxComplexArgLength,
    int MaxComplexArity,
    int MaxExpressionDepth,
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
        MaxExpressionDepth = 2,
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
        MaxExpressionDepth = 1,
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
        MaxExpressionDepth = 3,
        NumberMagnitude = 10,
        NumberPrecision = 0,
        IncludeQuotedStrings = false
    };
    public static readonly TermGeneratorProfile MonsterObjects = Default with
    {
        Name = nameof(MonsterObjects),
        MaxIdentifierLength = 1,
        MaxComplexArgLength = 1,
        MaxComplexFunctorLength = 1,
        MaxComplexArity = 256,
        MaxExpressionDepth = 256,
        NumberMagnitude = 1,
        NumberPrecision = 0,
        IncludeQuotedStrings = false
    };
    public static readonly TermGeneratorProfile Debug = Default with
    {
        Name = nameof(Debug),
        MaxIdentifierLength = 32,
        MaxComplexArgLength = 1,
        MaxComplexFunctorLength = 1,
        MaxComplexArity = 2,
        MaxExpressionDepth = 256,
        IncludeQuotedStrings = false
    };
}
