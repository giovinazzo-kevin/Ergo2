
using Ergo.Compiler.Emission;

namespace Ergo.Runtime.WAM;

public partial class ErgoVM
{
    #region External Memory
    #endregion

    #region Physical Memory Layout
    public __WORD[] _RAM = new __WORD[HEAP_SIZE + STACK_SIZE + MAX_ARGS + MAX_TMPS + DEFAULT_TRAIL_SIZE];
    public QueryBytecode _QUERY = null!;
    public Dictionary<string, int> _VARS = null!;
    #endregion

    #region Logical Memory Areas
    public ReadOnlySpan<__WORD> Code => _QUERY.Code;
    public ReadOnlySpan<Lang.Ast.Atom> Constants => _QUERY.Constants;
    public Span<__WORD> Store => _RAM.AsSpan();
    public Span<__WORD> Heap => _RAM.AsSpan(0, HEAP_SIZE);
    public Span<__WORD> Stack => _RAM.AsSpan(HEAP_SIZE, STACK_SIZE);
    public Span<__WORD> A => _RAM.AsSpan(HEAP_SIZE + STACK_SIZE, MAX_ARGS);
    public Span<__WORD> V => _RAM.AsSpan(HEAP_SIZE + STACK_SIZE + MAX_ARGS, MAX_TMPS);
    public Span<__WORD> Trail => _RAM.AsSpan(HEAP_SIZE + STACK_SIZE + MAX_ARGS + MAX_TMPS, DEFAULT_TRAIL_SIZE);
    #endregion

    #region State Registers
    public __ADDR P { get; set; }
    public __ADDR CP { get; set; }
    public __ADDR S { get; set; }
    public __ADDR HB { get; set; }
    public __ADDR H { get; set; }
    public __ADDR B0 { get; set; }
    public __ADDR B { get; set; }
    public __ADDR E { get; set; }
    public __ADDR TR { get; set; }
    public __WORD N { get; set; }
    public bool fail { get; set; }
    public GetMode mode { get; set; }
    #endregion

    #region Memory Access Helpers
    protected bool defined(Signature sig, out __ADDR address)
        => _QUERY.Labels.TryGetValue(sig, out address);
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
