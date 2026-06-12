namespace Ergo.Shared.Interfaces;

/// <summary>
/// Abstraction over the emitter context for abstract term compilation.
/// Concrete implementation in Ergo.Compiler.Emission.
/// </summary>
public interface IEmitterContext
{
    int PC { get; }
    int NumVars { get; set; }
    int Constant(object value);
    void Emit(int instruction);
}
