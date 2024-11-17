using Ergo.Shared.Exceptions;

namespace Ergo.Language.Lexing;

public sealed class LexerException(LexerError error, params object[] args) : ErgoException<LexerError>(error, args);