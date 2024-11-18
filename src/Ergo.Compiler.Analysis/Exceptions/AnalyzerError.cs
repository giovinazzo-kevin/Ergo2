using Ergo.Lang.Parsing;
using System.ComponentModel;

namespace Ergo.Compiler.Analysis.Exceptions;

public enum AnalyzerError
{
    [Description("module `{0}` must begin with a module/2 directive")]
    Module0MustStartWithModuleDirective,
    [Description("module `{0}` must be named `{1}`")]
    Module0MustBeNamed1, // non-standard but needed due to how libraries are linked
    [Description("cannot redefine module `{0}` as `{1}` {2}")]
    CannotRedefineModule0As1When2,
    [Description("undefined module: `{0}`")]
    UndefinedModule0,
    [Description("undefined predicate: `{0}`")]
    UndefinedPredicate0,
    [Description("unresolved goal: `{0}`")]
    UnresolvedGoal0,
    [Description("unresolved directive: `{0}`")]
    UnresolvedDirective0,
    [Description("expected term of type `{0}` at `{1}`; found `{2}`")]
    ExpectedTermOfType0At1Found2,
    [Description("the head of a clause can not be a variable:\r\n{0}")]
    Clause0HeadCanNotBeAVariable,

}
