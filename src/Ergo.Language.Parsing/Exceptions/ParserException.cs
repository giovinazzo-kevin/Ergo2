using Ergo.Shared.Exceptions;

namespace Ergo.Language.Parsing;

public sealed class ParserException(ParserError error, params object[] args) : ErgoException<ParserError>(error, args);