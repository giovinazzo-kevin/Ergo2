namespace Ergo.Lang.Ast.WellKnown;

public static class Hooks
{
    // compile-time: fired by the emitter/analyzer
    public static readonly __string GoalEmission = "goal_emission";
    public static readonly __string TermEmission = "term_emission";

    // runtime: fired by the VM during resolution
    public static readonly __string Unify = "unify";
    public static readonly __string OnCall = "on_call";

    // runtime: fired by dynamic predicate operations
    public static readonly __string OnAssert = "on_assert";
    public static readonly __string OnRetract = "on_retract";
}
