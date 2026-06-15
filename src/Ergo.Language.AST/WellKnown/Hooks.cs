namespace Ergo.Lang.Ast.WellKnown;

public static class Hooks
{
    // compile-time: fired by the emitter/analyzer
    public static readonly __string GoalExpansion = "goal_expansion";
    public static readonly __string TermExpansion = "term_expansion";

    // runtime: fired by the VM during resolution
    public static readonly __string Unify = "unify";
    public static readonly __string OnCall = "on_call";

    // runtime: fired by dynamic predicate operations
    public static readonly __string OnAssert = "on_assert";
    public static readonly __string OnRetract = "on_retract";
}
