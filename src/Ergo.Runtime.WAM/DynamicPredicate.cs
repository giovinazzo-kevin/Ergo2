using Ergo.Lang.Ast;

namespace Ergo.Runtime.WAM;

public class DynamicPredicate
{
    public readonly List<DynClause> Clauses = [];

    public IEnumerable<DynClause> Visible(int goalGen)
        => Clauses.Where(c => c.CreatedGen <= goalGen && goalGen < c.ErasedGen);
}

public class DynClause
{
    public readonly __WORD[] Code;
    public readonly Atom[] NewConstants;
    public readonly int CreatedGen;
    public int ErasedGen = int.MaxValue;
    public int Offset; // Updated per-query via RehydrateDynamicCode

    public DynClause(__WORD[] code, Atom[] newConstants, int createdGen)
    {
        Code = code;
        NewConstants = newConstants;
        CreatedGen = createdGen;
    }
}

public record struct DynContinuation(__WORD Sig, int ClauseIndex, int GoalGen);
