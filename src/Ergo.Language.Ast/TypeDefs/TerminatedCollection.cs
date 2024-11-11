namespace Ergo.Language.Ast;

public record TerminatedCollection(Atom EmptyElement, string OpeningDelim, string ClosingDelim) : Collection(OpeningDelim, ClosingDelim);
