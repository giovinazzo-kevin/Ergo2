using Ergo.Compiler.Emission;
using Ergo.Lang.Ast;
using Ergo.Lang.Ast.WellKnown;
using static Ergo.Lang.Ast.Operator;
using Query = Ergo.Compiler.Emission.Query;

namespace Ergo.Runtime.WAM;

public partial class ErgoVM
{
    #region Abstract Term Reconstruction
    private readonly Dictionary<(object Functor, int Arity), Func<Lang.Ast.Term[], Lang.Ast.Term>> _reconstructors = [];

    public void RegisterReconstructor(object functor, int arity, Func<Lang.Ast.Term[], Lang.Ast.Term> factory)
    {
        _reconstructors[(functor, arity)] = factory;
    }

    public void RegisterOperator(Operator op)
    {
        int arity = op.Fixity_ switch {
            Fixity.Prefix or Fixity.Postfix => 1,
            Fixity.Infix => 2,
            _ => throw new NotSupportedException($"Unknown fixity: {op.Fixity_}")
        };
        Func<Lang.Ast.Term[], Lang.Ast.Term> factory = op.Fixity_ switch {
            Fixity.Infix => args => new BinaryExpression(op, args[0], args[1]),
            Fixity.Prefix => args => new PrefixExpression(op, args[0]),
            Fixity.Postfix => args => new PostfixExpression(op, args[0]),
            _ => throw new NotSupportedException()
        };
        foreach (Atom functor in op.Functors)
            _reconstructors[(functor.Value, arity)] = factory;
    }

    public void RegisterOperator(Operator op, Func<Lang.Ast.Term[], Lang.Ast.Term> factory)
    {
        int arity = op.Fixity_ switch {
            Fixity.Prefix or Fixity.Postfix => 1,
            Fixity.Infix => 2,
            _ => throw new NotSupportedException()
        };
        foreach (Atom functor in op.Functors)
            _reconstructors[(functor.Value, arity)] = factory;
    }

    public void RegisterWellKnownOperators()
    {
        // General operator reconstruction
        RegisterOperator(Operators.Conjunction);
        RegisterOperator(Operators.Disjunction);
        RegisterOperator(Operators.Unification);
        RegisterOperator(Operators.Addition);
        RegisterOperator(Operators.Subtraction);
        RegisterOperator(Operators.Multiplication);
        RegisterOperator(Operators.Division);
        RegisterOperator(Operators.Pipe);

        // Special cases: :-/2 → Clause, :-/1 → Directive
        RegisterOperator(Operators.HornBinary, args => new Lang.Ast.Clause(args[0], args[1]));
        RegisterOperator(Operators.HornUnary, args => new Lang.Ast.Directive(args[0]));
    }
    #endregion

    public event Action<ErgoVM> SolutionEmitted = _ => { };
    private int _traceLevel = 0;

    public void Run(Query query)
    {
        CP = int.MaxValue;
        _QUERY = query.Bytecode;
        _VARS = query.Variables;
        _builtInHandlers = query.BuiltInHandlers;
        _abstractTerms = query.AbstractTerms;
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

