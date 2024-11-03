using Ergo.Shared.Exceptions;

namespace Ergo.Language.Parser;

public sealed class ParserException(ParserError error, params object[] args) : ErgoException<ParserError>(error, args);