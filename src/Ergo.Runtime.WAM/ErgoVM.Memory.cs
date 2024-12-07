
using Ergo.Compiler.Emission;

namespace Ergo.Runtime.WAM;

public partial class ErgoVM
{
    #region External Memory
    #endregion

    #region Physical Memory Layout
    private __WORD[] _RAM = new __WORD[HEAP_SIZE + STACK_SIZE + MAX_ARGS + MAX_TMPS + DEFAULT_TRAIL_SIZE];
    private Bytecode _BYTECODE = null!;
    private readonly Dictionary<__WORD, __ADDR> _labels = [];
    #endregion

    #region Logical Memory Areas
    public ReadOnlySpan<__WORD> Code => _BYTECODE.Code;
    public ReadOnlySpan<Lang.Ast.Atom> Constants => _BYTECODE.Constants;
    public Span<__WORD> Store => _RAM.AsSpan();
    public Span<__WORD> Heap => _RAM.AsSpan(0, HEAP_SIZE);
    public Span<__WORD> Stack => _RAM.AsSpan(HEAP_SIZE, STACK_SIZE);
    public Span<__WORD> A => _RAM.AsSpan(HEAP_SIZE + STACK_SIZE, MAX_ARGS);
    public Span<__WORD> V => _RAM.AsSpan(HEAP_SIZE + STACK_SIZE + MAX_ARGS, MAX_TMPS);
    public Span<__WORD> Trail => _RAM.AsSpan(HEAP_SIZE + STACK_SIZE + MAX_ARGS + MAX_TMPS, DEFAULT_TRAIL_SIZE);
    #endregion

    #region State Registers
    public __ADDR P { get; protected set; }
    public __ADDR CP { get; protected set; }
    public __ADDR S { get; protected set; }
    public __ADDR HB { get; protected set; }
    public __ADDR H { get; protected set; }
    public __ADDR B0 { get; protected set; }
    public __ADDR B { get; protected set; }
    public __ADDR E { get; protected set; }
    public __ADDR TR { get; protected set; }
    public __WORD N { get; protected set; }
    public bool fail { get; protected set; }
    public GetMode mode { get; protected set; }
    #endregion

    #region Memory Access Helpers
    protected bool defined(Signature sig, out __ADDR address)
        => _labels.TryGetValue(sig, out address);
    protected (bool Found, __ADDR Address) get_hash(__WORD match, __ADDR table, __WORD n)
    {
        for (__WORD i = 0; i < n; i++)
        {
            var k = table + i * 2;
            var key = Code[k];
            var value = Code[k + 1];
            if (match == key)
                return (true, value);
        }
        return (false, default);
    }
    #endregion
}
