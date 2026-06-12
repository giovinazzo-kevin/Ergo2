namespace Ergo.Shared.Interfaces;

/// <summary>
/// Abstraction over the WAM heap for abstract term unification and reading.
/// Concrete implementation in Ergo.Runtime.WAM.
/// </summary>
public interface IHeapAccess
{
    int H { get; }
    int this[int addr] { get; set; }
    int Deref(int addr);
}
