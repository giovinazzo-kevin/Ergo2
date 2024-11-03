using Ergo.Shared.Exceptions;

namespace Ergo.Language.Lexer;

public sealed class LexerException(LexerError error, params object[] args) : ErgoException<LexerError>(error, args);