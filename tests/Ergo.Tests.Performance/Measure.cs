namespace Ergo.PerformanceTests;

public readonly record struct Measure(int Count, TimeSpan Duration)
{
    public readonly TimeSpan PerElement = Count == 0 ? TimeSpan.MaxValue : Duration / Count;
}
