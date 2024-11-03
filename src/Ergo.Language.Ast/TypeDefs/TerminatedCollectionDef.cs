namespace Ergo.Language.Ast;

public record TerminatedCollectionDef(Atom EmptyElement, string OpeningDelim, string ClosingDelim) : Collection(OpeningDelim, ClosingDelim);
