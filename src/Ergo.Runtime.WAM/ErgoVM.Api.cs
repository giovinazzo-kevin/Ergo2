using Ergo.Compiler.Emission;
using Ergo.Lang.Ast;
using Ergo.Lang.Ast.WellKnown;
using static Ergo.Lang.Ast.Operator;
using Query = Ergo.Compiler.Emission.Query;

namespace Ergo.Runtime.WAM;

public partial class ErgoVM
{
    #region Abstract Term Reconstruction
    public IReadOnlyDictionary<(object Functor, int Arity), Func<Lang.Ast.Term[], Lang.Ast.Term>> _reconstructors = new Dictionary<(object, int), Func<Lang.Ast.Term[], Lang.Ast.Term>>();
    #endregion

    public event Action<ErgoVM> SolutionEmitted = _ => { };
    private int _traceLevel = 0;

    public void Run(Query query)
    {
        CP = int.MaxValue;
        _QUERY = query.Bytecode;
        _VARS = query.Variables;
        _builtInHandlers = query.BuiltInHandlers;
        _abstractTerms = query.AbstractTerms?.ToDictionary(kv => kv.Key, kv => new AbstractTermDispatch(kv.Value));
        _reconstructors = query.Reconstructors;
        if (_kb == null && query.Source != null)
            InitDynamic(new Emitter(), query.Source);
        // Re-append live dynamic clauses to new query bytecode
        if (_dynamics.Count > 0)
            RehydrateDynamicCode();
        _dynConts.Clear();
        _inDynClause = false;
        P = _QUERY.QueryStart;
        E = HEAP_SIZE;
        B = HEAP_SIZE;
        H = 0;
        TR = 0;
        exit = fail = false;
        while (!exit) {
            if (fail) {
                if (backtrack())
                    break;
                continue;
            }
            if (P >= Code.Length) {
                EmitSolution();
                break;
            }
            var op = __word();
            OP_TABLE[op](this);
        }
    }

}
