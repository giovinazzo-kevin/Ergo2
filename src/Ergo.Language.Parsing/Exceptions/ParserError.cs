namespace Ergo.Lang.Parsing;

public enum ParserError
{
    UnexpectedEndOfFile
    , PredicateHasSingletonVariables
    , ExpectedArgumentDelimiterOrClosedParens
    , ExpectedPredicateDelimiterOrTerminator
    , ExpectedClauseList
    , KeyExpected
    , UnterminatedClauseList
    , ComplexHasNoArguments
    , OperatorDoesNotExist
    , TermHasIllegalName
    , MismatchedParentheses
}
