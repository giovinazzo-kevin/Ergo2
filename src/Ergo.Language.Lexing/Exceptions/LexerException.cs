using Ergo.Shared.Exceptions;

namespace Ergo.Lang.Lexing;

public sealed class LexerException(LexerError error, params object[] args) : ErgoException<LexerError>(error, args);